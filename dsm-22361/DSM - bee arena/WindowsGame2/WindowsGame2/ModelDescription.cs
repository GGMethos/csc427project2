using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;

namespace WindowsGame2
{
    class ModelDescription
    {
        public Vector3 CameraPosition = new Vector3();
        public Model ShipModel = null;
        public ArrayList ModelColors = new ArrayList();
        public float RotationX = 0;
        public float RotationY = 0;
        public float RotationZ = 0;
        public float Scale = 0;
    }
}
