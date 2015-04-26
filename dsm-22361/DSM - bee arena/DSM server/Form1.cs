using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Server
{
    public partial class Form1 : Form
    {
        TCPServer ser = null;
        public Form1()
        {
            InitializeComponent();
            ser = new TCPServer(9000);
            Thread tt = new Thread(ser.StartServer);
            tt.Start();
            ser.Disconnection += Dis;
            ser.NewClient += NewC;
        }

        private delegate void ListAddDelegate(Object value);
        private void ListAdd(Object value)
        {
            if (this.ClientsList.InvokeRequired)
            {
                // This is a worker thread so delegate the task.
                this.ClientsList.Invoke(new ListAddDelegate(this.ListAdd), value);
            }
            else
            {
                // This is the UI thread so perform the task.
                this.ClientsList.Items.Add(value);
            }
        }

        private delegate void ListRemoveDelegate(Object value);
        private void ListRemove(Object value)
        {
            try
            {
                if (this.ClientsList.InvokeRequired)
                {
                    // This is a worker thread so delegate the task.
                    this.ClientsList.Invoke(new ListAddDelegate(this.ListRemove), value);
                }
                else
                {
                    // This is the UI thread so perform the task.
                    this.ClientsList.Items.Remove(value);
                }
            }
            catch { }
        }

        private void NewC(object sender, HandleClinet client)
        {
            ListAdd(client.ToString());
        }

        private void Dis(object sender, HandleClinet client)
        {
            ListRemove(client.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ser.StopServer();
        }

        private void buttonKick_Click(object sender, EventArgs e)
        {
            if (ClientsList.SelectedIndex >= 0)
            {
                ser.RemoveClient(ClientsList.SelectedIndex);
            }
        }
    }
}