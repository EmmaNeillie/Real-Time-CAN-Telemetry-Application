namespace WPF_CAN_Tool.Models
{
    public class DbcSignal
    {
        public string Name { get; set; } = string.Empty;
        public int StartBit { get; set; }
        public int Length { get; set; }
        public bool IsLittleEndian { get; set; }
        public bool IsSigned { get; set; }
        public double Scale { get; set; } = 1.0;
        public double Offset { get; set; } = 0.0;
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool IsMultiplexer { get; set; }
        public int? MultiplexerValue { get; set; }
    }
}
