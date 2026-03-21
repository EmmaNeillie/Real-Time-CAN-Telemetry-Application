using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;

namespace WPF_CAN_Tool
{
    public class WirelessCanReceiver : ICanReceiver
    {
        public event Action<CanFrame>? FrameReceived;

        public bool Start()
        {
            // open socket / serial / stream
            // start background read loop
            return true;
        }

        public void Stop()
        {
            // close connection
        }

        public void Dispose() => Stop();
    }
}
