using System.Text;
using KnockingTool.Models;

namespace KnockingTool.Services;

public static class PayloadBuilder
{
    public static byte[] Build(PayloadMode mode, string? content, int targetSize, int maxSize)
    {
        content ??= string.Empty;
        targetSize = Math.Clamp(targetSize, 0, maxSize);

        var parsed = mode switch
        {
            PayloadMode.Text => Encoding.ASCII.GetBytes(content),
            PayloadMode.Hex => TryParseHex(content),
            _ => Array.Empty<byte>()
        };

        var size = targetSize > 0 ? targetSize : parsed.Length;
        size = Math.Clamp(size, 0, maxSize);

        if (size == 0)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[size];
        switch (mode)
        {
            case PayloadMode.Zero:
                break;
            case PayloadMode.Random:
                Random.Shared.NextBytes(buffer);
                break;
            case PayloadMode.Text:
            case PayloadMode.Hex:
                if (parsed.Length > 0)
                {
                    Array.Copy(parsed, buffer, Math.Min(parsed.Length, size));
                }

                break;
        }

        return buffer;
    }

    private static byte[] TryParseHex(string content)
    {
        var hex = content.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        if (hex.Length == 0)
        {
            return Array.Empty<byte>();
        }

        if (hex.Length % 2 != 0)
        {
            throw new FormatException("طول رشته هگز باید زوج باشد");
        }

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(hex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
        }

        return bytes;
    }
}
