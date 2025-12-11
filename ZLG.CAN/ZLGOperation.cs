using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ZLG.CAN.Models;

namespace ZLG.CAN
{
    public class ZLGOperation
    {
        private ZLGConfig config;

        //public ZLGConfig Config
        //{
        //    get { return config; }
        //    set { config = value; }
        //}

        private ErrorMessage errorMessage = new ErrorMessage();

        public ErrorMessage ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }

        private bool isDeviceOpen;

        public bool IsDeviceOpen
        {
            get { return isDeviceOpen; }
        }

        public FrameType FrameType
        {
            get { return config.FrameType; }
            set
            {
                config.FrameType = value;
            }
        }

        public TransmissionMode TransmissionMode
        {
            get { return config.TransmissionMode; }
            set
            {
                config.TransmissionMode = value;
            }
        }

        public CANFDAccelerate CANFDAccelerate
        {
            get { return config.CanFDPara.CANFDAccelerate; }
            set
            {
                config.CanFDPara.CANFDAccelerate = value;
            }
        }
        public Models.ProtocolType CanFDProtocolType
        {
            get { return config.CanFDPara.ProtocolType; }
            set
            {
                config.CanFDPara.ProtocolType = value;
            }
        }
        //private uint deviceTypeIndex;
        public void SetConfig(ZLGConfig config)
        {
            //deviceTypeIndex = (uint)deviceInfoIndex;
            this.config = config;
        }

        private const int NULL = 0;
        private const int CANFD_BRS = 0x01; /* bit rate switch (second bitrate for payload data) */
        private const int CANFD_ESI = 0x02; /* error state indicator of the transmitting node */

        /* CAN payload length and DLC definitions according to ISO 11898-1 */
        private const int CAN_MAX_DLC = 8;
        private const int CAN_MAX_DLEN = 8;
        private const int LIN_MAX_DLEN = 8;
        /* CAN FD payload length and DLC definitions according to ISO 11898-7 */
        private const int CANFD_MAX_DLC = 15;
        private const int CANFD_MAX_DLEN = 64;

        private const uint CAN_EFF_FLAG = 0x80000000U; /* EFF/SFF is set in the MSB */
        private const uint CAN_RTR_FLAG = 0x40000000U; /* remote transmission request */
        private const uint CAN_ERR_FLAG = 0x20000000U; /* error message frame */
        private const uint CAN_ID_FLAG = 0x1FFFFFFFU; /* id */

        private int channel_index_;
        private IntPtr device_handle_;
        private IntPtr channel_handle_;
        private IntPtr[] ChannelHandles;
        private IntPtr lin_channel_handle;
        private IProperty property_;
        private List<string> list_box_data_ = new List<string>();
        private static object lock_obj = new object();

        private bool isInitCAN = false;

        public bool IsInitCAN
        {
            get { return isInitCAN; }
        }

        private bool isStartCAN = false;

        public bool IsStartCAN
        {
            get { return isStartCAN; }
        }

        private bool m_bCloud = false;

        /// <summary>
        /// 打开设备
        /// </summary>
        /// <param name="deviceIndex">设备索引</param>
        public void Open(uint deviceIndex)
        {
            device_handle_ = Method.ZCAN_OpenDevice(config.DeviceInfo.device_type, deviceIndex, 0);
            ChannelHandles = new IntPtr[config.DeviceInfo.channel_count];
            if (NULL == (int)device_handle_)
            {
                isDeviceOpen = false;
                return;
            }
            if (config.DeviceInfo.device_type == Define.ZCAN_USBCANFD_200U)
            {
                string path = "0/get_cn/1";
                byte[] sn_ = new byte[30];
                IntPtr sn = Method.ZCAN_GetValue(device_handle_, path);
                Marshal.Copy(sn, sn_, 0, 30);
            }
            isDeviceOpen = true;
        }

        public bool Close()
        {
            uint ret = Method.ZCAN_CloseDevice(device_handle_);
            if(ret == 1)
            {
                isDeviceOpen = false;
                isInitCAN = false;
                isStartCAN = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void InitCAN()
        {
            if (!isDeviceOpen)
            {
                isInitCAN = false;
                return;
            }
            uint type = config.DeviceInfo.device_type;
            channel_index_ = config.ChannelIndex;
            bool netDevice = type == Define.ZCAN_CANETTCP || type == Define.ZCAN_CANETUDP || type == Define.ZCAN_CANWIFI_TCP ||
                type == Define.ZCAN_CANFDNET_400U_TCP || type == Define.ZCAN_CANFDNET_400U_UDP ||
                type == Define.ZCAN_CANFDNET_200U_TCP || type == Define.ZCAN_CANFDNET_200U_UDP || type == Define.ZCAN_CANFDNET_800U_TCP ||
                type == Define.ZCAN_CANFDNET_800U_UDP;
            bool canfdnetDevice = type == Define.ZCAN_CANFDNET_400U_TCP || type == Define.ZCAN_CANFDNET_400U_UDP ||
                type == Define.ZCAN_CANFDNET_200U_TCP || type == Define.ZCAN_CANFDNET_200U_UDP || type == Define.ZCAN_CANFDNET_800U_TCP ||
                type == Define.ZCAN_CANFDNET_800U_UDP;
            bool pcieCanfd = type == Define.ZCAN_PCIECANFD_100U ||
                type == Define.ZCAN_PCIECANFD_200U ||
                type == Define.ZCAN_PCIECANFD_400U ||
                type == Define.ZCAN_PCIECANFD_200U_EX;
            bool usbCanfd = type == Define.ZCAN_USBCANFD_100U ||
                type == Define.ZCAN_USBCANFD_200U ||
                type == Define.ZCAN_USBCANFD_400U ||
                type == Define.ZCAN_USBCANFD_MINI ||
                type == Define.ZCAN_USBCANFD_800U;
            bool canfdDevice = usbCanfd || pcieCanfd;
            if (usbCanfd && (type != Define.ZCAN_USBCANFD_800U))
            {
                if (!setCANFDStandard((int)config.CanFDPara.Standard)) //设置CANFD标准
                {
                    errorMessage = new ErrorMessage
                    {
                        Name = "设置CANFD标准失败",
                        Description = $"设备类型:{type.ToString()}"
                    };
                    return;
                }
            }
            if (!canfdDevice)
            {
                if (!setBaudrate((uint)config.Baudrate))
                {
                    errorMessage = new ErrorMessage
                    {
                        Name = "设置波特率失败",
                        Description = $"设备类型:{type.ToString()}"
                    };
                    return;
                }
            }
            else
            {
                bool result = true;
                if (usbCanfd)
                {
                    if (type == Define.ZCAN_USBCANFD_200U || type == Define.ZCAN_USBCANFD_400U || type == Define.ZCAN_USBCANFD_800U)
                    {
                        //不使用此功能
                        //if (!setDataMerge())
                        //{
                        //    errorMessage = new ErrorMessage
                        //    {
                        //        ErrorName = "合并设置失败",
                        //        ErrorDescription = $"设备类型:{type.ToString()}"
                        //    };
                        //    return;
                        //}
                    }
                    result = setFdBaudrate((uint)config.USBCANFDAbit, (uint)config.USBCANFDDbit);
                }
                else if (pcieCanfd)
                {
                    result = setFdBaudrate((uint)config.PCIECANFDAbit, (uint)config.PCIECANFDDbit);
                    if (type == Define.ZCAN_PCIECANFD_400U || type == Define.ZCAN_PCIECANFD_200U_EX)
                    {
                        //if (!setDataMerge())
                        //{
                        //    errorMessage = new ErrorMessage
                        //    {
                        //        ErrorName = "合并设置失败",
                        //        ErrorDescription = $"设备类型:{type.ToString()}"
                        //    };
                        //    return;
                        //}
                    }
                }
                if (!result)
                {
                    errorMessage = new ErrorMessage
                    {
                        Name = "合并波特率失败",
                        Description = $"设备类型:{type.ToString()}"
                    };
                    isInitCAN = false;
                    return;
                }
            }
            ZCAN_CHANNEL_INIT_CONFIG config_ = new ZCAN_CHANNEL_INIT_CONFIG();
            if (!netDevice)
            {
                //0为正常
                config_.canfd.mode = 0;
                if (usbCanfd)
                {
                    config_.can_type = Define.TYPE_CANFD;
                    config_.canfd.mode = 0;
                }
                else if (pcieCanfd)
                {
                    config_.can_type = Define.TYPE_CANFD;
                    config_.canfd.filter = 0;
                    config_.canfd.acc_code = 0;
                    config_.canfd.acc_mask = 0xFFFFFFFF;
                    config_.canfd.mode = 0;
                }
                else
                {
                    config_.can_type = Define.TYPE_CAN;
                    config_.can.filter = 0;
                    config_.can.acc_code = 0;
                    config_.can.acc_mask = 0xFFFFFFFF;
                    config_.can.mode = 0;
                }
            }
            IntPtr pConfig = Marshal.AllocHGlobal(Marshal.SizeOf(config_));
            Marshal.StructureToPtr(config_, pConfig, true);

            channel_handle_ = Method.ZCAN_InitCAN(device_handle_, (uint)channel_index_, pConfig);
            ChannelHandles[channel_index_] = channel_handle_;
            Marshal.FreeHGlobal(pConfig);

            if (canfdnetDevice)  //CANFDNET设备开启合并接收
            {
                if (!setDataMerge())
                {
                    errorMessage = new ErrorMessage
                    {
                        Name = "合并设置失败",
                        Description = $"设备类型:{type.ToString()}"
                    };
                    isInitCAN = false;
                    return;
                }
            }

            if (NULL == (int)channel_handle_)
            {
                errorMessage = new ErrorMessage
                {
                    Name = "初始化CAN失败",
                    Description = $"设备类型:{type.ToString()}"
                };
                isInitCAN = false;
                return;
            }

            if (!netDevice)
            {
                if (usbCanfd && config.CanFDPara.TREnable)
                {
                    if (!setResistanceEnable())
                    {
                        errorMessage = new ErrorMessage
                        {
                            Name = "使能终端电阻失败",
                            Description = $"设备类型:{type.ToString()}"
                        };
                        isInitCAN = false;
                        return;
                    }
                }

                if (usbCanfd && !setFilter())
                {
                    //MessageBox.Show("设置滤波失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    errorMessage = new ErrorMessage
                    {
                        Name = "设置滤波失败",
                        Description = $"设备类型:{type.ToString()}"
                    };
                    isInitCAN = false;
                    return;
                }
                if (type == Define.ZCAN_USBCAN_2E_U || type == Define.ZCAN_USBCAN_E_U)
                {
                    if (!setFilter())
                    {
                        errorMessage = new ErrorMessage
                        {
                            Name = "设置滤波失败",
                            Description = $"设备类型:{type.ToString()}"
                        };
                        isInitCAN = false;
                        return;
                    }
                }
                isInitCAN = true;
            }
        }

        public void StartCAN()
        {
            if (!isInitCAN)
            {
                return;
            }
            if (Method.ZCAN_StartCAN(channel_handle_) != Define.STATUS_OK)
            {
                errorMessage = new ErrorMessage
                {
                    Name = "启动CAN失败",
                    Description = $"设备类型:{config.DeviceInfo.device_type}"
                };
                isStartCAN = false;
                return;
            }
            uint type = config.DeviceInfo.device_type;   //增加CANFDNET滤波,CANFDNET滤波必须在startcan之后进行。
            bool canfdnetDevice = type == Define.ZCAN_CANFDNET_400U_TCP || type == Define.ZCAN_CANFDNET_400U_UDP ||
                             type == Define.ZCAN_CANFDNET_200U_TCP || type == Define.ZCAN_CANFDNET_200U_UDP || type == Define.ZCAN_CANFDNET_800U_TCP ||
                             type == Define.ZCAN_CANFDNET_800U_UDP;
            if (canfdnetDevice && !setFilter())
            {
                errorMessage = new ErrorMessage
                {
                    Name = "设置滤波失败",
                    Description = $"设备类型:{type.ToString()}{Environment.NewLine}" +
                    $"增加CANFDNET滤波,CANFDNET滤波必须在startcan之后进行"
                };
                isStartCAN = false;
                return;
            }
            isStartCAN = true;
        }

        public bool Send(uint canId, uint channelIndex, string strData)
        {
            uint id = canId;
            string data = strData;
            int frame_type_index = (int)config.FrameType;
            int send_type_index = (int)config.TransmissionMode;
            int canfd_exp_index = (int)config.CanFDPara.CANFDAccelerate;
            int protocol_index = (int)config.CanFDPara.ProtocolType;
            uint result; //发送的帧数
            channel_handle_ = ChannelHandles[channelIndex];
            if (config.IsDataMerge != true)
            {
                if (0 == protocol_index) //can
                {
                    ZCAN_Transmit_Data can_data = new ZCAN_Transmit_Data();
                    can_data.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data.frame.data = new byte[8];
                    can_data.frame.can_dlc = (byte)SplitData(data, ref can_data.frame.data, CAN_MAX_DLEN);
                    can_data.transmit_type = (uint)send_type_index;
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(can_data));
                    Marshal.StructureToPtr(can_data, ptr, true);
                    result = Method.ZCAN_Transmit(channel_handle_, ptr, 1);
                    Marshal.FreeHGlobal(ptr);
                    /*
                     //////////////////////////////////////////////////////////////////////////////// 实现多帧发送的代码
                    ZCAN_Transmit_Data[] can_data = new ZCAN_Transmit_Data[2];
                    can_data[0].frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data[0].frame.data = new byte[8];
                    can_data[0].frame.can_dlc = (byte)SplitData(data, ref  can_data[0].frame.data, CAN_MAX_DLEN);
                    can_data[0].transmit_type = (uint)send_type_index;
                    can_data[1].frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data[1].frame.data = new byte[8];
                    can_data[1].frame.can_dlc = (byte)SplitData(data, ref  can_data[1].frame.data, CAN_MAX_DLEN);
                    can_data[1].transmit_type = (uint)send_type_index;
                    int size = Marshal.SizeOf(typeof(ZCAN_Transmit_Data));
                    IntPtr ptr = Marshal.AllocHGlobal(size*2);

                    for (int i = 0; i < 2; i++)
                    {
                        Marshal.StructureToPtr(can_data[i], (IntPtr)(ptr + i * size), true);
                    }

                    result = Method.ZCAN_Transmit(channel_handle_, ptr, 2);
                    Marshal.FreeHGlobal(ptr); */
                }
                else //canfd
                {
                    ZCAN_TransmitFD_Data canfd_data = new ZCAN_TransmitFD_Data();
                    canfd_data.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    canfd_data.frame.data = new byte[64];
                    canfd_data.frame.len = (byte)SplitData(data, ref canfd_data.frame.data, CANFD_MAX_DLEN);
                    canfd_data.transmit_type = (uint)send_type_index;
                    canfd_data.frame.flags = (byte)((canfd_exp_index != 0) ? CANFD_BRS : 0);
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(canfd_data));
                    Marshal.StructureToPtr(canfd_data, ptr, true);
                    result = Method.ZCAN_TransmitFD(channel_handle_, ptr, 1);
                    Marshal.FreeHGlobal(ptr);
                }
            }
            else
            {
                ZCANDataObj data_ = new ZCANDataObj();
                data_.chnl = (byte)channel_index_;
                data_.dataType = 1;  //can/canfd 报文
                if (protocol_index == 1)
                    data_.zcanCANFDData.flag = data_.zcanCANFDData.flag | flag.CANFD_FLAG;  //如果选择了CANFD类型，按位或上 CANFD标志位
                data_.zcanCANFDData.flag = data_.zcanCANFDData.flag | flag.TXECHOREQUEST_FLAG; //发送回显标志位开启，合并发送的数据会回到接收缓存，如果不需要可以手动注释掉。
                data_.zcanCANFDData.frame.flags = (byte)((canfd_exp_index != 0) ? CANFD_BRS : 0); //加速标志
                data_.zcanCANFDData.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                data_.zcanCANFDData.frame.data = new byte[64];
                data_.zcanCANFDData.frame.len = (byte)SplitData(data, ref data_.zcanCANFDData.frame.data, CANFD_MAX_DLEN); //拷贝数据段顺便获取数据段长度
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data_));
                Marshal.StructureToPtr(data_, ptr, true);
                result = Method.ZCAN_TransmitData(device_handle_, ptr, 1);
                Marshal.FreeHGlobal(ptr);
            }

            if (result != 1)
            {
                //MessageBox.Show("发送数据失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                AddErr();
                return false;
            }
            return true;
        }

        public uint ClearBuffer(uint channelIndex)
        {
            var channelHandle = ChannelHandles[channelIndex];
            return Method.ZCAN_ClearBuffer(channelHandle);
        }

        public bool Send(uint canId, uint channelIndex, byte[] data)
        {
            uint id = canId;
            //string data = strData;
            int frame_type_index = (int)config.FrameType;
            int send_type_index = (int)config.TransmissionMode;
            int canfd_exp_index = (int)config.CanFDPara.CANFDAccelerate;
            int protocol_index = (int)config.CanFDPara.ProtocolType;
            uint result; //发送的帧数
            channel_handle_ = ChannelHandles[channelIndex];
            if (config.IsDataMerge != true)
            {
                if (0 == protocol_index) //can
                {
                    ZCAN_Transmit_Data can_data = new ZCAN_Transmit_Data();
                    can_data.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data.frame.data = new byte[8];
                    can_data.frame.can_dlc = (byte)GetData(data, ref can_data.frame.data, CAN_MAX_DLEN);
                    can_data.transmit_type = (uint)send_type_index;
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(can_data));
                    Marshal.StructureToPtr(can_data, ptr, true);
                    result = Method.ZCAN_Transmit(channel_handle_, ptr, 1);
                    Marshal.FreeHGlobal(ptr);
                    /*
                     //////////////////////////////////////////////////////////////////////////////// 实现多帧发送的代码
                    ZCAN_Transmit_Data[] can_data = new ZCAN_Transmit_Data[2];
                    can_data[0].frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data[0].frame.data = new byte[8];
                    can_data[0].frame.can_dlc = (byte)SplitData(data, ref  can_data[0].frame.data, CAN_MAX_DLEN);
                    can_data[0].transmit_type = (uint)send_type_index;
                    can_data[1].frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    can_data[1].frame.data = new byte[8];
                    can_data[1].frame.can_dlc = (byte)SplitData(data, ref  can_data[1].frame.data, CAN_MAX_DLEN);
                    can_data[1].transmit_type = (uint)send_type_index;
                    int size = Marshal.SizeOf(typeof(ZCAN_Transmit_Data));
                    IntPtr ptr = Marshal.AllocHGlobal(size*2);

                    for (int i = 0; i < 2; i++)
                    {
                        Marshal.StructureToPtr(can_data[i], (IntPtr)(ptr + i * size), true);
                    }

                    result = Method.ZCAN_Transmit(channel_handle_, ptr, 2);
                    Marshal.FreeHGlobal(ptr); */
                }
                else //canfd
                {
                    ZCAN_TransmitFD_Data canfd_data = new ZCAN_TransmitFD_Data();
                    canfd_data.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                    canfd_data.frame.data = new byte[64];
                    canfd_data.frame.len = (byte)GetData(data, ref canfd_data.frame.data, CANFD_MAX_DLEN);
                    canfd_data.transmit_type = (uint)send_type_index;
                    canfd_data.frame.flags = (byte)((canfd_exp_index != 0) ? CANFD_BRS : 0);
                    IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(canfd_data));
                    Marshal.StructureToPtr(canfd_data, ptr, true);
                    result = Method.ZCAN_TransmitFD(channel_handle_, ptr, 1);
                    Marshal.FreeHGlobal(ptr);
                }
            }
            else
            {
                ZCANDataObj data_ = new ZCANDataObj();
                data_.chnl = (byte)channel_index_;
                data_.dataType = 1;  //can/canfd 报文
                if (protocol_index == 1)
                    data_.zcanCANFDData.flag = data_.zcanCANFDData.flag | flag.CANFD_FLAG;  //如果选择了CANFD类型，按位或上 CANFD标志位
                data_.zcanCANFDData.flag = data_.zcanCANFDData.flag | flag.TXECHOREQUEST_FLAG; //发送回显标志位开启，合并发送的数据会回到接收缓存，如果不需要可以手动注释掉。
                data_.zcanCANFDData.frame.flags = (byte)((canfd_exp_index != 0) ? CANFD_BRS : 0); //加速标志
                data_.zcanCANFDData.frame.can_id = MakeCanId(id, frame_type_index, 0, 0);
                data_.zcanCANFDData.frame.data = new byte[64];
                data_.zcanCANFDData.frame.len = (byte)GetData(data, ref data_.zcanCANFDData.frame.data, CANFD_MAX_DLEN); //拷贝数据段顺便获取数据段长度
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(data_));
                Marshal.StructureToPtr(data_, ptr, true);
                result = Method.ZCAN_TransmitData(device_handle_, ptr, 1);
                Marshal.FreeHGlobal(ptr);
            }

            if (result != 1)
            {
                //MessageBox.Show("发送数据失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                AddErr();
                return false;
            }
            return true;
        }

        private const int TYPE_CAN = 0;
        private const int TYPE_CANFD = 1;

        public T Receive<T>(uint channelIndex)
        {
            ZCAN_Receive_Data[] can_data = new ZCAN_Receive_Data[0];
            ZCAN_ReceiveFD_Data[] canfd_data = new ZCAN_ReceiveFD_Data[0];
            ZCANDataObj[] data_obj = new ZCANDataObj[10000];
            ZCAN_LIN_MSG[] lin_data = new ZCAN_LIN_MSG[10000];
            channel_handle_ = ChannelHandles[channelIndex];
            uint len = 0;
            T res = default(T);
            if (!config.IsDataMerge)
            { //分开接收
                if (typeof(T) == typeof(ZCAN_Receive_Data[]))
                {
                    len = Method.ZCAN_GetReceiveNum(channel_handle_, TYPE_CAN);

                    if (len > 0)
                    {
                        int size = Marshal.SizeOf(typeof(ZCAN_Receive_Data));
                        IntPtr ptr = Marshal.AllocHGlobal((int)len * size);
                        len = Method.ZCAN_Receive(channel_handle_, ptr, len, 50);
                        can_data = new ZCAN_Receive_Data[len];
                        for (int i = 0; i < len; ++i)
                        {
                            can_data[i] = (ZCAN_Receive_Data)Marshal.PtrToStructure(
                                (IntPtr)((Int64)ptr + i * size), typeof(ZCAN_Receive_Data));
                        }
                        Marshal.FreeHGlobal(ptr);
                    }
                    res = (T)(object)can_data;
                }
                if (typeof(T) == typeof(ZCAN_ReceiveFD_Data[]))
                {
                    len = Method.ZCAN_GetReceiveNum(channel_handle_, TYPE_CANFD);
                    if (len > 0)
                    {
                        int size = Marshal.SizeOf(typeof(ZCAN_ReceiveFD_Data));
                        IntPtr ptr = Marshal.AllocHGlobal((int)len * size);
                        len = Method.ZCAN_ReceiveFD(channel_handle_, ptr, len, 50);
                        canfd_data = new ZCAN_ReceiveFD_Data[len];
                        for (int i = 0; i < len; ++i)
                        {
                            canfd_data[i] = (ZCAN_ReceiveFD_Data)Marshal.PtrToStructure(
                                (IntPtr)((Int64)ptr + i * size), typeof(ZCAN_ReceiveFD_Data));
                        }
                        Marshal.FreeHGlobal(ptr);
                    }
                    res = (T)(object)canfd_data;
                }
            }
            else
            { //合并接收
                len = Method.ZCAN_GetReceiveNum(channel_handle_, 2); //合并接收类型type为2
                if (len > 0)
                {
                    int size = Marshal.SizeOf(typeof(ZCANDataObj));
                    IntPtr ptr = Marshal.AllocHGlobal((int)100 * size);
                    len = Method.ZCAN_ReceiveData(device_handle_, ptr, 100, 50);         //传设备的句柄
                    for (int i = 0; i < len; ++i)
                    {
                        data_obj[i] = (ZCANDataObj)Marshal.PtrToStructure(
                            (IntPtr)((Int64)ptr + i * size), typeof(ZCANDataObj));
                    }
                    Marshal.FreeHGlobal(ptr);
                }
                res = (T)(object)data_obj;
            }
            return res;
        }

        private void AddErr()
        {
            ZCAN_CHANNEL_ERROR_INFO pErrInfo = new ZCAN_CHANNEL_ERROR_INFO();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(pErrInfo));
            //Marshal.StructureToPtr(pErrInfo, ptr, true);
            if (Method.ZCAN_ReadChannelErrInfo(channel_handle_, ptr) != Define.STATUS_OK)
            {
                errorMessage = new ErrorMessage()
                {
                    Name = "获取错误信息失败",
                };
            }
            pErrInfo = (ZCAN_CHANNEL_ERROR_INFO)Marshal.PtrToStructure(ptr, typeof(ZCAN_CHANNEL_ERROR_INFO));
            Marshal.FreeHGlobal(ptr);
            errorMessage.ErrorCode = $"错误码：{pErrInfo.error_code:D1}";
        }

        //拆分text到发送data数组
        private int SplitData(string data, ref byte[] transData, int maxLen)
        {
            string[] dataArray = data.Split(' ');
            for (int i = 0; (i < maxLen) && (i < dataArray.Length); i++)
            {
                transData[i] = Convert.ToByte(dataArray[i].Substring(0, 2), 16);
            }

            return dataArray.Length;
        }

        private int GetData(byte[] data, ref byte[] transData, int maxLen)
        {
            for (int i = 0; (i < maxLen) && (i < data.Length); i++)
            {
                transData[i] = data[i];
            }

            return data.Length;
        }

        public uint MakeCanId(uint id, int eff, int rtr, int err)//1:extend frame 0:standard frame
        {
            uint ueff = (uint)(!!(Convert.ToBoolean(eff)) ? 1 : 0);
            uint urtr = (uint)(!!(Convert.ToBoolean(rtr)) ? 1 : 0);
            uint uerr = (uint)(!!(Convert.ToBoolean(err)) ? 1 : 0);
            return id | ueff << 31 | urtr << 30 | uerr << 29;
        }

        private bool setFilter()
        {
            string path = channel_index_ + "/filter_clear";//清除滤波
            string value = "0";

            if (0 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index_ + "/filter_mode";
            value = Convert.ToString((int)config.CanFDPara.Filter.FilterType);
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (value == "2")
            {
                //path = channel_index_ + "/filter_mode";
                //value ="0";
                //Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
                //path = channel_index_ + "/filter_start";
                //value ="0";
                //Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
                //path = channel_index_ + "/filter_end";
                //value = "0x7ff";
                //Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
                //path = channel_index_ + "/filter_ack";//滤波生效
                //value = "0";
                //Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
                return true;
            }
            if (0 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index_ + "/filter_start";
            value = config.CanFDPara.Filter.StartID;
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index_ + "/filter_end";
            value = config.CanFDPara.Filter.EndID;
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            path = channel_index_ + "/filter_ack";//滤波生效
            value = "0";
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            if (0 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }

            //如果要设置多条滤波，在清除滤波和滤波生效之间设置多条滤波即可
            return true;
        }

        //设置终端电阻使能
        private bool setResistanceEnable()
        {
            string path = channel_index_ + "/initenal_resistance";
            string value = (config.CanFDPara.TREnable ? "1" : "0");
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            return 1 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
        }

        //设置CANFD标准
        private bool setCANFDStandard(int canfd_standard)
        {
            string path = channel_index_ + "/canfd_standard";
            string value = canfd_standard.ToString();
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            uint ret = Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
            return (ret == 1);
        }

        //设置波特率
        private bool setBaudrate(UInt32 baud)
        {
            string path = channel_index_ + "/baud_rate";
            string value = baud.ToString();
            //char* pathCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(path).ToPointer();
            //char* valueCh = (char*)System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(value).ToPointer();
            return 1 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
        }

        private bool setFdBaudrate(UInt32 abaud, UInt32 dbaud)
        {
            string path = channel_index_ + "/canfd_abit_baud_rate";
            string value = abaud.ToString();
            if (1 != Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            path = channel_index_ + "/canfd_dbit_baud_rate";
            value = dbaud.ToString();
            if (1 != Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value)))
            {
                return false;
            }
            return true;
        }

        public uint GetReceiveNum(uint channelIndex, byte type)
        {
            var channelHandle = ChannelHandles[channelIndex];
            return Method.ZCAN_GetReceiveNum(channelHandle, type);
        }

        //设置开启合并接收
        private bool setDataMerge()
        {
            byte merge_ = 0;
            if (config.IsDataMerge) merge_ = 1;
            string path = channel_index_ + "/set_device_recv_merge";
            string value = merge_.ToString();
            return 1 == Method.ZCAN_SetValue(device_handle_, path, Encoding.ASCII.GetBytes(value));
        }
    }
}