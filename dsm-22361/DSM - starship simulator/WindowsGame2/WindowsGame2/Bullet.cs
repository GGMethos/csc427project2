using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Series3D2
{
    [Serializable]
    public class Bullet
    {
        public Vector3 position;
        public Quaternion rotation;
        public int ownerUid = 0;
        public bool played = false;
    }
}
