using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Server
{
    class HandleClinet
    {
        TcpClient clientSocket;
        string clNo;
        TCPServer parentServer = null;
        private bool stopClient = false;
        public int id = 0;

        public HandleClinet(int id)
        {
            this.id = id;
        }

        public void startClient(TcpClient inClientSocket, string clineNo, TCPServer parentServer)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clineNo;
            this.parentServer = parentServer;
            Thread ctThread = new Thread(receiver);
            ctThread.Start();
        }
        public void StopClient()
        {
            clientSocket.Close();
            stopClient = true;
        }
        private void receiver()
        {
            try
            {
                while (!stopClient && CheckConnection(clientSocket))
                {
                    if (clientSocket.Available > 0)
                    {
                        byte[] bytesFrom = new byte[85];
                        NetworkStream networkStream = clientSocket.GetStream();
                        networkStream.Read(bytesFrom, 0, 85);
                        parentServer.BroadcastMessage(bytesFrom, this);
                    }
                    Thread.Sleep(1);
                }
            }
            catch
            {
                parentServer.RemoveClient(this);
            }
            parentServer.RemoveClient(this);
        }

        bool CheckConnection(TcpClient client)
        {
            if (client.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
                {
                    // Client disconnected
                    return false;
                }
            }
            return true;
        }

        public void SendData(byte[] data)
        {
            NetworkStream networkStream = clientSocket.GetStream();
            networkStream.Write(data, 0, data.Length);
            networkStream.Flush();
            
        }

        public String ToString()
        {
            String result = "Error has occured!";
            try
            {
                result = "(" + this.id + ") " + ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Address.ToString() + ":" + ((IPEndPoint)clientSocket.Client.RemoteEndPoint).Port.ToString();
            }
            catch { };
            return result;
        }
    }
}
