using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace KCPNET
{
    public class KCPNet<T>
    where T : Session, new()
        // where K:Msg, new()
    {
        UdpClient udp;
        string serverIp;
        IPEndPoint remotePoint;


        public T session;
        public Task<bool> ConnectServer(int interval, int maxintervalSum = 5000)
        {
            udp.SendAsync(new byte[4], 4, remotePoint);
            int checkTimes = 0;
            Task<bool> task = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(interval);
                    checkTimes += interval;
                    if (session != null && session.IsConnected)
                    {
                        return true;
                    }
                    else
                    {
                        if (checkTimes > maxintervalSum)
                        {
                            return false;
                        }
                    }
                }
            });
            return task;
        }
        public void StartClient(string _serverIp, int port)
        {
            udp = new UdpClient(0);
            serverIp = _serverIp;
            remotePoint = new IPEndPoint(IPAddress.Parse(serverIp), port);
            Task.Run(ClientReceive);
        }
        async public void ClientReceive()
        {


            UdpReceiveResult result;
            while (true)
            {
                try
                {
                    result = await udp.ReceiveAsync();

                    if (Equals(remotePoint, result.RemoteEndPoint))
                    {
                        uint sid = BitConverter.ToUInt32(result.Buffer, 0);
                        if (sid == 0)
                        {
                            //前四位为0，表示还在建立连接
                            if (session != null && session.IsConnected)
                            {
                                //收到多余的服务器udp消息
                            }
                            else
                            {
                                //c#默认是小端存放数据，go默认大端。因此需要将数据反转
                                byte[] bytes = new byte[4];
                                Array.Copy(result.Buffer, 4, bytes, 0, 4);
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(bytes);
                                sid = BitConverter.ToUInt32(bytes, 0);
                                Console.WriteLine(sid);

                                // int kcpPort =(int) BitConverter.ToUInt32(result.Buffer, 8);
                                //此时已收到服务器对于kcp的ack，将remote从UDP端口改成KCP端口
                                remotePoint = new IPEndPoint(IPAddress.Parse(serverIp), 6666);
                                session = new T();

                                session.InitSession(sid, udp, remotePoint);
                            }
                        }
                        else
                        {
                            //前四位不为0，表示连接已经建立完成
                            session.m_kcp.Input(result.Buffer);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }

        }
    }
}
