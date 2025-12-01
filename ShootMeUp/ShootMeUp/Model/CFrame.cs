using ShootMeUp.Properties;
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
        /// <summary>
        /// The current object's position
        /// </summary>
        public (float X, float Y) Position;

        /// <summary>
        /// The current object's size
        /// </summary>
        public (int Width, int Height) Size;

        /// <summary>
        /// The current object's drawn image
        /// </summary>
        public Bitmap? Image { get; protected set; }

        /// <summary>
        /// Create a new CFrame
        /// </summary>
        /// <param name="X">The x pos</param>
        /// <param name="Y">The y pos</param>
        public CFrame(float X, float Y) : this(X, Y, 0, 0) { }

        /// <summary>
        /// Create a new CFrame
        /// </summary>
        /// <param name="X">The x pos</param>
        /// <param name="Y">The y pos</param>
        /// <param name="intSize">The x/y size</param>
        public CFrame(float X, float Y, int intSize) : this(X, Y, intSize, intSize) { }

        /// <summary>
        /// Create a new CFrame
        /// </summary>
        /// <param name="X">The x pos</param>
        /// <param name="Y">The y pos</param>
        /// <param name="intWidth">The width</param>
        /// <param name="intHeight">The height</param>
        public CFrame(float X, float Y, int intWidth, int intHeight)
        {
            Position.X = X;
            Position.Y = Y;
            this.Size.Width = intWidth;
            this.Size.Height = intHeight;

            // Default image (if nothing changes it)
            Image = Resources.EnemyZombiePigman;
        }

        public override string ToString()
        {
            return $"{{{Position.X},{Position.Y}}},{{{this.Size.Height},{this.Size.Width}}}";
        }

        public void DisposeImage()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
        }
    }
}
