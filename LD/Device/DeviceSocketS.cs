using System;
using System.Collections;
using System.ComponentModel;


namespace LD.Device
{

    /// <summary>
    /// Plc�豸
    /// </summary>
    public class DeviceSocketS : Device
    {

        /// <summary>
        /// Socket��
        /// </summary>
        public SortedList Sokcets = new SortedList();


        /// <summary>
        /// ˽�й��캯��
        /// </summary>
        public DeviceSocketS(Config.ConfigSocketS config)
        {
            this.config = config;
        }


        /// <summary>
        /// plc���ñ�
        /// </summary>
        private Config.ConfigSocketS config
        {
            set;
            get;
        }







        /// <summary>
        /// ��ʼ��
        /// </summary>
        public override void DoInit()
        {
            try
            {
                foreach (Config.SocketSItem item in this.config.SocketSItems)
                {
                    if (!item.IsActive) continue;

                    Bp.Socket.Server server = new Bp.Socket.Server(item .IP ,item.Port ,item );
                    server.TimeOut = item.OutTime;
                    server.TimeHeart = item.HeartSecond;
                    Sokcets.Add(item.SocketDevice, server);
                }

                IDictionaryEnumerator enumerator = this.Sokcets.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Bp.Socket.Server socket = enumerator.Value as Bp.Socket.Server;
                    socket.OnSocketReceive += new Bp.Socket.Server.SocketReceiveDelegate(socket_OnSocketReceive);
                    socket.OnSocketItemChange += new Bp.Socket.Server.SocketItemChange(socket_OnSocketItemChange);
                    socket.DoInit();
                }

            }
            catch (Exception ex)
            {
                throw new InitException(this.ToString(), ex.ToString());
            }
        }

        void socket_OnSocketItemChange(object device, BindingList<Bp.Socket.Server.SocketItem> Sockets)
        {
            try
            {
                Config.SocketSItem si = (Config.SocketSItem)device;
                si.SocketClients = new BindingList<Bp.Socket.Server.SocketItem>();
                foreach (Bp.Socket.Server.SocketItem it in Sockets)
                {
                    si.SocketClients.Add(it);
                }
            }
            catch { }
        }


        void socket_OnSocketReceive(object devicetype, System.Net.Sockets.Socket socket, byte[] dataB, string dataS)
        {
            
            Logic .SocketSHandle .Instance .socket_OnSocketReceive ( (Config .SocketSItem ) devicetype ,socket ,dataB ,dataS);
        }



        /// <summary>
        /// ����
        /// </summary>
        public override void DoStart()
        {
            try
            {
                IDictionaryEnumerator enumerator = this.Sokcets.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Bp.Socket.Server socket = enumerator.Value as Bp.Socket.Server;
                    socket.DoStart();

                    ((Config.SocketSItem)socket.DeviceType).IsConnected = true;
                    
                }
            }
            catch (Exception ex)
            {
                throw new StartException(this.ToString(), ex.ToString());
            }
        }

        /// <summary>
        /// ֹͣ
        /// </summary>
        public override void DoStop()
        {
            try
            {
                IDictionaryEnumerator enumerator = this.Sokcets.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Bp.Socket.Server socket = enumerator.Value as Bp.Socket.Server;
                    socket.DoStop();
                }
            }
            catch (Exception ex)
            {
                throw new StopException(this.ToString(), ex.ToString());
            }
        }

        /// <summary>
        /// �ͷ�
        /// </summary>
        public override void DoRelease()
        {
            try
            {
                IDictionaryEnumerator enumerator = this.Sokcets.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    Bp.Socket.Server socket = enumerator.Value as Bp.Socket.Server;
                    socket.DoRelease();
                }
            }
            catch (Exception ex)
            {
                throw new ReleaseException(this.ToString(), ex.ToString());
            }
        }

        /// <summary>
        /// �豸����
        /// </summary>
        public override string DeviceType
        {
            get
            {
                return "DeviceSocketS";
            }
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.DeviceType;
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data"></param>
        public void SocketSend(Common.SocketDevice device, string data)
        {
            Bp.Socket.Server socket = (Bp.Socket.Server)Sokcets[device];
            socket.SendData(data);
        }

        /// <summary>
        /// ��������
        /// </summary>
        /// <param name="device"></param>
        /// <param name="data"></param>
        public void SocketSend(Common.SocketDevice device, byte[] data)
        {
            Bp.Socket.Server socket = (Bp.Socket.Server)Sokcets[device];
            socket.SendData(data);
        }


    }
}
