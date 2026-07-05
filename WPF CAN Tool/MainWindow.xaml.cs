using System.Windows;
using System.Collections.Generic;
using Peak.Can.Basic;
using WPF_CAN_Tool.Pages;
using WPF_CAN_Tool.Models;
using System.IO;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Linq;

namespace WPF_CAN_Tool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // DBC configuration
        private Dictionary<uint, DbcMessage> _dbcMessages = new Dictionary<uint, DbcMessage>();
        private CanFrameDecoder? _frameDecoder;
        private HashSet<uint> _filterCanIds = new HashSet<uint>();
        internal const uint TsPlaceholderMessageId = 0x710;
        internal const uint SdcPlaceholderMessageId = 0x711;

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
            SetSdcNodeStates(127);

            MessageBoxResult modeResult = MessageBox.Show(
                "Run in TEST mode with simulated data?\n\nChoose No for normal hardware mode.",
                "Select startup mode",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.Yes);

            if (modeResult == MessageBoxResult.Yes)
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
                AddPlaceholderMessages();
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
                {
                    _carState.APPSStatus = appsStatus > 0;
                }
                if (signals.TryGetValue("BOTS_Status", out var botsStatus))
                {
                    _carState.BOTSStatus = botsStatus > 0;
                }

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

                UpdateTsState(signals);
                UpdateSdcState(signals);
            });
        }

        private void UpdateTsState(Dictionary<string, double> signals)
        {
            if (TryGetAnySignal(signals, out var voltage, "AccumulatorVoltage", "Accumulator_Voltage", "PackVoltage", "Pack_Voltage", "TS_Voltage"))
                _carState.AccumulatorVoltage = voltage;
            if (TryGetAnySignal(signals, out var current, "AccumulatorCurrent", "Accumulator_Current", "PackCurrent", "Pack_Current", "TS_Current"))
                _carState.AccumulatorCurrent = current;
            if (TryGetAnySignal(signals, out var minCellTemp, "MinCellTemp", "Min_Cell_Temp", "Minimum_Cell_Temp"))
                _carState.MinCellTemp = minCellTemp;
            if (TryGetAnySignal(signals, out var avgCellTemp, "AvgCellTemp", "Average_Cell_Temp", "Avg_Cell_Temp"))
                _carState.AvgCellTemp = avgCellTemp;
            if (TryGetAnySignal(signals, out var maxCellTemp, "MaxCellTemp", "Max_Cell_Temp", "Maximum_Cell_Temp"))
                _carState.MaxCellTemp = maxCellTemp;
            if (TryGetAnySignal(signals, out var hottestCellId, "HottestCellId", "Hottest_Cell_Id", "Max_Cell_Id"))
                _carState.HottestCellId = (int)hottestCellId;
            if (signals.TryGetValue("Torque_Command", out var torqueCommand))
                _carState.TorqueCommand = torqueCommand;
            if (signals.TryGetValue("Speed_Command", out var speedCommand))
                _carState.SpeedCommand = speedCommand;
        }

        private void UpdateSdcState(Dictionary<string, double> signals)
        {
            if (signals.TryGetValue("SDC_Node_Number", out var failedNodeNumber))
            {
                SetSdcNodeStates((int)failedNodeNumber);
                return;
            }

            if (TryGetAnySignal(signals, out var lvmsOk, "LVMS_Status", "LVMS_Ok", "LvmsOk", "LVMS"))
                _carState.LvmsOk = lvmsOk > 0;
            if (TryGetAnySignal(signals, out var bspdOk, "BSPD_Status", "BSPD_Ok", "BspdOk", "BSPD"))
                _carState.BspdOk = bspdOk > 0;
            if (TryGetAnySignal(signals, out var killSwitchOk, "KillSwitch_Status", "Kill_Switch_Status", "KillSwitchOk", "EstopOk", "EStop_Status"))
                _carState.KillSwitchOk = killSwitchOk > 0;
            if (TryGetAnySignal(signals, out var inertiaOk, "Inertia_Status", "Inertia_Ok", "InertiaOk", "Inertia_Switch_Status"))
                _carState.InertiaOk = inertiaOk > 0;
            if (TryGetAnySignal(signals, out var botsOk, "BOTS_Status", "BOTS_Ok", "BotsOk", "BOTS"))
                _carState.BotsOk = botsOk > 0;
            if (TryGetAnySignal(signals, out var appsOk, "APPS_Status", "APPS_Ok", "AppsOk", "APPS"))
                _carState.AppsOk = appsOk > 0;
            if (TryGetAnySignal(signals, out var lhsSbOk, "LHS_SB_Status", "LHS_SDB_Status", "LhsSbOk", "LHS_SB"))
                _carState.LhsSbOk = lhsSbOk > 0;
            if (TryGetAnySignal(signals, out var rhsSbOk, "RHS_SB_Status", "RHS_SDB_Status", "RhsSbOk", "RHS_SB"))
                _carState.RhsSbOk = rhsSbOk > 0;
            if (TryGetAnySignal(signals, out var latchboardOk, "Latchboard_Status", "Latchboard_Ok", "LatchboardOk"))
                _carState.LatchboardOk = latchboardOk > 0;
            if (TryGetAnySignal(signals, out var dcOk, "DC_Status", "DC_Ok", "DcOk", "DC"))
                _carState.DcOk = dcOk > 0;
            if (TryGetAnySignal(signals, out var hvdOk, "HVD_Status", "HVD_Ok", "HvdOk", "HVD"))
                _carState.HvdOk = hvdOk > 0;
            if (TryGetAnySignal(signals, out var mcOk, "MC_Status", "MC_Ok", "McOk", "Motor_Controller_Status"))
                _carState.McOk = mcOk > 0;
            if (TryGetAnySignal(signals, out var selfLatchOk, "SelfLatch_Status", "Self_Latch_Status", "SelfLatchOk"))
                _carState.SelfLatchOk = selfLatchOk > 0;
            if (TryGetAnySignal(signals, out var tsalSwitchOk, "TSAL_Switch_Status", "TSAL_Status", "TsalSwitchOk"))
                _carState.TsalSwitchOk = tsalSwitchOk > 0;
            if (TryGetAnySignal(signals, out var tsmsOk, "TSMS_Status", "TSMS_Ok", "TsmsOk", "TSMS"))
                _carState.TsmsOk = tsmsOk > 0;
            if (TryGetAnySignal(signals, out var prechargeOk, "Precharge_Status", "Precharge_Ok", "PrechargeOk"))
                _carState.PrechargeOk = prechargeOk > 0;
        }

        private void SetSdcNodeStates(int failedNodeIndex)
        {
            bool allOk = failedNodeIndex < 0 || failedNodeIndex >= 16;

            _carState.LvmsOk = allOk || failedNodeIndex != 0;
            _carState.BspdOk = allOk || failedNodeIndex != 1;
            _carState.KillSwitchOk = allOk || failedNodeIndex != 2;
            _carState.InertiaOk = allOk || failedNodeIndex != 3;
            _carState.BotsOk = allOk || failedNodeIndex != 4;
            _carState.AppsOk = allOk || failedNodeIndex != 5;
            _carState.LhsSbOk = allOk || failedNodeIndex != 6;
            _carState.RhsSbOk = allOk || failedNodeIndex != 7;
            _carState.LatchboardOk = allOk || failedNodeIndex != 8;
            _carState.DcOk = allOk || failedNodeIndex != 9;
            _carState.HvdOk = allOk || failedNodeIndex != 10;
            _carState.McOk = allOk || failedNodeIndex != 11;
            _carState.SelfLatchOk = allOk || failedNodeIndex != 12;
            _carState.TsalSwitchOk = allOk || failedNodeIndex != 13;
            _carState.TsmsOk = allOk || failedNodeIndex != 14;
            _carState.PrechargeOk = allOk || failedNodeIndex != 15;
        }

        private static bool TryGetAnySignal(Dictionary<string, double> signals, out double value, params string[] names)
        {
            foreach (var name in names)
            {
                if (signals.TryGetValue(name, out value))
                    return true;
            }

            value = 0;
            return false;
        }

        private void AddPlaceholderMessages()
        {
            if (!ContainsAnySignal("AccumulatorVoltage", "Accumulator_Voltage", "PackVoltage", "Pack_Voltage", "TS_Voltage"))
            {
                _dbcMessages[TsPlaceholderMessageId] = new DbcMessage
                {
                    Id = TsPlaceholderMessageId,
                    Name = "TS_Placeholder",
                    Length = 8,
                    Sender = "Test",
                    Signals = new List<DbcSignal>
                    {
                        CreateSignal("AccumulatorVoltage", 0, 16, false, 0.1),
                        CreateSignal("AccumulatorCurrent", 16, 16, true, 0.1),
                        CreateSignal("MinCellTemp", 32, 8, false, 1),
                        CreateSignal("AvgCellTemp", 40, 8, false, 1),
                        CreateSignal("MaxCellTemp", 48, 8, false, 1),
                        CreateSignal("HottestCellId", 56, 8, false, 1)
                    }
                };
            }

            if (!ContainsAnySignal("SDC_Node_Number", "LVMS_Status", "LvmsOk", "BSPD_Status", "BspdOk"))
            {
                _dbcMessages[SdcPlaceholderMessageId] = new DbcMessage
                {
                    Id = SdcPlaceholderMessageId,
                    Name = "SDC_Placeholder",
                    Length = 1,
                    Sender = "Test",
                    Signals = new List<DbcSignal>
                    {
                        CreateSignal("SDC_Node_Number", 0, 8, false, 1)
                    }
                };
            }
        }

        private bool ContainsAnySignal(params string[] signalNames)
        {
            return _dbcMessages.Values.Any(message =>
                message.Signals.Any(signal => signalNames.Contains(signal.Name)));
        }

        private static DbcSignal CreateSignal(string name, int startBit, int length, bool isSigned, double scale)
        {
            return new DbcSignal
            {
                Name = name,
                StartBit = startBit,
                Length = length,
                IsLittleEndian = true,
                IsSigned = isSigned,
                Scale = scale,
                Offset = 0
            };
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _receiver?.Dispose();
            _receiver = null;
            base.OnClosed(e);
        }
    }
}
