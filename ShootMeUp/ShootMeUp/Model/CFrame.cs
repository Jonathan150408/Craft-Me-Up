using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootMeUp.Model
{
    /// <summary>
    /// A basic Coordinate frame system
    /// </summary>
    public class CFrame
    {
        private float _fltX;
        private float _fltY;

        private int _intSize;

        public float X
        {
            get { return _fltX; }
            set
            {
                _fltX = value;
            }
        }

        public float Y
        {
            get { return _fltY; }
            set
            {
                _fltY = value;
            }
        }

        public int Size
        {
            get { return _intSize; }
            set { _intSize = value; }
        }

        public CFrame(int X, int Y, int intLength)
        {
            this.X = X;
            this.Y = Y;

            Size = intLength;
        }

        public CFrame(float X, float Y, int intLength)
        {
            this.X = X;
            this.Y = Y;

            Size = intLength;
        }

        public override string ToString()
        {
            return $"{{{X},{Y}}},{{{Size},{Size}}}";
        }
    }
}
