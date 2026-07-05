using System.Windows;
using System.Collections.Generic;
using Peak.Can.Basic;
using WPF_CAN_Tool.Pages;
using WPF_CAN_Tool.Models;
using System.IO;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace WPF_CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Enable TEST mode - set to false to use real hardware
        private const bool TEST_MODE = true;

        // DBC configuration
        private Dictionary<uint, DbcMessage> _dbcMessages = new Dictionary<uint, DbcMessage>();
        private CanFrameDecoder? _frameDecoder;
        private HashSet<uint> _filterCanIds = new HashSet<uint>();

        // PCAN channel
        private ushort pcanHandle = PCANBasic.PCAN_USBBUS1;

        private bool wired = false; // Set to true for Peak CAN device, false for datalogger

        private ICanReceiver? _receiver;

        private CarStateViewModel _carState = new CarStateViewModel();

        private DbcFileManager _dbcFileManager = new DbcFileManager();

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            MainFrame.Navigate(new LvOverviewPage(_carState));
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            await InitializeDbcFileAsync();

            // Skip connection dialog in test mode
            if (TEST_MODE)
            {
                wired = false;
                ConnectToTestReceiver();
            }
            else
            {
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
            }
        }

        private async Task InitializeDbcFileAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("⏳ Initializing DBC file...");
                
                // Try to get DBC file (from GitHub or local)
                var (success, filePath, errorMessage) = await _dbcFileManager.GetDbcFileAsync();

                if (success && !string.IsNullOrEmpty(filePath))
                {
                    LoadDbcFile(filePath);
                }
                else
                {
                    // If both GitHub and local file fail, ask user to select
                    System.Diagnostics.Debug.WriteLine($"✗ DBC file fetch failed: {errorMessage}");
                    await HandleDbcFileSelectionAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ Error in InitializeDbcFileAsync: {ex.Message}");
                await HandleDbcFileSelectionAsync();
            }
        }

        private async Task HandleDbcFileSelectionAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBoxResult result = MessageBox.Show(
                    "Failed to fetch DBC file from GitHub and no local file found.\n\nWould you like to select a DBC file manually?",
                    "DBC File Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "DBC Files (*.dbc)|*.dbc|All Files (*.*)|*.*",
                        Title = "Select DBC File",
                        CheckFileExists = true
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        LoadDbcFile(openFileDialog.FileName);
                        
                        // Optionally save to local cache for future use
                        try
                        {
                            string localCachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CAN_Config.dbc");
                            File.Copy(openFileDialog.FileName, localCachePath, overwrite: true);
                            System.Diagnostics.Debug.WriteLine($"✓ Cached DBC file to: {localCachePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠ Could not cache DBC file: {ex.Message}");
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "No DBC file selected. The application will continue without signal decoding.",
                            "Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            });
        }

        private void LoadDbcFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"DBC file not found: {filePath}");
                }

                var parser = new DbcParser();
                _dbcMessages = parser.ParseDbcAdvanced(filePath);
                _frameDecoder = new CanFrameDecoder(_dbcMessages);

                // Populate filter with all message IDs from DBC
                _filterCanIds.Clear();
                foreach (var msgId in _dbcMessages.Keys)
                {
                    _filterCanIds.Add(msgId);
                }

                System.Diagnostics.Debug.WriteLine($"✓ Loaded {_dbcMessages.Count} CAN messages from: {Path.GetFileName(filePath)}");
                foreach (var msg in _dbcMessages.Values)
                {
                    System.Diagnostics.Debug.WriteLine($"  Message: 0x{msg.Id:X3} {msg.Name} with {msg.Signals.Count} signals");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading DBC file: {ex.Message}", "DBC Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"✗ Error loading DBC file: {ex.Message}");
            }
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
            _receiver.FrameReceived += OnFrameReceived;

            if (!_receiver.Start())
            {
                MessageBox.Show("PCAN init failed.");
                _receiver.FrameReceived -= OnFrameReceived;
                _receiver.Dispose();
                _receiver = null;
            }
        }

        private void ConnectToTestReceiver()
        {
            _receiver = new TestCanReceiver(_dbcMessages);
            _receiver.FrameReceived += OnFrameReceived;

            if (!_receiver.Start())
            {
                MessageBox.Show("Test receiver failed to start.");
                _receiver.FrameReceived -= OnFrameReceived;
                _receiver.Dispose();
                _receiver = null;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("📊 TEST MODE ENABLED - Using simulated CAN data");
                Title = "WPF CAN Tool - [TEST MODE]";
            }
        }

        private void OnFrameReceived(CanFrame frame)
        {
            // Filter by CAN ID
            if (_filterCanIds.Count > 0 && !_filterCanIds.Contains(frame.Id))
                return;

            // Decode frame using DBC
            if (_frameDecoder != null)
            {
                var decodedSignals = _frameDecoder.DecodeFrame(frame);
                UpdateCarState(decodedSignals);
            }

            string msgData = BitConverter.ToString(frame.Data);
            string output = $"ID: 0x{frame.Id:X3}, Data: {msgData}";
            System.Diagnostics.Debug.WriteLine(output);
        }

        private void ConnectToDatalogger()
        {
            _receiver = new WirelessCanReceiver();  
            _receiver.FrameReceived += OnFrameReceived;

            if (!_receiver.Start()) {
                MessageBox.Show("Failed to connect to datalogger.");
                _receiver.FrameReceived -= OnFrameReceived;
                _receiver.Dispose();
                _receiver = null;
            }
        }

        private void UpdateCarState(Dictionary<string, double> signals)
        {
            // Use Dispatcher to ensure UI thread safety
            Dispatcher.Invoke(() =>
            {
                if (signals.TryGetValue("Accelerator_Percentage", out var accel))
                    _carState.AcceleratorPercentage = accel;
                if (signals.TryGetValue("Brake_Percentage", out var brake))
                    _carState.BrakePercentage = brake;

                if (signals.TryGetValue("APPS_Status", out var appsStatus))
                    _carState.APPSStatus = appsStatus > 0;
                if (signals.TryGetValue("BOTS_Status", out var botsStatus))
                    _carState.BOTSStatus = botsStatus > 0;

                if (signals.TryGetValue("FL_Wheelspeed", out var flSpeed))
                    _carState.FLWheelspeed = flSpeed;
                if (signals.TryGetValue("FR_Wheelspeed", out var frSpeed))
                    _carState.FRWheelspeed = frSpeed;
                if (signals.TryGetValue("RL_Wheelspeed", out var rlSpeed))
                    _carState.RLWheelspeed = rlSpeed;
                if (signals.TryGetValue("RR_Wheelspeed", out var rrSpeed))
                    _carState.RRWheelspeed = rrSpeed;

                if (signals.TryGetValue("FL_BrakeTemp", out var flTemp))
                    _carState.FLBrakeTemp = flTemp;
                if (signals.TryGetValue("FR_BrakeTemp", out var frTemp))
                    _carState.FRBrakeTemp = frTemp;
                if (signals.TryGetValue("RL_BrakeTemp", out var rlTemp))
                    _carState.RLBrakeTemp = rlTemp;
                if (signals.TryGetValue("RR_BrakeTemp", out var rrTemp))
                    _carState.RRBrakeTemp = rrTemp;

                if (signals.TryGetValue("N_actual", out var rpm))
                    _carState.MotorRpm = rpm;

                if (signals.TryGetValue("Energy_Setting_Request", out var energySetting))
                    _carState.EnergySetting = (int)energySetting;
            });
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _receiver?.Dispose();
            _receiver = null;
            base.OnClosed(e);
        }
    }
}
