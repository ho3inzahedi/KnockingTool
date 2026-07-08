using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KnockingTool.Models;

public class KnockNode : INotifyPropertyChanged
{
    private string _name = "Node";
    private string _destinationIp = "127.0.0.1";

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string DestinationIp
    {
        get => _destinationIp;
        set => SetField(ref _destinationIp, value);
    }

    public ObservableCollection<KnockStep> Steps { get; } = [];

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
