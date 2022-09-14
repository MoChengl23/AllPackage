
 
using System;
using System.Buffers;
using System.Net.Sockets.Kcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Google.Protobuf;
using System.Net.Sockets;
using Pb;
using UnityEngine;





namespace KCPNET
{
    public abstract class Session
    // where T: ProtobufTool , new()
    {

        private enum State
        {
            DisConnected,
            Connected,
        }
        private State state = State.DisConnected;
        public bool IsConnected { get { return state == State.Connected; } }
        protected uint m_sid;

        public Kcp m_kcp;
        public UdpClient udp;
        Handle m_handle;
        public void InitSession(uint sid, UdpClient _udp, IPEndPoint remotePoint)

        {
            udp = _udp;
            state = State.Connected;
            m_sid = sid;
            m_handle = new Handle();
            m_kcp = new Kcp(sid, m_handle);
            m_kcp.NoDelay(1, 10, 2, 1);
            m_kcp.WndSize(64, 64);
            m_kcp.SetMtu(512);

            m_handle.Out += buffer =>
            {
                byte[] bytes = buffer.ToArray();
                udp.SendAsync(bytes, buffer.Length, remotePoint);
            };
            m_handle.Recv = (byte[] buffer) =>
            {
                if (buffer != null)
                {
                    PbMessage pbMessage = ProtobufTool.ByteToPb(buffer);
                    OnReceiveMessage(pbMessage);
                }

            };
            Task.Run(Update);
        }



        public void ReceiveData(byte[] bytes)
        {
            m_kcp.Input(bytes.AsSpan());

        }
        /// <summary>
        /// 规定，如果发送长度为1的byte，表示玩家进入对局，长度为2表示玩家退出对局
        /// </summary>
        /// <param name="msg"></param>
        public void SendMessage(PbMessage msg)
        {

            if (IsConnected)
            {
Debug.Log("Thread.CurrentThread.Name");
Debug.Log("test");
                Debug.Log(Thread.CurrentThread.Name);
                m_kcp.Send(ProtobufTool.PbToByte(msg));
            }


        }



        async public void Update()
        {
            try
            {
                while (true)
                {
                    DateTime now = DateTime.Now;
                    OnUpdate(now);
                    m_kcp.Update(DateTime.UtcNow);
                    int len;
                    do
                    {
                        var (buffer, avalidSzie) = m_kcp.TryRecv();
                        len = avalidSzie;
                        if (buffer != null)
                        {
                            var temp = new byte[len];
                            buffer.Memory.Span.Slice(0, len).CopyTo(temp);
                            m_handle.Receive(temp);
                        }
                    } while (len > 0);

                    await Task.Delay(10);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public uint GetSid()
        {
            return m_sid;
        }

        protected abstract void OnReceiveMessage(PbMessage pbMessage);
        protected abstract void OnConnected();
        protected abstract void OnUpdate(DateTime now);

        protected abstract void OnDisConnected();



    }
}