using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace WindowsGame2
{
    public partial class Form1 : Form
    {
        public int port = 9000;
        public String ip = "none";
        public System.Drawing.Color color;
        public int uid = 0;
        public float fieldOfView = (float)(Math.PI / 3.0);
        public byte shipModel = 0;
        public bool randomPosition = false;
        
        public Form1()
        {
            Random r = new Random();
            color = System.Drawing.Color.FromArgb(r.Next(255), r.Next(255), r.Next(255));
            InitializeComponent();
            uid = r.Next();
            comboBoxFieldOfVIew.SelectedIndex = 1;
            comboBoxShipModel.SelectedIndex = 0;
            buttonColor.BackColor = color;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                port = int.Parse(textBoxPort.Text);
                ip = textBoxIP.Text;
                if (comboBoxFieldOfVIew.SelectedIndex == 0)
                    fieldOfView = (float)(Math.PI / 4.0);
                if (comboBoxFieldOfVIew.SelectedIndex == 1)
                    fieldOfView = (float)(Math.PI / 3.0);
                if (comboBoxFieldOfVIew.SelectedIndex == 2)
                    fieldOfView = (float)(Math.PI / 2.25);
                if (comboBoxFieldOfVIew.SelectedIndex == 3)
                    fieldOfView = (float)(Math.PI / 2.0);
                shipModel = (byte)comboBoxShipModel.SelectedIndex;
                randomPosition = checkBoxRandom.Checked;
                this.Close();
            }
            catch
            {
                MessageBox.Show("Unable to parse port number");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = color;
            if (colorDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                color = colorDialog1.Color;
                buttonColor.BackColor = color;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
