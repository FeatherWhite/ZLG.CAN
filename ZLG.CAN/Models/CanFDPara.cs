namespace ZLG.CAN.Models
{
    public class CanFDPara
    {
        public CANFDStandard Standard { get; set; }
        public Filter Filter { get; set; }
        /// <summary>
        /// 协议类型CAN/CANFD
        /// </summary>
        public ProtocolType ProtocolType { get; set; }
        public CANFDAccelerate CANFDAccelerate { get; set; }

        /// <summary>
        /// 终端电阻使能
        /// </summary>
        public bool TREnable { get; set; } = true;
    }
    public enum CANFDAccelerate
    {
        NO,
        YES,
    }
    public enum CANFDStandard
    {
        CANFDISO,
        CANFDBOSCH
    }
    public class Filter
    {
        public string StartID { get; set; } = string.Empty;
        public string EndID { get; set; } = string.Empty;
        public FilterType FilterType { get; set; }
    }
    public enum FilterType
    {
        StandardFrame,
        ExtendedFrame,
        Disable
    }
    public enum ProtocolType
    {
        CAN,
        CANFD
    }
}
