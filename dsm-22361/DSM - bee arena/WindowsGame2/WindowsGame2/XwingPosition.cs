using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Net;

namespace Series3D2
{
    [Serializable]
    public class XwingPosition
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector4 color;
        public int uid;
        public Bullet newBullet = null;
        public int killedByUid = -1;
        //if collision != 0 then the player was killed by collision with killedByUid, and player with uid = killedByUid
        //should also be killed
        //public byte collision = 0;
        public int lastUpdate = 0;
        public byte shipModel = 0;
        public int frags = 0;

        private static void ConverAndAddToArray(byte[]data, float number, ref int actualPosition, bool reverse)
        {
            byte []helpArray = BitConverter.GetBytes(number);
            if (reverse)
                Array.Reverse(helpArray);
            for (int a = 0; a < helpArray.Length; a++)
                data[actualPosition + a] = helpArray[a];
            actualPosition += helpArray.Length;
        }

        private static void ConverAndAddToArray(byte[]data, int number, ref int actualPosition, bool reverse)
        {
            byte []helpArray = BitConverter.GetBytes(number);
            if (reverse)
                Array.Reverse(helpArray);
            for (int a = 0; a < helpArray.Length; a++)
                data[actualPosition + a] = helpArray[a];
            actualPosition += helpArray.Length;
        }

        private static void ConverAndAddToArray(byte[] data, byte number, ref int actualPosition, bool reverse)
        {
            data[actualPosition] = number;
            actualPosition ++;
        }

        public static byte[] ToByteArray(XwingPosition Data)
        {
            //position rotation color uid
            byte[]outputData = new byte[(3 * sizeof(float)) + (4 * sizeof(float)) + (4 * sizeof(float)) + sizeof(int) + 
                //position rotation uid
                (3 * sizeof(float)) + (4 * sizeof(float)) + sizeof(int) +
                //uid
                sizeof(int) +
                //ship model
                sizeof(byte)];
            int actualPosition = 0;
            bool reverse = BitConverter.IsLittleEndian;
            uint positionNo1 = 0;
            //position
            ConverAndAddToArray(outputData, Data.position.X, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.position.Y, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.position.Z, ref actualPosition, reverse);
            //rotation
            ConverAndAddToArray(outputData, Data.rotation.W, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.rotation.X, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.rotation.Y, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.rotation.Z, ref actualPosition, reverse);
            //color
            ConverAndAddToArray(outputData, Data.color.W, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.color.X, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.color.Y, ref actualPosition, reverse);
            ConverAndAddToArray(outputData, Data.color.Z, ref actualPosition, reverse);    
            //uid
            ConverAndAddToArray(outputData, Data.uid, ref actualPosition, reverse);
            //has new bullet

            if (Data.newBullet != null)
            {
                //bullet posiotion
                ConverAndAddToArray(outputData, Data.newBullet.position.X, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, Data.newBullet.position.Y, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, Data.newBullet.position.Z, ref actualPosition, reverse);
                //bullet rotation
                ConverAndAddToArray(outputData, Data.newBullet.rotation.W, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, Data.newBullet.rotation.X, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, Data.newBullet.rotation.Y, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, Data.newBullet.rotation.Z, ref actualPosition, reverse);
                //bullet id
                ConverAndAddToArray(outputData, Data.newBullet.ownerUid, ref actualPosition, reverse);
            }
            else
            {
                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);

                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);
                ConverAndAddToArray(outputData, 0, ref actualPosition, reverse);

                ConverAndAddToArray(outputData, -1, ref actualPosition, reverse);
            }
            //killed by
            ConverAndAddToArray(outputData, Data.killedByUid, ref actualPosition, reverse);
            //ship model
            ConverAndAddToArray(outputData, Data.shipModel, ref actualPosition, reverse); 

            return outputData;
        }


        private static float ConvertFloatFromArray(byte[]data,ref int actualPosition, bool reverse)
        {
            byte []helpArray = new byte[sizeof(float)];
            for (int a = 0; a < sizeof(float);a ++)
                helpArray[a] = data[actualPosition + a];
            if (reverse)
                Array.Reverse(helpArray);
            actualPosition += helpArray.Length;
            return BitConverter.ToSingle(helpArray, 0);
        }

        private static int ConvertIntFromArray(byte[]data,ref int actualPosition, bool reverse)
        {
            byte []helpArray = new byte[sizeof(int)];
            for (int a = 0; a < sizeof(int);a ++)
                helpArray[a] = data[actualPosition + a];
            if (reverse)
                Array.Reverse(helpArray);
            actualPosition += helpArray.Length;
            return BitConverter.ToInt32(helpArray, 0);
        }

        private static byte ConvertByteFromArray(byte[] data, ref int actualPosition, bool reverse)
        {
            byte returnValue = data[actualPosition];
            actualPosition++;
            return returnValue;
        }

        public static XwingPosition FromByteArray(byte[] Data)
        {
            XwingPosition xwp = new XwingPosition();
            int actualPosition = 0;
            bool reverse = BitConverter.IsLittleEndian;

            xwp.position.X = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.position.Y = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.position.Z = ConvertFloatFromArray(Data, ref actualPosition, reverse);

            xwp.rotation.W = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.rotation.X = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.rotation.Y = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.rotation.Z = ConvertFloatFromArray(Data, ref actualPosition, reverse);

            xwp.color.W = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.color.X = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.color.Y = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.color.Z = ConvertFloatFromArray(Data, ref actualPosition, reverse);

            xwp.uid = ConvertIntFromArray(Data, ref actualPosition, reverse);

            xwp.newBullet = new Bullet();

            xwp.newBullet.position.X = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.newBullet.position.Y = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.newBullet.position.Z = ConvertFloatFromArray(Data, ref actualPosition, reverse);

            xwp.newBullet.rotation.W = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.newBullet.rotation.X = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.newBullet.rotation.Y = ConvertFloatFromArray(Data, ref actualPosition, reverse);
            xwp.newBullet.rotation.Z = ConvertFloatFromArray(Data, ref actualPosition, reverse);

            xwp.newBullet.ownerUid= ConvertIntFromArray(Data, ref actualPosition, reverse);

            xwp.killedByUid = ConvertIntFromArray(Data, ref actualPosition, reverse);

            xwp.shipModel = ConvertByteFromArray(Data, ref actualPosition, reverse);

            return xwp;
        }
    }
}
