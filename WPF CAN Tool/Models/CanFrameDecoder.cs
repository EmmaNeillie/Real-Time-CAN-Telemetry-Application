using System;
using System.Collections.Generic;

namespace WPF_CAN_Tool.Models
{
    public class CanFrameDecoder
    {
        private readonly Dictionary<uint, DbcMessage> _dbcMessages;

        public CanFrameDecoder(Dictionary<uint, DbcMessage> dbcMessages)
        {
            _dbcMessages = dbcMessages ?? new Dictionary<uint, DbcMessage>();
        }

        public Dictionary<string, double> DecodeFrame(CanFrame frame)
        {
            var decodedSignals = new Dictionary<string, double>();

            if (!_dbcMessages.ContainsKey(frame.Id))
                return decodedSignals;

            var message = _dbcMessages[frame.Id];

            foreach (var signal in message.Signals)
            {
                try
                {
                    double value = ExtractSignalValue(frame.Data, signal);
                    decodedSignals[signal.Name] = value;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error decoding signal {signal.Name}: {ex.Message}");
                }
            }

            return decodedSignals;
        }

        private double ExtractSignalValue(byte[] data, DbcSignal signal)
        {
            if (data == null || data.Length == 0)
                return 0;

            long rawValue = 0;

            if (signal.IsLittleEndian)
            {
                rawValue = ExtractLittleEndian(data, signal.StartBit, signal.Length);
            }
            else
            {
                rawValue = ExtractBigEndian(data, signal.StartBit, signal.Length);
            }

            // Apply two's complement for signed values
            if (signal.IsSigned)
            {
                long signBit = 1L << (signal.Length - 1);
                if ((rawValue & signBit) != 0)
                {
                    rawValue = -(long)(((1UL << signal.Length) - 1) ^ (ulong)rawValue) - 1;
                }
            }

            // Apply scale and offset
            double physicalValue = (rawValue * signal.Scale) + signal.Offset;

            return physicalValue;
        }

        private long ExtractLittleEndian(byte[] data, int startBit, int length)
        {
            long result = 0;
            int currentBit = startBit;
            int bitsRead = 0;

            while (bitsRead < length)
            {
                int byteIndex = currentBit / 8;
                int bitInByte = currentBit % 8;
                int bitsAvailableInByte = 8 - bitInByte;
                int bitsToRead = Math.Min(length - bitsRead, bitsAvailableInByte);

                if (byteIndex >= data.Length)
                    break;

                byte mask = (byte)((1 << bitsToRead) - 1);
                long bitsValue = (data[byteIndex] >> bitInByte) & mask;

                result |= bitsValue << bitsRead;

                bitsRead += bitsToRead;
                currentBit += bitsToRead;
            }

            return result;
        }

        private long ExtractBigEndian(byte[] data, int startBit, int length)
        {
            long result = 0;
            int currentBit = startBit;
            int bitsRead = 0;

            while (bitsRead < length)
            {
                int byteIndex = currentBit / 8;
                int bitInByte = 7 - (currentBit % 8);
                int bitsAvailableInByte = bitInByte + 1;
                int bitsToRead = Math.Min(length - bitsRead, bitsAvailableInByte);

                if (byteIndex >= data.Length)
                    break;

                byte mask = (byte)(((1 << bitsToRead) - 1) << (bitInByte - bitsToRead + 1));
                long bitsValue = (data[byteIndex] & mask) >> (bitInByte - bitsToRead + 1);

                result = (result << bitsToRead) | bitsValue;

                bitsRead += bitsToRead;
                currentBit += bitsToRead;
            }

            return result;
        }
    }
}
