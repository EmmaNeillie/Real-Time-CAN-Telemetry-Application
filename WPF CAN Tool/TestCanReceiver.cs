using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPF_CAN_Tool.Models;

namespace WPF_CAN_Tool
{
    /// <summary>
    /// Mock CAN receiver for testing purposes - generates simulated CAN data for all DBC messages
    /// </summary>
    public class TestCanReceiver : ICanReceiver
    {
        public event Action<CanFrame>? FrameReceived;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _dataGenerationTask;
        private Random _random = new Random();
        private readonly Dictionary<uint, DbcMessage> _dbcMessages;

        // Test data parameters
        private double _acceleratorValue = 0;
        private double _brakeValue = 0;
        private double _wheelSpeedFL = 0;
        private double _wheelSpeedFR = 0;
        private double _wheelSpeedRL = 0;
        private double _wheelSpeedRR = 0;
        private double _motorRpm = 0;
        private bool _appsStatus = true;
        private bool _botsStatus = true;
        private int _energySetting = 0;
        private short _torqueCommand = 0;
        private short _speedCommand = 0;

        public TestCanReceiver(Dictionary<uint, DbcMessage>? dbcMessages = null)
        {
            _dbcMessages = dbcMessages ?? new Dictionary<uint, DbcMessage>();
        }

        public bool Start()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _dataGenerationTask = Task.Run(() => GenerateTestDataAsync(_cancellationTokenSource.Token));
                System.Diagnostics.Debug.WriteLine("? Test CAN Receiver started");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error starting Test CAN Receiver: {ex.Message}");
                return false;
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();

            try
            {
                _dataGenerationTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Task was cancelled
            }

            _dataGenerationTask = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public void Dispose() => Stop();

        private async Task GenerateTestDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.Now;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Generate realistic test data with sine waves and random variations
                    double time = (DateTime.Now - startTime).TotalSeconds;

                    // Accelerator: sine wave 0-100%
                    _acceleratorValue = 50 + 50 * Math.Sin(time * 0.5);
                    SendAcceleratorFrame();
                    await Task.Delay(50, cancellationToken);

                    // Brake: sine wave 0-100%
                    _brakeValue = 30 + 30 * Math.Sin(time * 0.3 + 1.5);
                    SendBrakeFrame();
                    await Task.Delay(50, cancellationToken);

                    // Status: toggle every 5 seconds
                    _appsStatus = (int)(time / 5) % 2 == 0;
                    _botsStatus = (int)(time / 5) % 2 == 0;
                    SendStatusFrame();
                    await Task.Delay(50, cancellationToken);

                    // Wheel speeds: varying values (all 4 wheels)
                    _wheelSpeedFL = 25 + 20 * Math.Sin(time * 0.7);
                    _wheelSpeedFR = 26 + 19 * Math.Sin(time * 0.72);
                    _wheelSpeedRL = 24 + 21 * Math.Sin(time * 0.68);
                    _wheelSpeedRR = 25 + 20 * Math.Sin(time * 0.75);
                    SendWheelspeedFrames();
                    await Task.Delay(50, cancellationToken);

                    // Motor RPM: varying values
                    _motorRpm = 3000 + 2000 * Math.Sin(time * 0.4);
                    SendMotorRpmFrame();
                    await Task.Delay(50, cancellationToken);

                    // Energy setting: cycle through 0-3
                    _energySetting = (int)(time / 2) % 4;
                    SendEnergySettingFrame();
                    await Task.Delay(50, cancellationToken);

                    // Inverter commands: varying torque and speed
                    _torqueCommand = (short)(15000 * Math.Sin(time * 0.4));
                    _speedCommand = (short)(5000 * Math.Sin(time * 0.5 + 1.0));
                    SendInverterCommandFrame();
                    await Task.Delay(50, cancellationToken);

                    // Inverter return varies with motor RPM
                    SendInverterReturnFrame();
                    await Task.Delay(50, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in test data generation: {ex.Message}");
            }
        }

        private void SendAcceleratorFrame()
        {
            uint messageId = GetMessageId("Accelerator_Percentage", 256);

            // Signal: Accelerator_Percentage
            // Signal: Accelerator_Percentage at bit 0, 16 bits, scale 0.00305185, offset 0
            // Raw value = Actual / Scale
            ushort rawValue = (ushort)Math.Max(0, Math.Min(65535, _acceleratorValue / 0.00305185));
            byte[] data = BitConverter.GetBytes(rawValue);

            var frame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8] { data[0], data[1], 0, 0, 0, 0, 0, 0 },
                Dlc = 2
            };

            FrameReceived?.Invoke(frame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame.Id:X3} Accel, Data: {BitConverter.ToString(frame.Data, 0, frame.Dlc)}");
        }

        private void SendBrakeFrame()
        {
            uint messageId = GetMessageId("Brake_Percentage", 261);

            // Signal: Brake_Percentage
            // Signal: Brake_Percentage at bit 0, 8 bits, scale 0.392157, offset 0
            byte rawValue = (byte)Math.Max(0, Math.Min(255, _brakeValue / 0.392157));

            var frame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8] { rawValue, 0, 0, 0, 0, 0, 0, 0 },
                Dlc = 1
            };

            FrameReceived?.Invoke(frame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame.Id:X3} Brake, Data: {BitConverter.ToString(frame.Data, 0, frame.Dlc)}");
        }

        private void SendStatusFrame()
        {
            uint messageId = GetMessageId("BOTS_Status", 272);

            // Bit 0: BOTS_Status (1 bit)
            // Bit 1: APPS_Status (1 bit)
            byte botsStatus = _botsStatus ? (byte)1 : (byte)0;
            byte appsStatus = _appsStatus ? (byte)1 : (byte)0;

            byte data0 = (byte)(((botsStatus & 1) << 7) | ((appsStatus & 1) << 6));

            var frame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8] { data0, 0, 0, 0, 0, 0, 0, 0 },
                Dlc = 1
            };

            FrameReceived?.Invoke(frame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame.Id:X3} Status (APPS={_appsStatus}, BOTS={_botsStatus}), Data: {BitConverter.ToString(frame.Data, 0, frame.Dlc)}");
        }

        private void SendWheelspeedFrames()
        {
            uint messageId = GetMessageId("Wheelspeed_Mux", 768);

            // Wheelspeed (multiplexed)
            // Send separate frames for each wheel with Mux value

            // FL Wheelspeed (Mux = 0)
            short flRaw = (short)Math.Max(-32768, Math.Min(32767, _wheelSpeedFL));
            var flFrame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    0,  // Mux = 0 for FL
                    (byte)(flRaw & 0xFF),
                    (byte)((flRaw >> 8) & 0xFF),
                    60, // FL Brake temp
                    0, 0, 0, 0
                },
                Dlc = 4
            };
            FrameReceived?.Invoke(flFrame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{flFrame.Id:X3} WheelFL (Mux=0), Data: {BitConverter.ToString(flFrame.Data, 0, flFrame.Dlc)}");

            // FR Wheelspeed (Mux = 1)
            short frRaw = (short)Math.Max(-32768, Math.Min(32767, _wheelSpeedFR));
            var frFrame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    1,  // Mux = 1 for FR
                    (byte)(frRaw & 0xFF),
                    (byte)((frRaw >> 8) & 0xFF),
                    58, // FR Brake temp
                    0, 0, 0, 0
                },
                Dlc = 4
            };
            FrameReceived?.Invoke(frFrame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frFrame.Id:X3} WheelFR (Mux=1), Data: {BitConverter.ToString(frFrame.Data, 0, frFrame.Dlc)}");

            // RL Wheelspeed (Mux = 2)
            short rlRaw = (short)Math.Max(-32768, Math.Min(32767, _wheelSpeedRL));
            var rlFrame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    2,  // Mux = 2 for RL
                    (byte)(rlRaw & 0xFF),
                    (byte)((rlRaw >> 8) & 0xFF),
                    62, // RL Brake temp
                    0, 0, 0, 0
                },
                Dlc = 4
            };
            FrameReceived?.Invoke(rlFrame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{rlFrame.Id:X3} WheelRL (Mux=2), Data: {BitConverter.ToString(rlFrame.Data, 0, rlFrame.Dlc)}");

            // RR Wheelspeed (Mux = 3)
            short rrRaw = (short)Math.Max(-32768, Math.Min(32767, _wheelSpeedRR));
            var rrFrame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    3,  // Mux = 3 for RR
                    (byte)(rrRaw & 0xFF),
                    (byte)((rrRaw >> 8) & 0xFF),
                    59, // RR Brake temp
                    0, 0, 0, 0
                },
                Dlc = 4
            };
            FrameReceived?.Invoke(rrFrame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{rrFrame.Id:X3} WheelRR (Mux=3), Data: {BitConverter.ToString(rrFrame.Data, 0, rrFrame.Dlc)}");
        }

        private void SendMotorRpmFrame()
        {
            uint messageId = GetMessageId("N_actual", 1921);

            // Inverter_Return
            // Mux selector at bit 0 (value 48 for N_actual)
            // Signal: N_actual at bit 8, 16 bits, scale 0.183111, offset 0
            short rpmRaw = (short)Math.Max(-32768, Math.Min(32767, _motorRpm / 0.183111));

            var frame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    48,  // Inverter_Return_Mux = 48 for N_actual
                    (byte)(rpmRaw & 0xFF),
                    (byte)((rpmRaw >> 8) & 0xFF),
                    0, 0, 0, 0, 0
                },
                Dlc = 3
            };

            FrameReceived?.Invoke(frame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame.Id:X3} MotorRpm (Mux=48), Data: {BitConverter.ToString(frame.Data, 0, frame.Dlc)}");
        }

        private void SendEnergySettingFrame()
        {
            uint messageId = GetMessageId("Energy_Setting_Request", 2147484160);

            // Energy_Setting
            // Signal: Energy_Setting_Request at bit 0, 2 bits, scale 1, offset 0
            byte data0 = (byte)(_energySetting & 0x3);

            var frame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8] { data0, 0, 0, 0, 0, 0, 0, 0 },
                Dlc = 1
            };

            FrameReceived?.Invoke(frame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame.Id:X8} Energy (Setting={_energySetting}), Data: {BitConverter.ToString(frame.Data, 0, frame.Dlc)}");
        }

        private void SendInverterCommandFrame()
        {
            uint messageId = GetMessageId("Inverter_Command_Mux", 1537);

            // Inverter_Command
            // This is multiplexed, sending Torque_Command variant (Mux = 144)
            byte muxValue = 144;  // Mux for Torque_Command
            byte[] torqueBytes = BitConverter.GetBytes(_torqueCommand);

            var frame = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    muxValue,  // Mux selector
                    torqueBytes[0],
                    torqueBytes[1],
                    0, 0, 0, 0, 0
                },
                Dlc = 3
            };

            FrameReceived?.Invoke(frame);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame.Id:X3} InvCmd Torque (Mux=144), Data: {BitConverter.ToString(frame.Data, 0, frame.Dlc)}");

            // Also send Speed_Command variant (Mux = 49)
            muxValue = 49;  // Mux for Speed_Command
            byte[] speedBytes = BitConverter.GetBytes(_speedCommand);

            var frame2 = new CanFrame
            {
                Id = messageId,
                Data = new byte[8]
                {
                    muxValue,  // Mux selector
                    speedBytes[0],
                    speedBytes[1],
                    0, 0, 0, 0, 0
                },
                Dlc = 3
            };

            FrameReceived?.Invoke(frame2);
            System.Diagnostics.Debug.WriteLine($"? ID: 0x{frame2.Id:X3} InvCmd Speed (Mux=49), Data: {BitConverter.ToString(frame2.Data, 0, frame2.Dlc)}");
        }

        private void SendInverterReturnFrame()
        {
            // Already handled in SendMotorRpmFrame() for mux value 48
            // This could be extended to send other mux values if needed
        }

        private uint GetMessageId(string signalName, uint fallbackId)
        {
            var message = _dbcMessages.Values.FirstOrDefault(m =>
                m.Signals.Any(s => s.Name.Equals(signalName, StringComparison.OrdinalIgnoreCase)));

            return message?.Id ?? fallbackId;
        }
    }
}
