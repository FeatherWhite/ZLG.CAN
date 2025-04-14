using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ZLG.CAN.Models;

namespace ZLG.CAN
{
    public class USBCanFDCommunication
    {
        ZLGOperation zlgOperation = new ZLGOperation();

        public bool IsOpen { get; set; }
        public ErrorMessage Error { get; set; } = new ErrorMessage();
        public CANFDStandard CANFDStandard { get; set; } = CANFDStandard.CANFDISO;
        public CANFDAccelerate CANFDAccelerate { get; set; } = CANFDAccelerate.NO;
        public USBCANFDABaudrate USBCANFDABaudrate1 { get; set; } = USBCANFDABaudrate._500kbps;
        public USBCANFDABaudrate USBCANFDABaudrate2 { get; set; } = USBCANFDABaudrate._500kbps;
        public USBCANFDDBaudrate USBCANFDDBaudrate1 { get; set; } = USBCANFDDBaudrate._2000kbps;
        public USBCANFDDBaudrate USBCANFDDBaudrate2 { get; set; } = USBCANFDDBaudrate._2000kbps;
        public FrameType FrameType { get; set; } = FrameType.Extended;
        public void Open()
        {
            CanFDPara para1 = new CanFDPara()
            {
                Standard = CANFDStandard,
                Filter = new Filter()
                {
                    FilterType = FilterType.Disable
                },
                ProtocolType = ProtocolType.CANFD,
                CANFDAccelerate = CANFDAccelerate,
                TREnable = true
            };

            ZLGConfig config = new ZLGConfig(DeviceInfoIndex.ZCAN_USBCANFD_200U, 0,
                USBCANFDABaudrate1, USBCANFDDBaudrate1, para1, FrameType);
            zlgOperation.SetConfig(config);
            zlgOperation.Open(0);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.InitCAN();
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.StartCAN();
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }


            CanFDPara para2 = new CanFDPara()
            {
                Standard = CANFDStandard,
                Filter = new Filter()
                {
                    FilterType = FilterType.Disable
                },
                ProtocolType = ProtocolType.CANFD,
                CANFDAccelerate = CANFDAccelerate,
                TREnable = true
            };
            config = new ZLGConfig(DeviceInfoIndex.ZCAN_USBCANFD_200U, 1,
                USBCANFDABaudrate2, USBCANFDDBaudrate2, para2, FrameType);

            zlgOperation.SetConfig(config);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.InitCAN();
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.StartCAN();
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            IsOpen = true;
        }

        public void Close()
        {
            zlgOperation.Close();
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        {
            return zlgOperation.Send(canId, channelIndex, strData);
        }

        public ZCAN_ReceiveFD_Data Receive(uint channelIndex,uint receiveId)
        {
            var dataArray = zlgOperation.Receive<ZCAN_ReceiveFD_Data[]>(channelIndex);
            var res = dataArray.
                Where(data => GetId(data.frame.can_id) == receiveId).First();
            res.frame.can_id = GetId(res.frame.can_id);
            return res;
        }
        private uint GetId(uint canid)
        {
            return canid & 0x1FFFFFFFU;
        }
    }
}
