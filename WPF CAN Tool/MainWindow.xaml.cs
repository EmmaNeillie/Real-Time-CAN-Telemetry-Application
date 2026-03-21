using System.Windows;
using System.Collections.Generic;
using Peak.Can.Basic;
using WPF_CAN_Tool.Pages;

namespace WPF_CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // List of CAN IDs to filter TODO change values for CAN IDs
        private readonly HashSet<uint> filterCanIds = new HashSet<uint> { 0x100, 0x200, 0x300 };

        // PCAN channel
        private ushort pcanHandle = PCANBasic.PCAN_USBBUS1;

        private bool wired = false; // Set to true for Peak CAN device, false for datalogger

        private ICanReceiver? _receiver;

        private CarStateViewModel _carState = new CarStateViewModel();

        public MainWindow()
        {
            InitializeComponent();

            // Ask the user at startup whether to use the wired (Peak) connection.
            MessageBoxResult result = MessageBox.Show(
                "Use wired connection (Peak device)?",
                "Select connection type",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            wired = (result == MessageBoxResult.Yes);

            if (wired)
            {
                ConnectToPeakDevice();
            }
            else
            {
                ConnectToDatalogger();
            }

            MainFrame.Navigate(new LvOverviewPage(_carState));
        }

        private void LvOverview_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LvOverviewPage(_carState));
        }

        private void Pedals_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PedalsPage(_carState));
        }

        private void Sdc_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SdcPage(_carState));
        }

        private void TsOverview_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new TsOverviewPage(_carState));
        }

        private void Wheelspeeds_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new WheelspeedsPage(_carState));
        }

        private void ConnectToPeakDevice()
        {
            _receiver = new PeakCanReceiver(pcanHandle);
            _receiver.FrameReceived += OnWiredFrameReceived;

            if (!_receiver.Start())
            {
                MessageBox.Show("PCAN init failed.");
                _receiver.FrameReceived -= OnWiredFrameReceived;
                _receiver.Dispose();
                _receiver = null;
            }
        }

        private void OnWiredFrameReceived(CanFrame frame)
        {
            // Filter by CAN ID
            if (!filterCanIds.Contains(frame.Id))
                return;

            string msgData = BitConverter.ToString(frame.Data);
            string output = $"ID: 0x{frame.Id:X3}, Data: {msgData}";
            System.Diagnostics.Debug.WriteLine(output);

            // Update the UI here using Dispatcher.Invoke/BeginInvoke
        }

        private void ConnectToDatalogger()
        {
            _receiver = new WirelessCanReceiver();  
            _receiver.FrameReceived += OnWirelessFrameReceived;

            if (!_receiver.Start()) {
                MessageBox.Show("Failed to connect to datalogger.");
                _receiver.FrameReceived -= OnWirelessFrameReceived;
                _receiver.Dispose();
                _receiver = null;
            }
        }

        private void OnWirelessFrameReceived(CanFrame frame)
        {
            // Filter by CAN ID
            if (!filterCanIds.Contains(frame.Id))
                return;
            string msgData = BitConverter.ToString(frame.Data);
            string output = $"[Datalogger] ID: 0x{frame.Id:X3}, Data: {msgData}";
            System.Diagnostics.Debug.WriteLine(output);
            // Update the UI here using Dispatcher.Invoke/BeginInvoke
        }
        protected override void OnClosed(System.EventArgs e)
        {
            _receiver?.Dispose();
            _receiver = null;
            base.OnClosed(e);
        }
    }
}