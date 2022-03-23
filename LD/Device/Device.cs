using System;

namespace LD.Device
{
    
	/// <summary>
	/// �豸�ӿ�
	/// </summary>
	public interface IDevice
	{
		/// <summary>
		/// �豸��ʼ��
		/// </summary>
		void Init();

		/// <summary>
		/// �豸����
		/// </summary>
		void Start();

		/// <summary>
		/// �豸ֹͣ
		/// </summary>
		void Stop();

		/// <summary>
		/// �豸�ͷ�
		/// </summary>
		void Release();


		/// <summary>
		/// �豸ID
		/// </summary>
		int DeviceID{get;}


        /// <summary>
        /// �豸����
        /// </summary>
        string  DeviceType { get; }

        /// <summary>
        /// �豸��ע
        /// </summary>
        /// <returns></returns>
        string ToString();

	}


	/// <summary>
	/// �豸����
	/// </summary>
	public abstract class Device : IDevice
	{
		public Device(){}

        /// <summary>
        /// ��ʼ��
        /// </summary>
        public void Init()
        {
           
            Log.Runlog.Instance.Add(this.ToString(), "��ʼ��");
            DoInit();
        }
        /// <summary>
        /// ����
        /// </summary>
        public void Start()
        {
            Log.Runlog.Instance.Add(this.ToString(), "����");
            DoStart();
        }
        /// <summary>
        /// ֹͣ
        /// </summary>
        public void Stop()
        {
            Log.Runlog.Instance.Add(this.ToString(), "�ر�");
            DoStop();
        }
        /// <summary>
        /// �ͷ�
        /// </summary>
        public void Release()
        {
            Log.Runlog.Instance.Add(this.ToString(), "�ͷ�");
            DoRelease();
        }

		/// <summary>
		/// �豸ID
		/// </summary>
		public virtual int DeviceID
		{
			get { return 0; }
		}


		/// <summary>
		/// ��ʼ�����Ӻ���
		/// </summary>
		public abstract void DoInit();
		/// <summary>
		/// �������Ӻ���
		/// </summary>
		public abstract void DoStart();
		/// <summary>
		/// ֹͣ���Ӻ���
		/// </summary>
		public abstract void DoStop();
		/// <summary>
		/// �ͷŹ��Ӻ���
		/// </summary>
		public abstract void DoRelease();
		/// <summary>
		/// �豸����
		/// </summary>
		public abstract string  DeviceType{ get; }

  
		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public abstract override string ToString();
		

	}

	/// <summary>
	/// ��ʼ���쳣��
	/// </summary>
	public class InitException : Exception
	{
		/// <summary>
		/// �豸
		/// </summary>
		private string device;
		/// <summary>
		/// ԭ��
		/// </summary>
		private string reason;
		/// <summary>
		/// ���캯��
		/// </summary>
		/// <param name="device"></param>
		/// <param name="reason"></param>
		public InitException( string device, string reason ) : base( reason )
		{
			this.device = device;
			this.reason = reason;
		}
		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format( "�豸[{0}]��ʼ������:{1}", device , reason );
		}

	}


	/// <summary>
	/// �����쳣��
	/// </summary>
	public class StartException : Exception
	{
		/// <summary>
		/// �豸
		/// </summary>
		private string device;
		/// <summary>
		/// ԭ��
		/// </summary>
		private string reason;
		/// <summary>
		/// ���캯��
		/// </summary>
		/// <param name="device"></param>
		/// <param name="reason"></param>
		public StartException( string device, string reason ) : base( reason )
		{
			this.device = device;
			this.reason = reason;
		}
		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format( "�豸[{0}]��������:{1}", device , reason );
		}

	}


	/// <summary>
	/// ֹͣ�쳣��
	/// </summary>
	public class StopException : Exception
	{	
		/// <summary>
		/// �豸
		/// </summary>
		private string device;
		/// <summary>
		/// ԭ��
		/// </summary>
		private string reason;
		/// <summary>
		/// ���캯��
		/// </summary>
		/// <param name="device"></param>
		/// <param name="reason"></param>
		public StopException( string device, string reason ) : base( reason )
		{
			this.device = device;
			this.reason = reason;
		}
		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format( "�豸[{0}]ֹͣ����:{1}", device , reason );
		}

	}


	/// <summary>
	/// �ͷ��쳣��
	/// </summary>
	public class ReleaseException : Exception
	{
		/// <summary>
		/// �豸
		/// </summary>
		private string device;
		/// <summary>
		/// ԭ��
		/// </summary>
		private string reason;
		/// <summary>
		/// ���캯��
		/// </summary>
		/// <param name="device"></param>
		/// <param name="reason"></param>
		public ReleaseException( string device, string reason ) : base( reason )
		{
			this.device = device;
			this.reason = reason;
		}
		/// <summary>
		/// ToString
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return String.Format( "�豸[{0}]�ͷŴ���:{1}", device , reason );
		}

	}
}
