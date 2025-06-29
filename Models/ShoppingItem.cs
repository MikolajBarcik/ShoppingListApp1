using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShoppingListApp.Models;

public class ShoppingItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private ItemStatus _status = ItemStatus.None;
    private bool _isBought = false;

    public string Name 
    { 
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public ItemStatus Status 
    { 
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsBought
    {
        get => _isBought;
        set => SetProperty(ref _isBought, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}