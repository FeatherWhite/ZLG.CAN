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
        private ZLGOperation zlgOperation = new ZLGOperation();

        public bool IsOpen { get; set; }
        public ErrorMessage Error { get; set; } = new ErrorMessage();
        public CANFDStandard[] CANFDStandard { get; set; } = [Models.CANFDStandard.CANFDISO, Models.CANFDStandard.CANFDISO];
        public CANFDAccelerate[] CANFDAccelerate { get; set; } = [Models.CANFDAccelerate.NO, Models.CANFDAccelerate.NO];
        public USBCANFDABaudrate[] USBCANFDABaudrate { get; set; } = [Models.USBCANFDABaudrate._500kbps, Models.USBCANFDABaudrate._500kbps];
        public USBCANFDDBaudrate[] USBCANFDDBaudrate { get; set; } = [Models.USBCANFDDBaudrate._2000kbps, Models.USBCANFDDBaudrate._2000kbps];
        public ProtocolType[] ProtocolType { get; set; } = [Models.ProtocolType.CAN, Models.ProtocolType.CAN];
        public FrameType[] FrameType { get; set; } = [Models.FrameType.Standard, Models.FrameType.Standard];
        public DeviceInfoIndex[] DeviceInfoIndex { get; set; }
            = [Models.DeviceInfoIndex.ZCAN_USBCANFD_200U, Models.DeviceInfoIndex.ZCAN_USBCANFD_200U];
        private CanFDPara[] para;
        private ZLGConfig[] config;

        public bool Open()
        {
            IsOpen = true;

            para =
            [
               new CanFDPara()
               {
                    Standard = CANFDStandard[0],
                    Filter = new Filter()
                    {
                        FilterType = FilterType.Disable
                    },
                    ProtocolType = ProtocolType[0],
                    CANFDAccelerate = CANFDAccelerate[0],
                    TREnable = true
               },
               new CanFDPara()
               {
                    Standard = CANFDStandard[1],
                    Filter = new Filter()
                    {
                        FilterType = FilterType.Disable
                    },
                    ProtocolType = ProtocolType[1],
                    CANFDAccelerate = CANFDAccelerate[1],
                    TREnable = true
               },
            ];
            config = [
                new ZLGConfig(DeviceInfoIndex[0], 0,
                USBCANFDABaudrate[0], USBCANFDDBaudrate[0], para[0], FrameType[0]),
                new ZLGConfig(DeviceInfoIndex[1], 1,
                USBCANFDABaudrate[1], USBCANFDDBaudrate[1], para[1], FrameType[1])
                ];

            zlgOperation.SetConfig(config[0]);
            zlgOperation.Open(0);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
            }
            zlgOperation.InitCAN();
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
            }
            zlgOperation.StartCAN();
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
            }

            zlgOperation.SetConfig(config[1]);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
            }
            zlgOperation.InitCAN();
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
            }
            zlgOperation.StartCAN();
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
            }
            return IsOpen;
        }

        public void Close()
        {
            zlgOperation.Close();
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        {
            zlgOperation.FrameType = config[channelIndex].FrameType;
            zlgOperation.TransmissionMode = config[channelIndex].TransmissionMode;
            zlgOperation.CANFDAccelerate = para[channelIndex].CANFDAccelerate;
            zlgOperation.CanFDProtocolType = para[channelIndex].ProtocolType;
            return zlgOperation.Send(canId, channelIndex, strData);
        }

        public bool Send(uint canId, uint channelIndex, byte[] data)
        {
            zlgOperation.FrameType = config[channelIndex].FrameType;
            zlgOperation.TransmissionMode = config[channelIndex].TransmissionMode;
            zlgOperation.CANFDAccelerate = para[channelIndex].CANFDAccelerate;
            zlgOperation.CanFDProtocolType = para[channelIndex].ProtocolType;
            bool isSuccess = zlgOperation.Send(canId, channelIndex, data);
            if (isSuccess)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " +
                    $"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} "
                    + $"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
            }
            return isSuccess;
        }

        public T Receive<T>(uint channelIndex)
        {
            return zlgOperation.Receive<T>(channelIndex);
        }

        public ZCAN_ReceiveFD_Data ReceiveFD(uint channelIndex, uint receiveId)
        {
            var array = zlgOperation.Receive<ZCAN_ReceiveFD_Data[]>(channelIndex);
            ZCAN_ReceiveFD_Data ret = new ZCAN_ReceiveFD_Data();
            if (array != null)
            {
                if (array.Length > 0)
                {
                    var query = array.
                        Where(data => GetId(data.frame.can_id) == receiveId);
                    ret = query.First();
                }
            }
            return ret;
        }

        public ZCAN_Receive_Data Receive(uint channelIndex, uint receiveId)
        {
            var array = zlgOperation.Receive<ZCAN_Receive_Data[]>(channelIndex);
            ZCAN_Receive_Data ret = new ZCAN_Receive_Data();
            if (array != null)
            {
                if (array.Length > 0)
                {
                    var query = array.
                        Where(data => GetId(data.frame.can_id) == receiveId);
                    if (query.Count() > 0)
                    {
                        ret = query.First();
                    }
                }
            }
            return ret;
        }

        private uint GetId(uint canid)
        {
            return canid & 0x1FFFFFFFU;
        }
    }
}