using System;
using System.Linq;
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
        private bool _readingActive;

        public PeakCanReceiver(ushort pcanHandle)
        {
            _pcanHandle = pcanHandle;
        }

        public bool Start()
        {
            var status = PCANBasic.Initialize(_pcanHandle, TPCANBaudrate.PCAN_BAUD_500K);
            if (status != TPCANStatus.PCAN_ERROR_OK)
                return false;

            _readingActive = true;
            _readThread = new Thread(ReadLoop) { IsBackground = true };
            _readThread.Start();
            return true;
        }

        private void ReadLoop()
        {
            while (_readingActive)
            {
                TPCANMsg canMsg;
                TPCANTimestamp canTimestamp;
                var status = PCANBasic.Read(_pcanHandle, out canMsg, out canTimestamp);

                if (status == TPCANStatus.PCAN_ERROR_OK)
                {
                    var frame = new CanFrame
                    {
                        Id = canMsg.ID,
                        IsExtended = (canMsg.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) != 0,
                        Dlc = canMsg.LEN,
                        Data = canMsg.DATA.Take(canMsg.LEN).ToArray(),
                        Timestamp = DateTime.Now
                    };

                    FrameReceived?.Invoke(frame);
                }
                else if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
                {
                    // optional logging
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
    }
}
