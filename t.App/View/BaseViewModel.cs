using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace t.App.View;

public class BaseViewModel : INotifyPropertyChanged
{
    protected readonly ILogger logger;

    public BaseViewModel(ILogger logger)
    {
        this.logger = logger;
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    }
    protected bool SetProperty<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
