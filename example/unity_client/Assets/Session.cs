﻿using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Gamnet
{
	public class Packet : Buffer {
		public const int OFFSET_LENGTH = 0;
		public const int OFFSET_MSGSEQ = 2;
		public const int OFFSET_MSGID = 6;
		public const int HEADER_SIZE = 10;

		private ushort 	_length;
		private uint 	_msg_seq;
		private int 	_msg_id;
		public bool 	reliable;

		public ushort length {
			set {
				byte[] buf_length = BitConverter.GetBytes(value);
				System.Buffer.BlockCopy(buf_length, 0, data, OFFSET_LENGTH, 2);
                _length = value;
			}
			get {
				return _length;
			}
		}
		public uint msg_seq {
			set {
				byte[] buf_msg_seq = BitConverter.GetBytes(value);
				System.Buffer.BlockCopy(buf_msg_seq, 0, data, OFFSET_MSGSEQ, 4);
                _msg_seq = value;
			}
			get {
				return _msg_seq;
			}
		}
		public int msg_id {
			set {
				byte[] buf_msg_id = BitConverter.GetBytes(value);
				System.Buffer.BlockCopy(buf_msg_id, 0, data, OFFSET_MSGID, 4);
                _msg_id = value;
			}
			get {
				return _msg_id;
			}
		}

		public Packet() {
			_length = 0;
			_msg_seq = 0;
			_msg_id = 0;
			reliable = false;
			write_index = HEADER_SIZE;
		}
		public Packet(Packet src) : base(src) {
			_length = src._length;
			_msg_seq = src._msg_seq;
			_msg_id = src._msg_id;
			reliable = src.reliable;
		}
	}
	public class TimeoutMonitor
	{
		private Dictionary<uint, List<System.Timers.Timer>> timers = new Dictionary<uint, List<System.Timers.Timer>>();
		public delegate void OnTimeout();

		public TimeoutMonitor()	{}

		public void SetTimeout(uint msg_id, int timeout, OnTimeout timeout_callback)	{
			System.Timers.Timer timer = new System.Timers.Timer();
			timer.Interval = timeout * 1000;
			timer.AutoReset = false;
			timer.Elapsed += delegate {
				timeout_callback();
			};
			timer.Start();

			if (false == timers.ContainsKey(msg_id)) {
				timers.Add(msg_id, new List<System.Timers.Timer>());
			}
			timers[msg_id].Add(timer);
		}
		public void UnsetTimeout(uint msg_id) {
			if (false == timers.ContainsKey(msg_id)) {
				return;
			}
			if (0 == timers[msg_id].Count) {
				return;
			}
			timers[msg_id].RemoveAt(0);
		}
	}
	
    public class Session
    {
		public enum ConnectionState
		{
			Disconnected,
			OnConnecting,
			Connected
		}
		// length<2byte>(header length + body length) | msg_id<4byte> | body

		public int 			heartbeat_time = 60000;
		private int 		_connect_timeout = 60000;
		private object 		_sync_obj = new object();
        private Socket 		_socket = null;
        private IPEndPoint 	_endpoint = null;

		private ConnectionState 	_state = ConnectionState.Disconnected;
        public ConnectionState      state { get { return _state; } }
		private Gamnet.Buffer		_recv_buff = new Gamnet.Buffer();
		private TimeoutMonitor 		_timeout_monitor = null;
		private System.Timers.Timer	_timer = null;

		private List<SessionEvent> 	_event_queue = new List<SessionEvent>();
		private List<Gamnet.Packet>	_send_queue = new List<Gamnet.Packet>(); // 바로 보내지 못하고 
		private int 				_send_queue_idx = 0;

		private Dictionary<uint, Delegate_OnReceive> _handlers = new Dictionary<uint, Delegate_OnReceive>();

		private NetworkReachability	_networkReachability = NetworkReachability.NotReachable;
	
		private uint 	_session_key = 0;
		private string 	_session_token = "";

		private uint _msg_seq = 0;
		public uint msg_seq {
			get {
				return _msg_seq;
			}
		}

		public delegate void Delegate_OnConnect();
		public delegate void Delegate_OnReconnect();
		public delegate void Delegate_OnClose();
		public delegate void Delegate_OnError(Gamnet.Exception e);
        public delegate void Delegate_OnReceive(System.IO.MemoryStream ms);

		public Delegate_OnConnect onConnect;
		public Delegate_OnReconnect onReconnect;
		public Delegate_OnClose onClose;
		public Delegate_OnError onError;

        public abstract class SessionEvent {
            protected Session session;
			public SessionEvent(Session session) {
                this.session = session;
            }
            public abstract void Event();
        };
		public class ConnectEvent : SessionEvent {
            public ConnectEvent(Session session) : base(session) {}
            public override void Event() {
				if (null != session.onConnect) {
					session.onConnect ();
				}
            }
        }
		public class ReconnectEvent : SessionEvent {
            public ReconnectEvent(Session session) : base(session) { }
            public override void Event() {
				if (null != session.onReconnect) {
					session.onReconnect ();
				}
            }
        }
        public class ErrorEvent : SessionEvent {
            public ErrorEvent(Session session) : base(session) { }
			public Gamnet.Exception exception;
            public override void Event() {
				if (null != session.onError) {
					session.onError (exception);
				}
            }
        }
		public class CloseEvent : SessionEvent {
			public CloseEvent(Session session) : base(session) { }
			public override void Event() {
				if (null != session.onClose) {
					session.onClose ();
				}
			}
		}
        public class ReceiveEvent : SessionEvent {
            public ReceiveEvent(Session session) : base(session) { }
            public Gamnet.Buffer buffer;
            public override void Event() {
                session.OnReceive(buffer);
            }
        }

		public Session() {
			RegisterHandler (MsgID_Connect_Ans, Recv_Connect_Ans);
			RegisterHandler (MsgID_Reconnect_Ans, Recv_Reconnect_Ans);
			RegisterHandler (MsgID_HeartBeat_Ans, Recv_HeartBeat_Ans);
			RegisterHandler (MsgID_Kickout_Ntf, Recv_Kickout_Ntf);
		}
		public void Connect(string host, int port, int timeout_sec = 60) {
            try {
				lock(_sync_obj) {
					_send_queue_idx = 0;
					_send_queue.Clear();
				}

                _networkReachability = Application.internetReachability;
                _timeout_monitor = new TimeoutMonitor();
				_socket = null;
				_endpoint = null;
				_timer = null;
				_msg_seq = 0;
                _state = ConnectionState.OnConnecting;
				_connect_timeout = timeout_sec * 1000;

                IPAddress ip = null;
                try {
                    ip = IPAddress.Parse(host);
                }
                catch (System.FormatException) {
                    IPHostEntry hostEntry = Dns.GetHostEntry(host);
                    if (hostEntry.AddressList.Length > 0) {
                        ip = hostEntry.AddressList[0];
                    }
                }
                _endpoint = new IPEndPoint(ip, port);

                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(_endpoint, new AsyncCallback(Callback_Connect), socket);
				SetConnectTimeout();
            }
            catch (SocketException e) {
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
            }
        }
		private void Callback_Connect(IAsyncResult result) {
            try	{
				Debug.Log ("connect()");
				_socket = (Socket)result.AsyncState;
				_socket.EndConnect(result);
				_socket.ReceiveBufferSize = Gamnet.Buffer.BUFFER_SIZE;
				_socket.SendBufferSize = Gamnet.Buffer.BUFFER_SIZE;
                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 10000);
                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
				_timer.Stop ();

				Receive();

				_state = ConnectionState.Connected;

				Send_Connect_Req();

                _timer = new System.Timers.Timer();
				_timer.Interval = heartbeat_time;
                _timer.AutoReset = true;
                _timer.Elapsed += delegate { 
					Send_HeartBeat_Req(); 
				};
                _timer.Start();
            }
			catch (System.Exception e) {
				Error(new Gamnet.Exception(ErrorCode.ConnectFailError, e.Message));
				Close();
			}
		}

        private void Reconnect() {
            if (ConnectionState.Disconnected != _state) {
                return;
            }
            _state = ConnectionState.OnConnecting;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.BeginConnect(_endpoint, new AsyncCallback(Callback_Reconnect), socket);
			SetConnectTimeout();
        }
		private void Callback_Reconnect(IAsyncResult result) {
			try	{
				Debug.Log ("reconnect");
				_socket = (Socket)result.AsyncState;
				_socket.EndConnect(result);
				_socket.ReceiveBufferSize = Gamnet.Buffer.BUFFER_SIZE;
				_socket.SendBufferSize = Gamnet.Buffer.BUFFER_SIZE;
                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 10000);
                //_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
				_timer.Stop ();

				Receive();

				_state = ConnectionState.Connected;

				lock (_sync_obj) {
					List<Gamnet.Packet> tmp = new List<Gamnet.Packet>(_send_queue);
					_send_queue.Clear ();
					_send_queue_idx = 0;
					Send_Reconnect_Req();
					foreach (var itr in tmp) {
						itr.read_index = 0;
						_send_queue.Add (itr);
					}
				}
                _timer = new System.Timers.Timer();
				_timer.Interval = heartbeat_time;
                _timer.AutoReset = true;
                _timer.Elapsed += delegate { 
					Send_HeartBeat_Req(); 
				};
                _timer.Start();
            }
			catch (SocketException e) {
				Error(new Gamnet.Exception(ErrorCode.ConnectFailError, e.Message));
				Close();
			}
		}

		private void Receive() {
            try {
				Gamnet.Buffer buffer = new Gamnet.Buffer();
                _socket.BeginReceive(buffer.data, 0, Gamnet.Buffer.BUFFER_SIZE, 0, new AsyncCallback(Callback_Receive), buffer);
            }
            catch (SocketException e) {
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
                Close();
            }
        }
		private void Callback_Receive(IAsyncResult result) {
            try {
                Gamnet.Buffer buffer = (Gamnet.Buffer)result.AsyncState;
                int recvBytes = _socket.EndReceive(result);
                if (0 == recvBytes) {
                    Close();
                    return;
                }
                buffer.write_index += recvBytes;

                ReceiveEvent evt = new ReceiveEvent(this);
                evt.buffer = buffer;
				lock(_sync_obj) {
                	_event_queue.Add(evt);
				}
                Receive();
            }
            catch (SocketException e) {
				if (ConnectionState.Disconnected == _state) {
					return;
				}
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
                Close();
            }
		}
		public void OnReceive(Gamnet.Buffer buf) {
			_recv_buff += buf;
			while (_recv_buff.Size() >= Packet.HEADER_SIZE)
			{
				ushort packetLength = BitConverter.ToUInt16(_recv_buff.data, _recv_buff.read_index + Packet.OFFSET_LENGTH);
				if (packetLength > Gamnet.Buffer.BUFFER_SIZE) {
					Error(new Gamnet.Exception(ErrorCode.BufferOverflowError, "The packet length is greater than the buffer max length."));
                    return;
				}

				if (packetLength > _recv_buff.Size()) { // not enough
					return;
				}

				uint msgID = BitConverter.ToUInt32(_recv_buff.data, _recv_buff.read_index + Packet.OFFSET_MSGID);
				if (false == _handlers.ContainsKey(msgID)) {
					Error(new Gamnet.Exception (ErrorCode.UnhandledMsgError , "can't find registered msg(id:" + msgID + ")"));
                    return;
				}

				_recv_buff.read_index += Packet.HEADER_SIZE;
				_timeout_monitor.UnsetTimeout(msgID);

				Delegate_OnReceive handler = _handlers[msgID];

				try	{
					handler(_recv_buff);
				}
				catch (System.Exception e) {
					Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
				}

				_recv_buff.read_index += packetLength - Packet.HEADER_SIZE;
			}
		}

		public void Error(Gamnet.Exception e) {
            ErrorEvent evt = new ErrorEvent(this);
            evt.exception = e;
			lock (_sync_obj) {
				_event_queue.Add (evt);
			}
        }

        public void Close() {
            if (ConnectionState.Disconnected == _state) {
                return;
            }
			Debug.Log ("close");
            try {
				_state = ConnectionState.Disconnected;
                _timer.Stop();
				//_socket.Shutdown(SocketShutdown.Send);
                _socket.BeginDisconnect(false, new AsyncCallback(Callback_Close), _socket);
            }
            catch (SocketException e)
            {
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
            }
        }
		private void Callback_Close(IAsyncResult result) {
			try	{
				_socket.EndDisconnect(result);
				_socket.Close();
				CloseEvent evt = new CloseEvent(this);
				lock(_sync_obj)	{
					_event_queue.Add(evt);
				}
			}
			catch (SocketException e) {
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
			}
		}
        
		public TimeoutMonitor SendMsg(object msg, bool handOverRelility = false) {
			try	{
				Reconnect();

				System.IO.MemoryStream ms = new System.IO.MemoryStream();
				System.Type type = msg.GetType();
				type.GetMethod("Store").Invoke(msg, new object[] { ms });
				System.Reflection.FieldInfo fi = type.GetField("MSG_ID");

				int msgID = (int)fi.GetValue(msg);
				int dataLength = (int)(type.GetMethod("Size").Invoke(msg, null));
				ushort packetLength = (ushort)(Packet.HEADER_SIZE + dataLength);

				if (packetLength > Gamnet.Buffer.BUFFER_SIZE) {
					throw new System.Exception(string.Format("Overflow the send buffer max size : {0}", packetLength));
				}

                Gamnet.Packet packet = new Gamnet.Packet();
				packet.length = packetLength;
				packet.msg_seq = ++_msg_seq;
				packet.msg_id = msgID;
				packet.reliable = handOverRelility;
				packet.Append(ms);

				SendMsg(packet);
				if (0 == msg_seq % 10 && ConnectionState.Connected == _state)
				{
					Send_HeartBeat_Req ();
				}
			}
			catch (System.Exception e) {
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
			}
			return _timeout_monitor;
		}
		private void SendMsg(Gamnet.Packet packet) {
			lock (_sync_obj) {
				_send_queue.Add(packet);
				if (1 == _send_queue.Count - _send_queue_idx && ConnectionState.Connected == _state) {
					_socket.BeginSend(_send_queue[_send_queue_idx].data, 0, _send_queue[_send_queue_idx].Size(), 0, new AsyncCallback(Callback_SendMsg), packet);
				}
            }
		}
        private void Callback_SendMsg(IAsyncResult result) {
            try	{
                lock (_sync_obj) {
					int writedBytes = _socket.EndSend(result);
                    Gamnet.Packet packet = _send_queue[_send_queue_idx];
					Debug.Log ("send(msg_seq:" + packet.msg_seq + ", msg_id:" + packet.msg_id + ", length:" + packet.length + ")");
                    packet.read_index += writedBytes;
                    if (packet.Size() > 0) {
                        Gamnet.Packet newPacket = new Gamnet.Packet(packet);
						_socket.BeginSend(newPacket.data, 0, newPacket.Size(), 0, new AsyncCallback(Callback_SendMsg), newPacket);
                        return;
                    }

					if(true == packet.reliable) {
						_send_queue_idx++;
					}
					else {
						_send_queue.RemoveAt(_send_queue_idx);	
					}

                    if (_send_queue_idx < _send_queue.Count) {
						_socket.BeginSend(_send_queue[_send_queue_idx].data, 0, _send_queue[_send_queue_idx].Size(), 0, new AsyncCallback(Callback_SendMsg), _send_queue[_send_queue_idx]);
						return;
                    }
                }
			}
            catch (SocketException e)
			{
				Error(new Gamnet.Exception(ErrorCode.UndefinedError, e.Message));
			}
		}

		void RemoveSentPacket(uint msg_seq) {
			lock (_sync_obj) {
				Debug.Log ("remove msg(msg_seq:" + msg_seq + ")");
				while (0 < _send_queue.Count) {
					if (_send_queue [0].msg_seq > msg_seq) {
						break;
					}
					_send_queue.RemoveAt (0);
					_send_queue_idx--;
				}
			}
		}
		void SetConnectTimeout() {
			_timer = new System.Timers.Timer();
			_timer.Interval = _connect_timeout;
			_timer.AutoReset = false;
			_timer.Elapsed += delegate {
				Error(new Gamnet.Exception(ErrorCode.ConnectTimeoutError, "fail to connect(" + _endpoint.ToString() + ")"));
				Close();
			};
			_timer.Start();
		}
        public void Update() {
			lock (_sync_obj) {
				while (0 < _event_queue.Count) {
					SessionEvent evt = _event_queue[0];
					_event_queue.RemoveAt (0);
					evt.Event ();
				}
			}

			if (Application.internetReachability != _networkReachability) {
				_networkReachability = Application.internetReachability;
				Close ();
			}
        }
		public void RegisterHandler(uint msg_id, Delegate_OnReceive handler) {
            if (_handlers.ContainsKey(msg_id))
            {
                _handlers[msg_id] += handler;
            }
            else
            {
                _handlers.Add(msg_id, handler);
            }
        }
        public void UnregisterHandler(uint msg_id, Delegate_OnReceive handler)
        {
            if (_handlers.ContainsKey(msg_id))
            {
                _handlers[msg_id] -= handler;
                if (null == _handlers[msg_id])
                {
                    _handlers.Remove(msg_id);
                }
            }
        }

        public void UnregisterHandler(uint msg_id)
        {
            if (_handlers.ContainsKey(msg_id))
            {
                _handlers.Remove(msg_id);
            }
        }
        
		const int MsgID_Connect_Req		= 0001;
		void Send_Connect_Req()
		{
			Gamnet.Packet packet = new Gamnet.Packet();
			packet.length = Packet.HEADER_SIZE;
			packet.msg_seq = ++_msg_seq;
			packet.msg_id = MsgID_Connect_Req;
			packet.reliable = false;

			SendMsg(packet);
		}
		const int MsgID_Connect_Ans		= 0001;
		[System.Serializable] 
		class Msg_Connect_Ans { 
			public int error_code = 0;
			public uint session_key = 0; 
			public string session_token = "";
		}
		void Recv_Connect_Ans(System.IO.MemoryStream buffer) {
			string json = System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
			Msg_Connect_Ans ans = JsonUtility.FromJson<Msg_Connect_Ans>(json);
			if(0 != ans.error_code)
			{
				Error(new Gamnet.Exception(ans.error_code, "fail to connect"));
				return;
			}
			_session_key = ans.session_key;
			_session_token = ans.session_token;

			ConnectEvent evt = new ConnectEvent(this);
			_event_queue.Add(evt); // already locked
		}

		const int MsgID_Reconnect_Req 	= 0002;
		[System.Serializable] 
		class Msg_Reconnect_Req { 
			public uint session_key = 0; 
			public string session_token = "";
		}
		void Send_Reconnect_Req()
		{
			Msg_Reconnect_Req req = new Msg_Reconnect_Req();
			req.session_key = _session_key;
			req.session_token = _session_token;
            string json = JsonUtility.ToJson(req);
            Debug.Log("send reconnect message(" + json + ")");
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

			Gamnet.Packet packet = new Gamnet.Packet();
			packet.length = (ushort)(Packet.HEADER_SIZE + data.Length);
			packet.msg_seq = ++_msg_seq;
			packet.msg_id = MsgID_Reconnect_Req;
			packet.reliable = false;
			packet.Append(data);

			SendMsg (packet);
		}
		const int MsgID_Reconnect_Ans                = 0002;
		[System.Serializable] 
		class Msg_Reconnect_Ans {
			public int error_code = 0;
			public uint session_key = 0;
			public string session_token = "";
			public uint msg_seq = 0;
		}
		void Recv_Reconnect_Ans(System.IO.MemoryStream buffer) {
			string json = System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
			Msg_Reconnect_Ans ans = JsonUtility.FromJson<Msg_Reconnect_Ans>(json);
			if(0 != ans.error_code)
			{
				Error(new Gamnet.Exception(ans.error_code, "fail to reconnect"));
				return;
			}
			_session_key = ans.session_key;
			_session_token = ans.session_token;
			//RemoveSentPacket(ans.msg_seq);
			ReconnectEvent evt = new ReconnectEvent(this);
			_event_queue.Add(evt); // already locked
		}

		const int MsgID_HeartBeat_Req                = 0003;
		void Send_HeartBeat_Req()
		{
			Gamnet.Packet packet = new Gamnet.Packet ();
			packet.length = Packet.HEADER_SIZE;
			packet.msg_seq = ++_msg_seq;
			packet.msg_id = MsgID_HeartBeat_Req;
			packet.reliable = true;
			Debug.Log ("send heart beat(msg_seq:" + packet.msg_seq + ")");
			SendMsg (packet);
		}
		const int MsgID_HeartBeat_Ans                = 0003;
		[System.Serializable]
		class Msg_HeartBeat_Ans {
			public int error_code = 0;
			public uint msg_seq = 0;
		}
		void Recv_HeartBeat_Ans(System.IO.MemoryStream buffer) {
			string json = System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
			Msg_HeartBeat_Ans ans = JsonUtility.FromJson<Msg_HeartBeat_Ans>(json);
			if(0 != ans.error_code)
			{
				Error(new Gamnet.Exception(ans.error_code, "fail to reconnect"));
				return;
			}
			RemoveSentPacket(ans.msg_seq);
		}

		const int MsgID_Kickout_Ntf 	                = 0004;
		[System.Serializable]
		class Msg_Kickout_Ntf {
			public int error_code = 0;
		}
		void Recv_Kickout_Ntf(System.IO.MemoryStream buffer) {
			string json = System.Text.Encoding.UTF8.GetString(buffer.GetBuffer(), (int)buffer.Position, (int)(buffer.Length - buffer.Position));
			Msg_Kickout_Ntf ntf = JsonUtility.FromJson<Msg_Kickout_Ntf>(json);
			Error(new Gamnet.Exception(ntf.error_code));
			Close();
		}
    }
}