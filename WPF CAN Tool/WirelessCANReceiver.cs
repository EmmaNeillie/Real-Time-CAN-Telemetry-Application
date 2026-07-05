using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_CAN_Tool
{
    public class WirelessCanReceiver : ICanReceiver
    {
        public event Action<CanFrame>? FrameReceived;

        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _readTask;

        private const string DefaultHost = "192.168.1.100"; // TODO: Make configurable
        private const int DefaultPort = 5000; // TODO: Make configurable

        public bool Start()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _tcpClient = new TcpClient();

                // Connect to the wireless device
                _tcpClient.Connect(DefaultHost, DefaultPort);
                _networkStream = _tcpClient.GetStream();

                // Start background read loop
                _readTask = Task.Run(() => ReadFramesAsync(_cancellationTokenSource.Token));

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting WirelessCanReceiver: {ex.Message}");
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();

            try
            {
                _readTask?.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Task was cancelled or timed out
            }

            _networkStream?.Dispose();
            _tcpClient?.Dispose();

            _networkStream = null;
            _tcpClient = null;
            _readTask = null;
        }

        public void Dispose() => Stop();

        private async Task ReadFramesAsync(CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (!cancellationToken.IsCancellationRequested && _networkStream != null)
                {
                    int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        // Connection closed
                        break;
                    }

                    // Parse frames from received data
                    ParseAndDispatchFrames(buffer, bytesRead);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading from wireless device: {ex.Message}");
            }
        }

        private void ParseAndDispatchFrames(byte[] buffer, int length)
        {
            // Parse CAN frames from the buffer
            // Format depends on your wireless device protocol
            // This is a basic example assuming DLC + ID + Data format
            
            for (int i = 0; i < length - 4; i++)
            {
                // Simple frame format: [DLC(1) | ID_HIGH(1) | ID_LOW(1) | DATA(DLC)]
                int dlc = buffer[i];
                if (dlc > 8) continue;

                uint id = ((uint)buffer[i + 1] << 8) | buffer[i + 2];
                byte[] data = new byte[dlc];
                Array.Copy(buffer, i + 3, data, 0, dlc);

                var frame = new CanFrame
                {
                    Id = id,
                    Data = data,
                    Dlc = (byte)dlc
                };

                FrameReceived?.Invoke(frame);

                i += dlc + 2;
            }
        }
    }
}
