using Avalonia.Controls;
using ShoppingListApp.ViewModels;

namespace ShoppingListApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}