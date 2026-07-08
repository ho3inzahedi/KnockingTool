using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KnockingTool.Models;

public class KnockStep : INotifyPropertyChanged
{
    private KnockProtocol _protocol = KnockProtocol.Tcp;
    private int _port = 80;
    private int _payloadSize = 32;
    private bool _includeIpHeader;
    private int _delayMs = 500;

    public int Order { get; set; }

    public KnockProtocol Protocol
    {
        get => _protocol;
        set => SetField(ref _protocol, value);
    }

    public int Port
    {
        get => _port;
        set => SetField(ref _port, value);
    }

    public int PayloadSize
    {
        get => _payloadSize;
        set => SetField(ref _payloadSize, value);
    }

    public bool IncludeIpHeader
    {
        get => _includeIpHeader;
        set => SetField(ref _includeIpHeader, value);
    }

    public int DelayMs
    {
        get => _delayMs;
        set => SetField(ref _delayMs, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
