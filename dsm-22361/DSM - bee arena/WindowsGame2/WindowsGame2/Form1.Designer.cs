namespace WindowsGame2
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxIP = new System.Windows.Forms.TextBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.buttonColor = new System.Windows.Forms.Button();
            this.comboBoxFieldOfVIew = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.comboBoxShipModel = new System.Windows.Forms.ComboBox();
            this.checkBoxRandom = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // textBoxIP
            // 
            this.textBoxIP.Location = new System.Drawing.Point(69, 6);
            this.textBoxIP.Name = "textBoxIP";
            this.textBoxIP.Size = new System.Drawing.Size(100, 20);
            this.textBoxIP.TabIndex = 0;
            this.textBoxIP.Text = "127.0.0.1";
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(69, 40);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(100, 20);
            this.textBoxPort.TabIndex = 1;
            this.textBoxPort.Text = "9000";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Server port";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Server IP";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(15, 79);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Connect";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // buttonColor
            // 
            this.buttonColor.Location = new System.Drawing.Point(190, 3);
            this.buttonColor.Name = "buttonColor";
            this.buttonColor.Size = new System.Drawing.Size(102, 50);
            this.buttonColor.TabIndex = 5;
            this.buttonColor.Text = "Change starship color";
            this.buttonColor.UseVisualStyleBackColor = true;
            this.buttonColor.Click += new System.EventHandler(this.button2_Click);
            // 
            // comboBoxFieldOfVIew
            // 
            this.comboBoxFieldOfVIew.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFieldOfVIew.FormattingEnabled = true;
            this.comboBoxFieldOfVIew.Items.AddRange(new object[] {
            "PI over 4",
            "PI over 3",
            "PI over 2.25",
            "PI over 2"});
            this.comboBoxFieldOfVIew.Location = new System.Drawing.Point(259, 86);
            this.comboBoxFieldOfVIew.Name = "comboBoxFieldOfVIew";
            this.comboBoxFieldOfVIew.Size = new System.Drawing.Size(121, 21);
            this.comboBoxFieldOfVIew.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(187, 89);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Field of view";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(187, 65);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Ship model";
            // 
            // comboBoxShipModel
            // 
            this.comboBoxShipModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxShipModel.FormattingEnabled = true;
            this.comboBoxShipModel.Items.AddRange(new object[] {
            "X-Wing",
            "Tie-Fighter",
            "Tie-Invader",
            "Unknown Spaceship"});
            this.comboBoxShipModel.Location = new System.Drawing.Point(259, 57);
            this.comboBoxShipModel.Name = "comboBoxShipModel";
            this.comboBoxShipModel.Size = new System.Drawing.Size(121, 21);
            this.comboBoxShipModel.TabIndex = 9;
            // 
            // checkBoxRandom
            // 
            this.checkBoxRandom.AutoSize = true;
            this.checkBoxRandom.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBoxRandom.Location = new System.Drawing.Point(167, 117);
            this.checkBoxRandom.Name = "checkBoxRandom";
            this.checkBoxRandom.Size = new System.Drawing.Size(105, 17);
            this.checkBoxRandom.TabIndex = 10;
            this.checkBoxRandom.Text = "Random position";
            this.checkBoxRandom.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(397, 146);
            this.Controls.Add(this.checkBoxRandom);
            this.Controls.Add(this.comboBoxShipModel);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBoxFieldOfVIew);
            this.Controls.Add(this.buttonColor);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.textBoxIP);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "Deadly Sky Masters";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxIP;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.Button buttonColor;
        private System.Windows.Forms.ComboBox comboBoxFieldOfVIew;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox comboBoxShipModel;
        private System.Windows.Forms.CheckBox checkBoxRandom;
    }
}