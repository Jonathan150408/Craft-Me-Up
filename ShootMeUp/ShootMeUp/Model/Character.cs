using ShootMeUp.Helpers;
using ShootMeUp.Properties;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ShootMeUp.Model
{
    /// <summary>
    /// The base class for characters.
    /// </summary>
    public class Character : CFrame
    {
        protected int _GAMESPEED;

        /// <summary>
        /// The character's speed in the X and Y direction
        /// </summary>
        protected (int X, int Y) _intSpeed;

        /// <summary>
        /// The character's base speed
        /// </summary>
        protected int _intBaseSpeed;

        // Variables used for projectile cooldown
        protected DateTime _lastArrowShotTime = DateTime.MinValue;
        protected DateTime _lastFireballShotTime = DateTime.MinValue;

        protected TimeSpan ArrowCooldown = TimeSpan.FromSeconds(3);
        protected TimeSpan FireballCooldown = TimeSpan.FromSeconds(9);

        protected Type _Type;

        /// <summary>
        /// The character's remaining lives
        /// </summary>
        public int Lives { get; set; }

        /// <summary>
        /// The character's type (player, ...)
        /// </summary>
        public enum Type
        {
            Player,
            Zombie,
            Skeleton,
            Baby_Zombie,
            Blaze,
            Zombie_Pigman
        }

        // <summary>
        /// The character's current type
        /// </summary>
        public Type CharType
        {
            get { return _Type; }
        }

        /// <summary>
        /// The character's constructor
        /// </summary>
        /// <param name="x">Its starting X position</param>
        /// <param name="y">Its starting Y position</param>
        /// <param name="length">The length of the character</param>
        /// <param name="type">The character's type (player, enemy)</param>
        /// <param name="GAMESPEED">The game's speed</param>
        public Character(int x, int y, int length, Character.Type type, int GAMESPEED) : base(x, y, length)
        {
            _GAMESPEED = GAMESPEED;
            Lives = 10;
            _Type = type;
            _intBaseSpeed = 4;

            if (CharType != Character.Type.Player)
            {
                HealthLabel = new Label();
                HealthLabel.AutoSize = true;
                HealthLabel.BackColor = Color.Transparent;
                HealthLabel.ForeColor = Color.White;
                HealthLabel.Font = new Font("Arial", 10, FontStyle.Bold);
            }

            ArrowCooldown = TimeSpan.FromSeconds(ArrowCooldown.TotalSeconds / GAMESPEED);
            FireballCooldown = TimeSpan.FromSeconds(FireballCooldown.TotalSeconds / GAMESPEED);

            if (_Type == Type.Player)
                DisplayedImage.Image = Resources.CharacterPlayer;            
        }

        protected (bool X, bool Y) CheckObstacleCollision()
        {
            (bool X, bool Y) blnColliding = (false, false);

            // Create hypothetical CFrames to simulate movement along each axis independently
            CFrame cfrX = new CFrame(DisplayedImage.Location.X + _intSpeed.X, DisplayedImage.Location.Y, DisplayedImage.Width, DisplayedImage.Height);
            CFrame cfrY = new CFrame(DisplayedImage.Location.X, DisplayedImage.Location.Y + _intSpeed.Y, DisplayedImage.Width, DisplayedImage.Height);

            foreach (Obstacle obstacle in ShootMeUp.Obstacles)
            {
                // Skip the current obstacle if it has no collisions
                if (!obstacle.CanCollide)
                    continue;

                if (ShootMeUp.IsOverlapping(cfrX, obstacle))
                {
                    blnColliding.X = true; // Collision if moved along X axis
                }

                if (ShootMeUp.IsOverlapping(cfrY, obstacle))
                {
                    blnColliding.Y = true; // Collision if moved along Y axis
                }

                // Early exit if both collisions detected
                if (blnColliding.X && blnColliding.Y)
                    break;
            }

            return blnColliding;
        }

        /// <summary>
        /// Move the character on both axis if they're alive
        /// </summary>
        /// <param name="x">The movement on the x axis</param>
        /// <param name="y">The movement on the y axis</param>
        public void Move(int x, int y)
        {
            if (Lives > 0)
            {
                _intSpeed.X = x * _intBaseSpeed;
                _intSpeed.Y = y * _intBaseSpeed;

                // Variables used for speed calculation
                int X = DisplayedImage.Location.X;
                int Y = DisplayedImage.Location.Y;

                // Get the current CFrame
                CFrame currentCFrame = (CFrame)this;

                // Check to see if the character is gonna clip in anything
                (bool X, bool Y) blnColliding = CheckObstacleCollision();

                // Let the player move in the given direction if there wouldn't be any collisions
                if (!blnColliding.X)
                    X += _intSpeed.X;

                if (!blnColliding.Y)
                    Y += _intSpeed.Y;

                DisplayedImage.Location = new Point(X, Y);
            }
        }

        /// <summary>
        /// Shoot a projectile
        /// </summary>
        /// <param name="target">The projectile's target</param>
        /// <param name="type">The projectile type</param>
        /// <returns>A projectile if it shot, otherwise none</returns>
        public Projectile? Shoot(Point target, Projectile.Type type)
        {
            // Store the current time
            DateTime now = DateTime.Now;

            // Shoot an arrow from the player's position to the cursor's position if they are alive
            if (Lives > 0)
            {
                // Create variables used for the projectile's generation
                int intProjectileX = DisplayedImage.Location.X;
                int intProjectileY = DisplayedImage.Location.Y;

                int intTargetX = target.X;
                int intTargetY = target.Y;

                // Get the character's center
                int intCharacterCenterX = DisplayedImage.Location.X + (DisplayedImage.Width / 2);
                int intCharacterCenterY = DisplayedImage.Location.Y + (DisplayedImage.Height / 2);


                // Send the corresponding projectile if the character is allowed to
                if (type == Projectile.Type.Arrow && now - _lastArrowShotTime >= ArrowCooldown)
                {
                    _lastArrowShotTime = now;

                    return new Projectile(type, this, intTargetX, intTargetY, _GAMESPEED);
                }
                else if (type == Projectile.Type.Fireball && now - _lastFireballShotTime >= FireballCooldown)
                {
                    _lastFireballShotTime = now;

                    return new Projectile(type, this, intTargetX, intTargetY, _GAMESPEED);
                }
            }

            return null;
        }

        //// Draw the lives of the character
        //for (int i = 0; i<Lives; i++)
        //{
        //    // Draw the PlayerToken as many times as there are lives
        //    drawingSpace.Graphics.DrawImage(Resources.CharacterPlayer, (16 * i) + (8 * i) + 8, 32, 16, 16);
        //}

        public override string ToString()
        {
            if (Lives > 0)
                return $"{((int)((double)Lives)).ToString()} HP";
            else
                return "";
        }
    }
}