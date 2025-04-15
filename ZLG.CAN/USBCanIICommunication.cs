using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLG.CAN.Models;

namespace ZLG.CAN
{
    public class USBCanIICommunication
    {
        ZLGOperation zlgOperation = new ZLGOperation();

        public bool IsOpen { get; set; }
        public ErrorMessage Error { get; set; } = new ErrorMessage();
        public ZLGCANPara CanPara { get { return para; } }
        private ZLGCANPara para = new ZLGCANPara();
        public void Open()
        {
            ZLGConfig config = new ZLGConfig(para.deviceInfoIndex, 0, para.kBaudrates[0], para.frameType[0]);
            zlgOperation.SetConfig(config);
            zlgOperation.Open(para.deviceIndex);
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

            config = new ZLGConfig(para.deviceInfoIndex, 1, para.kBaudrates[1], para.frameType[1]);
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

        public void SetPara(ZLGCANPara para)
        {
            this.para = para;
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        { 
            zlgOperation.FrameType = para.frameType[channelIndex];
            return zlgOperation.Send(canId, channelIndex, strData);
        }

        public bool Send(uint canId, uint channelIndex, byte[] data)
        {
            zlgOperation.FrameType = para.frameType[channelIndex];
            bool isSuccess = zlgOperation.Send(canId, channelIndex, data);
            if (isSuccess)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " +
                    $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} "
                    + $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送失败");
            }
            return isSuccess;
        }

        public T Receive<T>(uint channelIndex)
        {
            return zlgOperation.Receive<T>(channelIndex);
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
                    ret = query.First();
                }
            }
            return ret;
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
