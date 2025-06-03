using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZLG.CAN.Models;

namespace ZLG.CAN
{
    public class USBCanIICommunication
    {
        ZLGOperation zlgOperation = new ZLGOperation();
        public delegate void LoggerInfoFunc(string message);
        public LoggerInfoFunc LogInfo;
        public bool IsOpen { get; set; }
        public ErrorMessage Error { get; set; } = new ErrorMessage();
        public ZLGCANPara CanPara { get { return para; } }
        private ZLGCANPara para = new ZLGCANPara();
        public void Open()
        {
            ZLGConfig config = new ZLGConfig(para.deviceInfoIndex, 0, para.kBaudrates[0], para.frameType[0]);
            zlgOperation.SetConfig(config);
            zlgOperation.Open(para.deviceIndex);
            //LogInfo($"设备索引:{para.deviceIndex} 打开设备");
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.InitCAN();
            //LogInfo($"设备索引:{para.deviceIndex} 通道索引:0 初始化CAN");
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.StartCAN();
            //LogInfo($"设备索引:{para.deviceIndex} 通道索引:0 启动CAN");
            if (!zlgOperation.IsStartCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }

            config = new ZLGConfig(para.deviceInfoIndex, 1, para.kBaudrates[1], para.frameType[1]);
            zlgOperation.SetConfig(config);
            if (!zlgOperation.IsDeviceOpen)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.InitCAN();
            //LogInfo($"设备索引:{para.deviceIndex} 通道索引:1 初始化CAN");
            if (!zlgOperation.IsInitCAN)
            {
                Error = zlgOperation.ErrorMessage;
                IsOpen = false;
                return;
            }
            zlgOperation.StartCAN();
            //LogInfo($"设备索引:{para.deviceIndex} 通道索引:1 启动CAN");
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

        public void SetPara(ZLGCANPara para)
        {
            this.para = para;
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        { 
            zlgOperation.FrameType = para.frameType[channelIndex];
            bool isSuccess = zlgOperation.Send(canId, channelIndex, strData);
            if (isSuccess)
            {
                Console.WriteLine($"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}");
                if (LogInfo != null)
                {
                    LogInfo($"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}");
                }      
            }
            else
            {
                Console.WriteLine($"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
                Error = zlgOperation.ErrorMessage;
                if(LogInfo != null)
                {
                    LogInfo($"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
                    LogInfo($"错误码:{Error.ErrorCode}");
                }

            }
            return isSuccess;
        }

        public bool Send(uint canId, uint channelIndex, byte[] data)
        {
            zlgOperation.FrameType = para.frameType[channelIndex];
            bool isSuccess = zlgOperation.Send(canId, channelIndex, data);
            if (isSuccess)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " +
                    $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
                if (LogInfo != null)
                {
                    LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " +
                    $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
                }
                
            }
            else
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} "
                    + $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
                if(LogInfo != null)
                {
                    LogInfo($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} "
                    + $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
                    LogInfo($"错误码:{Error.ErrorCode}");
                }
            }
            return isSuccess;
        }

        public T Receive<T>(uint channelIndex)
        {
            return zlgOperation.Receive<T>(channelIndex);
        }

        //public T Receive<T>(uint channelIndex,uint receiveId)
        //{
        //    T ret = default;
        //    if(typeof(T) == typeof(ZCAN_Receive_Data))
        //    {
        //        var array = zlgOperation.Receive<ZCAN_Receive_Data[]>(channelIndex);
        //        if (array != null)
        //        {
        //            if (array.Length > 0)
        //            {
        //                var query = array.
        //                    Where(data => GetId(data.frame.can_id) == receiveId);
        //                var first = query.FirstOrDefault();
        //                ret = (T)Convert.ChangeType(first, typeof(T));
        //            }
        //        }
        //    }
        //    return ret;
        //}

        public (bool isSuccess,  ZCAN_Receive_Data all)Receive(uint channelIndex, uint receiveId)
        {
            var array = zlgOperation.Receive<ZCAN_Receive_Data[]>(channelIndex);
            bool isSuccess = false;
            ZCAN_Receive_Data ret = new ZCAN_Receive_Data();
            if (array != null)
            {
                if (array.Length > 0)
                {
                    var query = array.
                        Where(data => GetId(data.frame.can_id) == receiveId);
                    if(query.Count() > 0)
                    {
                        ret = query.First();
                        isSuccess = true;
                    }
                }
            }
            return (isSuccess, ret);
        }
        private uint GetId(uint canid)
        {
            return canid & 0x1FFFFFFFU;
        }
    }

    public class ZLGCANPara
    {
        public uint deviceIndex;
        public DeviceInfoIndex deviceInfoIndex;
        public KBaudrate[] kBaudrates = new KBaudrate[2];
        public FrameType[] frameType = [FrameType.Extended,FrameType.Extended];
    }
}
