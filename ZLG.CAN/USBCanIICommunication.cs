using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
        public bool Open()
        {
            // 1. 先尝试执行正常的初始化流程
            if (TryInitializeHardware())
            {
                return IsOpen = true;
            }

            // 🌟 2. 如果失败，说明底层由于频繁开闭或Bus-Off彻底锁死，触发【设备级硬件硬复位】
            LogInfo?.Invoke("⚠️ 检测到CAN通道锁死或初始化失败，正在触发物理层硬复位重试...");

            // 强制关闭设备释放所有底层句柄（哪怕底层已经坏掉）
            zlgOperation.Close();
            Thread.Sleep(100); // 留出物理放电/驱动释放时间

            // 🌟 3. 重新建立全新的设备与通道生命周期
            if (TryInitializeHardware())
            {
                LogInfo?.Invoke("🎉 硬件硬复位成功，CAN通道已恢复正常！");
                return IsOpen = true;
            }

            LogInfo?.Invoke("❌ 硬件硬复位后依然无法初始化，请检查USB连线、供电或终端电阻。");
            return IsOpen = false;
        }

        /// <summary>
        /// 提取出的核心初始化逻辑
        /// </summary>
        private bool TryInitializeHardware()
        {
            try
            {
                // ================== 通道 0 初始化 ==================
                ZLGConfig config0 = new ZLGConfig(para.deviceInfoIndex, 0, para.kBaudrates[0], para.frameType[0]);
                zlgOperation.SetConfig(config0);
                zlgOperation.Open(para.deviceIndex);

                if (!zlgOperation.IsDeviceOpen) return false;

                // 此时如果能初始化，先调用Init
                zlgOperation.InitCAN();
                if (!zlgOperation.IsInitCAN)
                {
                    // 🌟 只有在 InitCAN 成功之后，或者明确有旧句柄时，ResetCAN 才有效
                    // 如果这里失败了，说明句柄根本没初始化成功，调 ResetCAN(0) 也没用
                    return false;
                }

                zlgOperation.StartCAN();
                if (!zlgOperation.IsStartCAN) return false;

                // ================== 通道 1 初始化 ==================
                ZLGConfig config1 = new ZLGConfig(para.deviceInfoIndex, 1, para.kBaudrates[1], para.frameType[1]);
                zlgOperation.SetConfig(config1);

                zlgOperation.InitCAN();
                if (!zlgOperation.IsInitCAN) return false;

                zlgOperation.StartCAN();
                if (!zlgOperation.IsStartCAN) return false;

                return true;
            }
            catch (Exception ex)
            {
                LogInfo?.Invoke($"初始化期间发生异常: {ex.Message}");
                return false;
            }
        }

        public bool Close()
        {
            return zlgOperation.Close();
        }

        public void SetPara(ZLGCANPara para)
        {
            this.para = para;
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        {
            zlgOperation.FrameType = canId >= 0x7FF ? FrameType.Extended : FrameType.Standard;
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
                Console.WriteLine($"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}失败");
                Error = zlgOperation.ErrorMessage;
                if(LogInfo != null)
                {
                    LogInfo($"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{strData}失败");
                    LogInfo($"错误码:{Error.ErrorCode}");
                }

            }
            return isSuccess;
        }

        public bool Send(uint canId, uint channelIndex, byte[] data)
        {
            zlgOperation.FrameType = canId >= 0x7FF ? FrameType.Extended : FrameType.Standard;
            bool isSuccess = zlgOperation.Send(canId, channelIndex, data);
            if (isSuccess)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} " +
                    $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
                if (LogInfo != null)
                {
                    LogInfo(
                    $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}");
                }
                
            }
            else
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} "
                    + $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}失败");
                if(LogInfo != null)
                {
                    LogInfo(
                    $"{para.deviceInfoIndex} CanId:0x{canId.ToString("X")},通道:{channelIndex} 发送:{BitConverter.ToString(data)}失败");
                    LogInfo($"错误码:{Error.ErrorCode}");
                }
            }
            return isSuccess;
        }

        public T Receive<T>(uint channelIndex)
        {
            return zlgOperation.Receive<T>(channelIndex);
        }

        public uint GetReceiveNum(uint channelIndex)
        {
            return zlgOperation.GetReceiveNum(channelIndex, 0);
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

        public void ClearBuffer(uint channelIndex)
        {
            zlgOperation.ClearBuffer(channelIndex);
            LogInfo?.Invoke($"清除通道:{channelIndex} 接收缓存");
        }

        public (bool isSuccess, ZCAN_Receive_Data all)Receive(uint channelIndex, uint receiveId)
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
                        ret = query.Last();
                        if(LogInfo != null)
                        {
                            LogInfo($"接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                            $"通道:{channelIndex}," +
                            $"数据:{BitConverter.ToString(ret.frame.data)}");
                        }
                        isSuccess = true;
                    }
                }
            }
            if (!isSuccess)
            {
                if (LogInfo != null)
                {
                    LogInfo($"接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                    $"通道:{channelIndex}," +
                    $"未接收到任何数据");
                }
            }
            return (isSuccess, ret);
        }
        /// <summary>
        /// 接收同一CanId的多条报文
        /// </summary>
        /// <param name="channelIndex"></param>
        /// <param name="receiveId"></param>
        /// <returns></returns>
        public (bool isSuccess, ZCAN_Receive_Data[] all) ReceiveMulti(uint channelIndex, uint receiveId)
        {
            var array = zlgOperation.Receive<ZCAN_Receive_Data[]>(channelIndex);
            bool isSuccess = false;
            ZCAN_Receive_Data[] rets = new ZCAN_Receive_Data[0];
            if (array != null)
            {
                if (array.Length > 0)
                {
                    var query = array.
                        Where(data => GetId(data.frame.can_id) == receiveId);
                    if (query.Count() > 0)
                    {
                        rets = query.ToArray();
                        foreach (var ret in rets)
                        {
                            if (LogInfo != null)
                            {
                                LogInfo($"接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                                $"通道:{channelIndex}," +
                                $"数据:{BitConverter.ToString(ret.frame.data)}");
                            }
                        }
                        isSuccess = true;
                    }
                }
            }
            if (!isSuccess)
            {
                if (LogInfo != null)
                {
                    LogInfo($"接收CanID: 0x{GetId(receiveId).ToString("X")}," +
                    $"通道:{channelIndex}," +
                    $"未接收到任何数据");
                }
            }
            return (isSuccess, rets);
        }

        public List<(bool isSuccess, ZCAN_Receive_Data all)> Receive(uint channelIndex, uint[] receiveIds)
        {
            var array = zlgOperation.Receive<ZCAN_Receive_Data[]>(channelIndex);
            List < (bool isSuccess, ZCAN_Receive_Data all) > rets = new List<(bool isSuccess, ZCAN_Receive_Data all)>();
            foreach (var receiveId in receiveIds)
            {
                bool isSuccess = false;
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
                            if (LogInfo != null)
                            {
                                LogInfo($"接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                                $"通道:{channelIndex}," +
                                $"数据:{BitConverter.ToString(ret.frame.data)}");
                            }
                            isSuccess = true;
                        }
                    }
                }
                if (!isSuccess)
                {
                    if (LogInfo != null)
                    {
                        LogInfo($"接收CanID: 0x{GetId(ret.frame.can_id).ToString("X")}," +
                        $"通道:{channelIndex}," +
                        $"未接收到任何数据");
                    }
                }
                rets.Add((isSuccess,ret));
            }
            
            return rets;
        }

        public int ReceiveInplace(uint channelIndex, ZCAN_Receive_Data[] data, int waitTime = 50)
        {
            return zlgOperation.ReceiveInplace(channelIndex, data, waitTime);
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
