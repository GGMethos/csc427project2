using System;
using Series3D2;
using System.Windows.Forms;

namespace WindowsGame2
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Form1 f = new Form1();
            f.ShowDialog();
            int port = f.port;
            String ip = f.ip;
            float fow = f.fieldOfView;
            Microsoft.Xna.Framework.Vector4 color = 
                new Microsoft.Xna.Framework.Vector4((float)((double)f.color.R / 255.0),
                    (float)((double)f.color.G / 255.0),
                    (float)((double)f.color.B / 255.0),
                    1.0f);
            if (ip == "none")
                return;
            try
            {
                using (Game1 game = new Game1(ip, port, color, f.uid, fow, f.shipModel, f.randomPosition))
                {
                    game.Run();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Source +
                    "\r\n-------------------------------------\r\n" +
                    e.Message +
                    "\r\n-------------------------------------\r\n" +
                    e.StackTrace, "Deadly Sky Masters");
            }
        }
    }
#endif
}

