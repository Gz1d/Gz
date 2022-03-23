using System;
using System.Threading;
using System.IO;


namespace LD.Device
{

    /// <summary>
    /// Plc�豸
    /// </summary>
    public class DeviceSystem : Device
    {
        /// <summary>
        /// ˽�й��캯��
        /// </summary>
        public DeviceSystem(Config.ConfigSystem config)
        {
            this.config = config;
        }

        /// <summary>
        /// plc���ñ�
        /// </summary>
        private Config.ConfigSystem config
        {
            set;
            get;
        }

        /// <summary>
        /// ȡ���߳�
        /// </summary>
        private Thread UpdateThread { set; get; }


        /// <summary>
        /// �رձ�־
        /// </summary>
        private bool isClose
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
                this.isClose = false;
            }
            catch (Exception ex)
            {
                throw new InitException(this.ToString(), ex.ToString());
            }
        }




        /// <summary>
        /// ����
        /// </summary>
        public override void DoStart()
        {
            try
            {
                //ThreadStart thread = new ThreadStart(this.UpdateIsConnected);
                //this.UpdateThread = new Thread(thread);
                //this.UpdateThread.IsBackground = true;
                //this.UpdateThread.Start();
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
                this.isClose = true;
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
                if (this.UpdateThread == null) return;
                this.UpdateThread.DisableComObjectEagerCleanup();
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
                return "DeviceSystem";
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

        #region ���Ӹ����߳�




        #endregion


    }
}
