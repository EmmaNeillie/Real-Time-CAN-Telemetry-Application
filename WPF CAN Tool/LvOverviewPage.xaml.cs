using System.Windows.Controls;

namespace WPF_CAN_Tool.Pages;

public partial class LvOverviewPage : Page
{
    public LvOverviewPage(CarStateViewModel carState)
    {
        InitializeComponent();
        DataContext = carState;
    }
}
