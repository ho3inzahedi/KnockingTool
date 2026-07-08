using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KnockingTool.Models;

public class RepeatSettings : INotifyPropertyChanged
{
    private int _count = 1;
    private int _intervalMs = 1000;

    /// <summary>1 = یک بار، 0 = بی‌نهایت تا توقف</summary>
    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

    public int IntervalMs
    {
        get => _intervalMs;
        set => SetField(ref _intervalMs, value);
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
