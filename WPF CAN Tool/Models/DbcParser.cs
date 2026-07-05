using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WPF_CAN_Tool.Models
{
    public class DbcParser
    {
        public Dictionary<uint, DbcMessage> ParseDbc(string dbcFilePath)
        {
            var messages = new Dictionary<uint, DbcMessage>();

            try
            {
                var lines = File.ReadAllLines(dbcFilePath);
                var messageDict = new Dictionary<uint, DbcMessage>();

                // Parse messages
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (line.StartsWith("BO_ "))
                    {
                        var message = ParseMessage(line);
                        if (message != null)
                        {
                            messageDict[message.Id] = message;
                        }
                    }
                }

                // Parse signals
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (line.StartsWith("SG_ "))
                    {
                        var (signal, messageId) = ParseSignal(line);
                        if (signal != null && messageDict.ContainsKey(messageId))
                        {
                            messageDict[messageId].Signals.Add(signal);
                        }
                    }
                }

                return messageDict;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing DBC file: {ex.Message}");
                return messages;
            }
        }

        private DbcMessage? ParseMessage(string line)
        {
            // Format: BO_ 1537 Inverter_Command: 3 0x6__LVBox
            var match = Regex.Match(line, @"BO_ (\d+) (\w+): (\d+) (.+)");
            if (match.Success)
            {
                return new DbcMessage
                {
                    Id = uint.Parse(match.Groups[1].Value),
                    Name = match.Groups[2].Value,
                    Length = int.Parse(match.Groups[3].Value),
                    Sender = match.Groups[4].Value.Trim()
                };
            }

            return null;
        }

        private (DbcSignal?, uint) ParseSignal(string line)
        {
            // Format: SG_ SignalName m0 : StartBit|Length@ByteOrder+/- (Scale,Offset) [Min|Max] "Unit" Receivers
            // Example: SG_ FL_Wheelspeed m0 : 8|16@1- (1,0) [-32768|32767] "" Vector__XXX
            
            // Extract message ID from context (passed as part of signal line parsing)
            // We'll need to track this differently
            var match = Regex.Match(line, @"SG_ (\w+)(\s+m\d+)?\s*:\s*(\d+)\|(\d+)@(\d)([\+\-])\s*\(([^,]+),([^\)]+)\)\s*\[([^\|]+)\|([^\]]+)\]\s*""([^""]*)""\s*(.*)");

            if (match.Success)
            {
                string signalName = match.Groups[1].Value;
                int startBit = int.Parse(match.Groups[3].Value);
                int length = int.Parse(match.Groups[4].Value);
                int byteOrder = int.Parse(match.Groups[5].Value); // 0 = big endian, 1 = little endian
                bool isSigned = match.Groups[6].Value == "-";
                double scale = double.Parse(match.Groups[7].Value);
                double offset = double.Parse(match.Groups[8].Value);
                double minValue = double.Parse(match.Groups[9].Value);
                double maxValue = double.Parse(match.Groups[10].Value);
                string unit = match.Groups[11].Value;

                var signal = new DbcSignal
                {
                    Name = signalName,
                    StartBit = startBit,
                    Length = length,
                    IsLittleEndian = byteOrder == 1,
                    IsSigned = isSigned,
                    Scale = scale,
                    Offset = offset,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    Unit = unit
                };

                // Extract message ID from the line or context
                // For now, return 0 - we'll handle this in the calling code
                return (signal, 0);
            }

            return (null, 0);
        }

        public Dictionary<uint, DbcMessage> ParseDbcAdvanced(string dbcFilePath)
        {
            var messages = new Dictionary<uint, DbcMessage>();

            try
            {
                var content = File.ReadAllText(dbcFilePath);
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                uint currentMessageId = 0;
                DbcMessage? currentMessage = null;

                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();

                    // Parse message definition
                    if (trimmedLine.StartsWith("BO_ "))
                    {
                        currentMessage = ParseMessage(trimmedLine);
                        if (currentMessage != null)
                        {
                            currentMessageId = currentMessage.Id;
                            messages[currentMessageId] = currentMessage;
                        }
                    }
                    // Parse signal definition
                    else if (trimmedLine.StartsWith("SG_ ") && currentMessage != null)
                    {
                        var signal = ParseSignalLine(trimmedLine);
                        if (signal != null)
                        {
                            currentMessage.Signals.Add(signal);
                        }
                    }
                }

                return messages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing DBC file: {ex.Message}");
                return messages;
            }
        }

        private DbcSignal? ParseSignalLine(string line)
        {
            // Format: SG_ SignalName [m0] : StartBit|Length@ByteOrder+/- (Scale,Offset) [Min|Max] "Unit" Receivers
            try
            {
                // Remove leading "SG_ "
                string content = line.Substring(4);

                // Find the colon that separates signal name from bit position
                int colonIndex = content.IndexOf(':');
                if (colonIndex < 0) return null;

                string namePart = content.Substring(0, colonIndex).Trim();
                string bitPart = content.Substring(colonIndex + 1).Trim();

                // Extract signal name (remove multiplexer info like "m0")
                string signalName = Regex.Replace(namePart, @"\s+m\d+$", "").Trim();

                // Parse bit information: StartBit|Length@ByteOrder+/-
                var bitMatch = Regex.Match(bitPart, @"(\d+)\|(\d+)@(\d)([\+\-])");
                if (!bitMatch.Success) return null;

                int startBit = int.Parse(bitMatch.Groups[1].Value);
                int length = int.Parse(bitMatch.Groups[2].Value);
                int byteOrder = int.Parse(bitMatch.Groups[3].Value);
                bool isSigned = bitMatch.Groups[4].Value == "-";

                // Parse scale, offset, min, max, unit
                var scaleMatch = Regex.Match(bitPart, @"\(([^,]+),([^\)]+)\)");
                var rangeMatch = Regex.Match(bitPart, @"\[([^\|]+)\|([^\]]+)\]");
                var unitMatch = Regex.Match(bitPart, @"""([^""]*)""");

                double scale = scaleMatch.Success ? double.Parse(scaleMatch.Groups[1].Value) : 1.0;
                double offset = scaleMatch.Success ? double.Parse(scaleMatch.Groups[2].Value) : 0.0;
                double minValue = rangeMatch.Success ? double.Parse(rangeMatch.Groups[1].Value) : 0.0;
                double maxValue = rangeMatch.Success ? double.Parse(rangeMatch.Groups[2].Value) : 0.0;
                string unit = unitMatch.Success ? unitMatch.Groups[1].Value : "";

                return new DbcSignal
                {
                    Name = signalName,
                    StartBit = startBit,
                    Length = length,
                    IsLittleEndian = byteOrder == 1,
                    IsSigned = isSigned,
                    Scale = scale,
                    Offset = offset,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    Unit = unit
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing signal line: {ex.Message}");
                return null;
            }
        }
    }
}
