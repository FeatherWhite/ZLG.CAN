using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZLG.CAN.Models
{
    public class ZLGConfig
    {
        //public int NULL => 0;
        //public int CANFD_BRS => 0x01; // bit rate switch (second bitrate for payload data)
        //public int CANFD_ESI => 0x02; // error state indicator of the transmitting node

        ////CAN有效负载长度和DLC定义
        //public int CAN_MAX_DLC => 8;
        //public int CAN_MAX_DLEN => 8;
        //public int LIN_MAX_DLEN => 8;

        //// CANFD有效负载长度和DLC定义
        //public int CANFD_MAX_DLC => 15;
        //public int CANFD_MAX_DLEN => 64;

        //// CAN标志
        //public uint CAN_EFF_FLAG => 0x80000000U; // EFF/SFF is set in the MSB
        //public uint CAN_RTR_FLAG => 0x40000000U; // remote transmission request
        //public uint CAN_ERR_FLAG => 0x20000000U; // error message frame
        //public uint CAN_ID_FLAG => 0x1FFFFFFFU; // id
        public int ChannelIndex { get; set; }
        private DeviceInfo[] DeviceInfos =>
        [
            new DeviceInfo(Define.ZCAN_USBCAN1, 1),
            new DeviceInfo(Define.ZCAN_USBCAN2, 2),
            new DeviceInfo(Define.ZCAN_PCI9820I,2),
            new DeviceInfo(Define.ZCAN_USBCAN_E_U, 1),
            new DeviceInfo(Define.ZCAN_USBCAN_2E_U, 2),
            new DeviceInfo(Define.ZCAN_USBCAN_4E_U, 4),
            new DeviceInfo(Define.ZCAN_PCIECANFD_100U, 1),
            new DeviceInfo(Define.ZCAN_PCIECANFD_200U, 2),
            new DeviceInfo(Define.ZCAN_PCIECANFD_200U_EX,2),
            new DeviceInfo(Define.ZCAN_PCIECANFD_400U, 4),
            new DeviceInfo(Define.ZCAN_USBCANFD_200U, 2),
            new DeviceInfo(Define.ZCAN_USBCANFD_400U,4),
            new DeviceInfo(Define.ZCAN_USBCANFD_800U, 8),
            new DeviceInfo(Define.ZCAN_USBCANFD_100U, 1),
            new DeviceInfo(Define.ZCAN_USBCANFD_MINI, 1),
            new DeviceInfo(Define.ZCAN_CANETTCP, 1),
            new DeviceInfo(Define.ZCAN_CANETUDP, 1),
            new DeviceInfo(Define.ZCAN_CANWIFI_TCP, 1),
            new DeviceInfo(Define.ZCAN_CANFDNET_200U_TCP, 2),
            new DeviceInfo(Define.ZCAN_CANFDNET_200U_UDP, 2),
            new DeviceInfo(Define.ZCAN_CANFDNET_400U_TCP, 4),
            new DeviceInfo(Define.ZCAN_CANFDNET_400U_UDP, 4),
            new DeviceInfo(Define.ZCAN_CANFDNET_800U_TCP, 8),
            new DeviceInfo(Define.ZCAN_CANFDNET_800U_UDP, 8),
            new DeviceInfo(Define.ZCAN_CLOUD, 1)
        ];
        public DeviceInfo DeviceInfo => DeviceInfos[(uint)Index];
        public FrameType FrameType { get; set; } = FrameType.Extended;
        public KBaudrate Baudrate { get; set; }
        public USBCANFDABaudrate USBCANFDAbit { get; set; }
        public USBCANFDDBaudrate USBCANFDDbit { get; set; }
        public PCIECANFDABaudrate PCIECANFDAbit { get; set; }
        public PCIECANFDDBaudrate PCIECANFDDbit { get; set; }
        public LINBaudrate LINbit { get; set; }

        public uint[] kBaudrate =>
        [
            1000000,//1000kbps
            800000,//800kbps
            500000,//500kbps
            250000,//250kbps
            125000,//125kbps
            100000,//100kbps
            50000,//50kbps
            20000,//20kbps
            10000,//10kbps
            5000 //5kbps
        ];
        public uint[] kUSBCANFDAbit =>
        [
            1000000, // 1Mbps
            800000, // 800kbps
            500000, // 500kbps
            250000, // 250kbps
            125000, // 125kbps
            100000, // 100kbps
            50000, // 50kbps
            800000, // 800kbps
        ];
        public uint[] kUSBCANFDDbit =>
        [
            5000000, // 5Mbps
            4000000, // 4Mbps
            2000000, // 2Mbps
            1000000, // 1Mbps
            800000, // 800kbps
            500000, // 500kbps
            250000, // 250kbps
            125000, // 125kbps
            100000, // 100kbps
        ];
        public uint[] kPCIECANFDAbit =>
        [
            1000000, // 1Mbps
            800000, // 800kbps
            500000, // 500kbps
            250000, // 250kbps
        ];
        public uint[] kPCIECANFDDbit =>
        [
            8000000, // 8Mbps
            4000000, // 4Mbps
            2000000, // 2Mbps
        ];
        public uint[] kLINbit =>
        [
            2400,
            4800,
            9600,
            10417,
            19200,
            20000,
        ]; // LIN常见波特率
        /// <summary>
        /// 数据合并
        /// </summary>
        public bool IsDataMerge { get; set; } = false;
        public CanFDPara CanFDPara { get; set; } = new CanFDPara();

        /// <summary>
        /// 传输方式
        /// </summary>
        public TransmissionMode TransmissionMode { get; set; }
       
        private DeviceInfoIndex Index;
        public ZLGConfig(DeviceInfoIndex index,int channelIndex, KBaudrate baudrate,FrameType frameType=FrameType.Extended)
        {
            this.Index = index;
            this.ChannelIndex = channelIndex;
            this.Baudrate = baudrate;
            this.FrameType = frameType;
        }
        /// <summary>
        /// 使用CAN协议，无数据域
        /// </summary>
        public ZLGConfig(DeviceInfoIndex index, int channelIndex, CanFDPara para,
            USBCANFDABaudrate Abaudrate = USBCANFDABaudrate._500kbps, FrameType frameType = FrameType.Extended)
        {
            this.Index = index;
            this.ChannelIndex = channelIndex;
            this.USBCANFDAbit = Abaudrate;
            this.USBCANFDDbit = USBCANFDDBaudrate._500kbps;
            this.CanFDPara = para;
            this.FrameType = frameType;
        }
        /// <summary>
        /// 使用CANFD协议
        /// </summary>
        public ZLGConfig(DeviceInfoIndex index, int channelIndex, USBCANFDABaudrate Abaudrate,
            USBCANFDDBaudrate Dbaudrate, CanFDPara para, FrameType frameType = FrameType.Extended)
        {
            this.Index = index;
            this.ChannelIndex = channelIndex;
            this.USBCANFDAbit = Abaudrate;
            this.USBCANFDDbit = Dbaudrate;
            this.CanFDPara = para;
            this.FrameType = frameType;
        }
    }
    public enum FrameType
    {
        Standard,
        Extended
    }
    public enum TransmissionMode
    {
        /// <summary>
        /// 正常发送
        /// </summary>
        NormalSend,     
        /// <summary>
        /// 单次发送
        /// </summary>
        SingleSend,
        /// <summary>
        /// 自发自收
        /// </summary>
        SelfTransmitReceive, 
        /// <summary>
        /// 单次自发自收
        /// </summary>
        SingleSelfTransmitReceive
    }

    public enum DeviceInfoIndex
    {
        ZCAN_USBCAN1,
        ZCAN_USBCAN2,
        ZCAN_PCI9820I,
        ZCAN_USBCAN_E_U,
        ZCAN_USBCAN_2E_U,
        ZCAN_USBCAN_4E_U,
        ZCAN_PCIECANFD_100U,
        ZCAN_PCIECANFD_200U,
        ZCAN_PCIECANFD_200U_EX,
        ZCAN_PCIECANFD_400U,
        ZCAN_USBCANFD_200U,
        ZCAN_USBCANFD_400U,
        ZCAN_USBCANFD_800U,
        ZCAN_USBCANFD_100U,
        ZCAN_USBCANFD_MINI,
        ZCAN_CANETTCP,
        ZCAN_CANETUDP,
        ZCAN_CANWIFI_TCP,
        ZCAN_CANFDNET_200U_TCP,
        ZCAN_CANFDNET_200U_UDP,
        ZCAN_CANFDNET_400U_TCP,
        ZCAN_CANFDNET_400U_UDP,
        ZCAN_CANFDNET_800U_TCP,
        ZCAN_CANFDNET_800U_UDP,
        ZCAN_CLOUD
    }

    public enum KBaudrate:uint
    {
        _1000kbps = 1000000,//1000kbps
        _800kbps = 800000,//800kbps
        _500kbps = 500000,//500kbps
        _250kbps = 250000,//250kbps
        _125kbps = 125000,//125kbps
        _100kbps = 100000,//100kbps
        _50kbps = 50000,//50kbps
        _20kbps = 20000,//20kbps
        _10kbps = 10000,//10kbps
        _5kbps = 5000 //5kbps
    };
    public enum USBCANFDABaudrate:uint
    {
        _1000kbps = 1000000, // 1Mbps
        _800kbps = 800000,   // 800kbps
        _500kbps = 500000,   // 500kbps
        _250kbps = 250000,   // 250kbps
        _125kbps = 125000,   // 125kbps
        _100kbps = 100000,   // 100kbps
        _50kbps = 50000,     // 50kbps

    }

    public enum USBCANFDDBaudrate:uint
    {
        _5000kbps = 5000000, // 5Mbps
        _4000kbps = 4000000, // 4Mbps
        _2000kbps = 2000000, // 2Mbps
        _1000kbps = 1000000, // 1Mbps
        _800kbps = 800000,   // 800kbps
        _500kbps = 500000,   // 500kbps
        _250kbps = 250000,   // 250kbps
        _125kbps = 125000,   // 125kbps
        _100kbps = 100000    // 100kbps
    }

    public enum PCIECANFDABaudrate:uint
    {
        _1000kbps = 1000000, // 1Mbps
        _800kbps = 800000,   // 800kbps
        _500kbps = 500000,   // 500kbps
        _250kbps = 250000    // 250kbps
    }

    public enum PCIECANFDDBaudrate:uint
    {
        _8000kbps = 8000000, // 8Mbps
        _4000kbps = 4000000, // 4Mbps
        _2000kbps = 2000000  // 2Mbps
    }

    public enum LINBaudrate:uint
    {
        _2400bps = 2400,
        _4800bps = 4800,
        _9600bps = 9600,
        _10417bps = 10417,
        _19200bps = 19200,
        _20000bps = 20000
    }
    public class Define
    {
        public const int TYPE_CAN = 0;
        public const int TYPE_CANFD = 1;
        public const int ZCAN_USBCAN1 = 3;
        public const int ZCAN_USBCAN2 = 4;
        public const int ZCAN_PCI9820I = 16;
        public const int ZCAN_CANETUDP = 12;
        public const int ZCAN_CANETTCP = 17;
        public const int ZCAN_CANWIFI_TCP = 25;
        public const int ZCAN_USBCAN_E_U = 20;
        public const int ZCAN_USBCAN_2E_U = 21;
        public const int ZCAN_USBCAN_4E_U = 31;
        public const int ZCAN_PCIECANFD_100U = 38;
        public const int ZCAN_PCIECANFD_200U = 39;
        public const int ZCAN_PCIECANFD_200U_EX = 62;
        public const int ZCAN_PCIECANFD_400U = 61;
        public const int ZCAN_USBCANFD_200U = 41;
        public const int ZCAN_USBCANFD_400U = 76;
        public const int ZCAN_USBCANFD_100U = 42;
        public const int ZCAN_USBCANFD_MINI = 43;
        public const int ZCAN_USBCANFD_800U = 59;
        public const int ZCAN_CLOUD = 46;
        public const int ZCAN_CANFDNET_200U_TCP = 48;
        public const int ZCAN_CANFDNET_200U_UDP = 49;
        public const int ZCAN_CANFDNET_400U_TCP = 52;
        public const int ZCAN_CANFDNET_400U_UDP = 53;
        public const int ZCAN_CANFDNET_800U_TCP = 57;
        public const int ZCAN_CANFDNET_800U_UDP = 58;
        public const int STATUS_ERR = 0;
        public const int STATUS_OK = 1;
    };
}
