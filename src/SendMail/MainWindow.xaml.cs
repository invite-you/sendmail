using System.Windows;
using SendMail.ViewModels;

namespace SendMail;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
