using System.Windows.Controls;

namespace WPF_CAN_Tool.Pages;

public partial class SdcPage : Page
{
    private readonly CarStateViewModel _carState;

    public SdcPage(CarStateViewModel carState)
    {
        InitializeComponent();
        _carState = carState;
        DataContext = _carState;
    }
}
