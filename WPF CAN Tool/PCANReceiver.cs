using System;
using System.Linq;
using System.Text;
using System.Threading;
using Peak.Can.Basic;

namespace WPF_CAN_Tool
{
    public class CanFrame
    {
        public uint Id { get; set; }
        public bool IsExtended { get; set; }
        public byte Dlc { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime Timestamp { get; set; }
    }

    public interface ICanReceiver : IDisposable
    {
        event Action<CanFrame>? FrameReceived;
        bool Start();
        void Stop();
    }

    public class PeakCanReceiver : ICanReceiver
    {
        public event Action<CanFrame>? FrameReceived;

        private readonly ushort _pcanHandle;
        private Thread? _readThread;
        private volatile bool _readingActive;

        public PeakCanReceiver(ushort pcanHandle)
        {
            _pcanHandle = pcanHandle;
        }

        public bool Start()
        {
            TPCANStatus status;

            try
            {
                status = PCANBasic.Initialize(_pcanHandle, TPCANBaudrate.PCAN_BAUD_500K);
            }
            catch (DllNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"PCANBasic.dll not found or not loadable: {ex.Message}");
                return false;
            }
            catch (BadImageFormatException ex)
            {
                System.Diagnostics.Debug.WriteLine($"PCANBasic.dll architecture mismatch: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PCAN init exception: {ex.Message}");
                return false;
            }

            if (status != TPCANStatus.PCAN_ERROR_OK)
            {
                System.Diagnostics.Debug.WriteLine($"PCAN init failed: {FormatPcanError(status)}");
                return false;
            }

            PCANBasic.Reset(_pcanHandle);

            _readingActive = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
            return true;
        }

        private void ReadLoop()
        {
            while (_readingActive)
            {
                var status = PCANBasic.Read(_pcanHandle, out TPCANMsg canMsg, out TPCANTimestamp canTimestamp);

                if (status == TPCANStatus.PCAN_ERROR_OK)
                {
                    byte dlc = Math.Min(canMsg.LEN, (byte)canMsg.DATA.Length);
                    bool isExtended = (canMsg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) != 0;
                    uint id = isExtended ? (canMsg.ID | 0x80000000u) : canMsg.ID;

                    var frame = new CanFrame
                    {
                        Id = id,
                        IsExtended = isExtended,
                        Dlc = dlc,
                        Data = canMsg.DATA.Take(dlc).ToArray(),
                        Timestamp = DateTime.Now
                    };

                    FrameReceived?.Invoke(frame);
                }
                else if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                {
                    System.Diagnostics.Debug.WriteLine($"PCAN read error: {FormatPcanError(status)}");
                }

                Thread.Sleep(5);
            }
        }

        public void Stop()
        {
            _readingActive = false;
            _readThread?.Join(500);
            PCANBasic.Uninitialize(_pcanHandle);
        }

        public void Dispose() => Stop();

        private static string FormatPcanError(TPCANStatus status)
        {
            var buffer = new StringBuilder(256);
            var textStatus = PCANBasic.GetErrorText(status, 0x09, buffer);

            return textStatus == TPCANStatus.PCAN_ERROR_OK
                ? $"{status} - {buffer}"
                : status.ToString();
        }
    }
}
