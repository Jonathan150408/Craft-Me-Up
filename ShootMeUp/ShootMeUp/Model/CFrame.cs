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
        public PictureBox DisplayedImage;

        public Label? HealthLabel { get; set; }

        /// <summary>
        /// Create a new CFrame
        /// </summary>
        /// <param name="X">The x pos</param>
        /// <param name="Y">The y pos</param>
        /// <param name="intWidth">The width</param>
        /// <param name="intHeight">The height</param>
        public CFrame(int X, int Y, int intWidth, int intHeight)
        {
            DisplayedImage = new PictureBox();

            DisplayedImage.Location = new Point(X, Y);
            DisplayedImage.Width = intWidth;
            DisplayedImage.Height = intHeight;

            DisplayedImage.SizeMode = PictureBoxSizeMode.StretchImage;
            DisplayedImage.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Create a new CFrame
        /// </summary>
        /// <param name="X">The x pos</param>
        /// <param name="Y">The y pos</param>
        /// <param name="intSize">The x/y size</param>
        public CFrame(int X, int Y, int intSize)
        {
            DisplayedImage = new PictureBox();

            DisplayedImage.Location = new Point(X, Y);
            DisplayedImage.Width = intSize;
            DisplayedImage.Height = intSize;

            DisplayedImage.SizeMode = PictureBoxSizeMode.StretchImage;
            DisplayedImage.BackColor = Color.Transparent;
        }

        /// <summary>
        /// Create a new CFrame
        /// </summary>
        /// <param name="X">The x pos</param>
        /// <param name="Y">The y pos</param>
        public CFrame(int X, int Y)
        {
            DisplayedImage = new PictureBox();

            DisplayedImage.Location = new Point(X, Y);

            DisplayedImage.SizeMode = PictureBoxSizeMode.StretchImage;
            DisplayedImage.BackColor = Color.Transparent;
        }

        public override string ToString()
        {
            return $"{{{DisplayedImage.Location.X},{DisplayedImage.Location.Y}}},{{{DisplayedImage.Height},{DisplayedImage.Width}}}";
        }
    }
}
