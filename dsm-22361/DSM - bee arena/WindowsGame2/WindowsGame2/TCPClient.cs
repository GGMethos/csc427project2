using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
namespace Series3D2
{
    //class for MP client (first thing player sees on game launch)
    public class TCPClient
    {
        // delegate declaration
        public delegate void NewDataRecived(object sender, byte[] data);

        // event declaration
        public event NewDataRecived NewData;

        bool stopClient = false;
        String ip = "127.0.0.1";
        int port = 0;
        TcpClient clientSocket = null;
        public TCPClient(String ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }
        public void StartConnection()
        {
            clientSocket = new System.Net.Sockets.TcpClient();
            clientSocket.Connect(ip, port);
            Thread t = new Thread(ReceiveData);
            t.Start();
        }
        public void StopConnection()
        {
            stopClient = true;
            clientSocket.Close();
        }
        public void ReceiveData()
        {
            try
            {
                while (!stopClient)
                {
                    while (clientSocket.Available > 0)
                    {
                        NetworkStream serverStream = clientSocket.GetStream();
                        byte[] inStream = new byte[85];
                        serverStream.Read(inStream, 0, 85);
                        NewData(this, inStream);
                    }
                    Thread.Sleep(1);
                }
            }
            catch { }
        }

        public void SendData(byte[] data)
        {
            NetworkStream serverStream = clientSocket.GetStream();
            serverStream.Write(data, 0, data.Length);
            serverStream.Flush();
        }
    }
}
