using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using KnockingTool.Models;

namespace KnockingTool.Services;

public static class PacketSender
{
    public static async Task<(bool Success, string Message)> SendAsync(KnockStep step, string destinationIp)
    {
        return step.Protocol switch
        {
            KnockProtocol.Icmp => await SendIcmpAsync(destinationIp, step),
            KnockProtocol.Tcp => await SendTcpAsync(destinationIp, step),
            KnockProtocol.Udp => await SendUdpAsync(destinationIp, step),
            _ => (false, "پروتکل ناشناخته")
        };
    }

    private static async Task<(bool Success, string Message)> SendIcmpAsync(string destinationIp, KnockStep step)
    {
        if (!IPAddress.TryParse(destinationIp, out var ipAddress))
        {
            return (false, "آدرس IP نامعتبر است");
        }

        try
        {
            var buffer = BuildPayload(step, 65500);

            if (step.IncludeIpHeader)
            {
                return await Task.Run(() => SendIcmpWithRawHeader(ipAddress, buffer));
            }

            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 3000, buffer);
            var success = reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired;
            return (success, $"ICMP Echo — وضعیت: {reply.Status}, RTT: {reply.RoundtripTime}ms");
        }
        catch (FormatException ex)
        {
            return (false, $"محتوای هگز نامعتبر: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"خطا در ICMP: {ex.Message}");
        }
    }

    private static (bool Success, string Message) SendIcmpWithRawHeader(IPAddress destination, byte[] payload)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

        var packet = BuildIpIcmpPacket(destination, payload);
        socket.SendTo(packet, new IPEndPoint(destination, 0));
        return (true, $"ICMP با هدر IP+ICMP (28 بایت) + payload {payload.Length} بایت ارسال شد");
    }

    private static byte[] BuildIpIcmpPacket(IPAddress destination, byte[] payload)
    {
        const int ipHeaderSize = 20;
        const int icmpHeaderSize = 8;
        var payloadSize = payload.Length;
        var totalLength = (ushort)(ipHeaderSize + icmpHeaderSize + payloadSize);
        var packet = new byte[totalLength];

        packet[0] = 0x45;
        packet[1] = 0x00;
        packet[2] = (byte)(totalLength >> 8);
        packet[3] = (byte)(totalLength & 0xFF);
        packet[4] = 0x00;
        packet[5] = 0x01;
        packet[6] = 0x40;
        packet[7] = 0x00;
        packet[8] = 64;
        packet[9] = 1; // ICMP
        packet[10] = 0x00;
        packet[11] = 0x00;

        var sourceIp = GetLocalIpForDestination(destination);
        Buffer.BlockCopy(sourceIp.GetAddressBytes(), 0, packet, 12, 4);
        Buffer.BlockCopy(destination.GetAddressBytes(), 0, packet, 16, 4);

        var ipChecksum = ComputeChecksum(packet.AsSpan(0, ipHeaderSize));
        packet[10] = (byte)(ipChecksum >> 8);
        packet[11] = (byte)(ipChecksum & 0xFF);

        var icmpOffset = ipHeaderSize;
        packet[icmpOffset] = 8;
        packet[icmpOffset + 1] = 0;
        packet[icmpOffset + 2] = 0x00;
        packet[icmpOffset + 3] = 0x00;
        packet[icmpOffset + 4] = 0x00;
        packet[icmpOffset + 5] = 0x01;
        packet[icmpOffset + 6] = 0x00;
        packet[icmpOffset + 7] = 0x01;

        if (payloadSize > 0)
        {
            Buffer.BlockCopy(payload, 0, packet, icmpOffset + icmpHeaderSize, payloadSize);
        }

        var icmpChecksum = ComputeChecksum(packet.AsSpan(icmpOffset, icmpHeaderSize + payloadSize));
        packet[icmpOffset + 2] = (byte)(icmpChecksum >> 8);
        packet[icmpOffset + 3] = (byte)(icmpChecksum & 0xFF);

        return packet;
    }

    private static IPAddress GetLocalIpForDestination(IPAddress destination)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Connect(new IPEndPoint(destination, 1));
        if (socket.LocalEndPoint is IPEndPoint localEndPoint)
        {
            return localEndPoint.Address;
        }

        return IPAddress.Parse("127.0.0.1");
    }

    private static ushort ComputeChecksum(ReadOnlySpan<byte> data)
    {
        uint sum = 0;
        var i = 0;
        while (i < data.Length - 1)
        {
            sum += (ushort)(data[i] << 8 | data[i + 1]);
            i += 2;
        }

        if (data.Length % 2 == 1)
        {
            sum += (ushort)(data[^1] << 8);
        }

        while ((sum >> 16) != 0)
        {
            sum = (sum & 0xFFFF) + (sum >> 16);
        }

        return (ushort)~sum;
    }

    private static async Task<(bool Success, string Message)> SendTcpAsync(string destinationIp, KnockStep step)
    {
        if (!IPAddress.TryParse(destinationIp, out _))
        {
            return (false, "آدرس IP نامعتبر است");
        }

        var port = Math.Clamp(step.Port, 1, 65535);

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(destinationIp, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(1000));

            if (completed == connectTask)
            {
                await connectTask;
                return (true, $"TCP SYN به {destinationIp}:{port} ارسال شد (اتصال برقرار شد)");
            }

            return (true, $"TCP SYN به {destinationIp}:{port} ارسال شد (timeout — برای knocking کافی است)");
        }
        catch (SocketException ex)
        {
            return (true, $"TCP به {destinationIp}:{port} — {ex.SocketErrorCode} (بسته ارسال شد)");
        }
        catch (Exception ex)
        {
            return (false, $"خطا در TCP: {ex.Message}");
        }
    }

    private static async Task<(bool Success, string Message)> SendUdpAsync(string destinationIp, KnockStep step)
    {
        if (!IPAddress.TryParse(destinationIp, out var ipAddress))
        {
            return (false, "آدرس IP نامعتبر است");
        }

        var port = Math.Clamp(step.Port, 1, 65535);

        try
        {
            var data = BuildPayload(step, 65507);
            if (data.Length == 0)
            {
                data = new byte[] { 0x00 };
            }

            using var client = new UdpClient();
            await client.SendAsync(data, data.Length, new IPEndPoint(ipAddress, port));
            return (true, $"UDP {data.Length} بایت به {destinationIp}:{port} ارسال شد");
        }
        catch (FormatException ex)
        {
            return (false, $"محتوای هگز نامعتبر: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"خطا در UDP: {ex.Message}");
        }
    }

    private static byte[] BuildPayload(KnockStep step, int maxSize)
        => PayloadBuilder.Build(step.PayloadMode, step.PayloadContent, step.PayloadSize, maxSize);
}
