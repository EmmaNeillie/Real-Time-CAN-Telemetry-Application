using System.Collections.Generic;

namespace WPF_CAN_Tool.Models
{
    public class DbcMessage
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Length { get; set; }
        public string Sender { get; set; } = string.Empty;
        public List<DbcSignal> Signals { get; set; } = new();
    }
}
