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
        public delegate void LoggerInfoFunc(string message);
        public LoggerInfoFunc LogInfo;
        public uint DeviceIndex { get; set; } = 0;
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
        public bool[] TREnable { get; set; } = [true, true];
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
                    TREnable = TREnable[0]
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
                    TREnable = TREnable[1]
               },
            ];
            config = [
                new ZLGConfig(DeviceInfoIndex[0], 0,
                USBCANFDABaudrate[0], USBCANFDDBaudrate[0], para[0], FrameType[0]),
                new ZLGConfig(DeviceInfoIndex[1], 1,
                USBCANFDABaudrate[1], USBCANFDDBaudrate[1], para[1], FrameType[1])
                ];

            zlgOperation.SetConfig(config[0]);
            zlgOperation.Open(DeviceIndex);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen &= false;
            }
            zlgOperation.InitCAN();
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen &= false;
            }
            zlgOperation.StartCAN();
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen &= false;
            }

            zlgOperation.SetConfig(config[1]);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen &= false;
            }
            zlgOperation.InitCAN();
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen &= false;
            }
            zlgOperation.StartCAN();
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen &= false;
            }
            return IsOpen;
        }

        public void Close()
        {
            IsOpen = false;
            zlgOperation.Close();
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        {
            zlgOperation.FrameType = canId > 0x7FF ? Models.FrameType.Extended : Models.FrameType.Standard;
            zlgOperation.TransmissionMode = config[channelIndex].TransmissionMode;
            zlgOperation.CANFDAccelerate = para[channelIndex].CANFDAccelerate;
            zlgOperation.CanFDProtocolType = para[channelIndex].ProtocolType;
            bool isSuccess = zlgOperation.Send(canId, channelIndex, strData);
            if (isSuccess)
            {
                Console.WriteLine($"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}");
                LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}");        
            }
            else
            {
                Console.WriteLine($"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}失败");
                Error = zlgOperation.ErrorMessage;
                if (LogInfo != null)
                {
                    LogInfo($"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}失败");
                    LogInfo($"错误码:{Error.ErrorCode}");
                }

            }
            return isSuccess;
        }

        public bool Send(uint canId, uint channelIndex, byte[] data)
        {
            zlgOperation.FrameType = canId > 0x7FF ? Models.FrameType.Extended : Models.FrameType.Standard;
            zlgOperation.TransmissionMode = config[channelIndex].TransmissionMode;
            zlgOperation.CANFDAccelerate = para[channelIndex].CANFDAccelerate;
            zlgOperation.CanFDProtocolType = para[channelIndex].ProtocolType;
            bool isSuccess = zlgOperation.Send(canId, channelIndex, data);
            if (isSuccess)
            {
                //Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " +
                //    $"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
                LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
            }
            else
            {
                //Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} "
                //    + $"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
                LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}失败");
                LogInfo?.Invoke($"错误码:{Error.ErrorCode}");
            }
            return isSuccess;
        }

        public T Receive<T>(uint channelIndex)
        {
            return zlgOperation.Receive<T>(channelIndex);
        }

        public void Clear(uint channelIndex)
        {
            zlgOperation.ClearBuffer(channelIndex);
            LogInfo?.Invoke($"清除通道:{channelIndex} 接收缓存");
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
                    if (query.Count() > 0)
                    {
                        ret = query.First();
                        LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                            $"通道:{channelIndex}," +
                            $"数据:{BitConverter.ToString(ret.frame.data)}");
                    }                    
                }
            }
            return ret;
        }

        public (bool isSuccess,ZCAN_ReceiveFD_Data all) ReceiveFD_V2(uint channelIndex, uint receiveId)
        {
            var array = zlgOperation.Receive<ZCAN_ReceiveFD_Data[]>(channelIndex);
            bool isSuccess = false;
            ZCAN_ReceiveFD_Data ret = new ZCAN_ReceiveFD_Data();
            if (array != null)
            {
                if (array.Length > 0)
                {
                    var query = array.
                        Where(data => GetId(data.frame.can_id) == receiveId);
                    if (query.Count() > 0)
                    {
                        ret = query.Last();

                        LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                            $"通道:{channelIndex},数据:{BitConverter.ToString(ret.frame.data)}");
                        
                        isSuccess = true;
                    }
                }
            }
            if (!isSuccess)
            {
                LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                    $"通道:{channelIndex},未接收到任何数据");               
            }
            return (isSuccess, ret);
        }

        public List<(bool isSuccess, ZCAN_ReceiveFD_Data all)> ReceiveFD_V2(uint channelIndex, uint[] receiveIds)
        {
            var array = zlgOperation.Receive<ZCAN_ReceiveFD_Data[]>(channelIndex);
            List<(bool isSuccess, ZCAN_ReceiveFD_Data all)> rets = new List<(bool isSuccess, ZCAN_ReceiveFD_Data all)>();
            foreach (var receiveId in receiveIds)
            {
                bool isSuccess = false;
                ZCAN_ReceiveFD_Data ret = new ZCAN_ReceiveFD_Data();
                if (array != null)
                {
                    if (array.Length > 0)
                    {
                        var query = array.
                            Where(data => GetId(data.frame.can_id) == receiveId);
                        if (query.Count() > 0)
                        {
                            ret = query.First();
                            if (LogInfo != null)
                            {
                                LogInfo($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                                $"通道:{channelIndex},数据:{BitConverter.ToString(ret.frame.data)}");
                            }
                            isSuccess = true;
                        }
                    }
                }
                if (!isSuccess)
                {
                    if (LogInfo != null)
                    {
                        LogInfo($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                        $"通道:{channelIndex},未接收到任何数据");
                    }
                }
                rets.Add((isSuccess, ret));
            }
            return rets;
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
                        ret = query.Last();
                        LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                            $"通道:{channelIndex},数据:{BitConverter.ToString(ret.frame.data)}");
                    }
                    else
                    {
                        LogInfo?.Invoke($"{DeviceInfoIndex[channelIndex]} 接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                            $"通道:{channelIndex},未接收到任何数据");
                    }
                }
            }
            return ret;
        }

        public uint GetReceiveNum(uint channelIndex, byte type)
        {
            return zlgOperation.GetReceiveNum(channelIndex, type);
        }
        private uint GetId(uint canid)
        {
            return canid & 0x1FFFFFFFU;
        }
    }
}