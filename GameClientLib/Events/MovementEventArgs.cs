using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tyranny.Networking.Events
{
    public class MovementEventArgs : EventArgs
    {
        public Guid Guid { get; set; }
        public Vector3 Position { get; set; }

        public class Vector3
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
    }
}
