using ShootMeUp.Helpers;
using ShootMeUp.Properties;
using System.Numerics;

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
        protected (float X, float Y) _fltSpeed;

        /// <summary>
        /// The character's type (player, ...)
        /// </summary>
        public enum Type
        {
            Player,
            Enemy
        }

        protected Type _Type;

        /// <summary>
        /// The character's base speed
        /// </summary>
        protected float _fltBaseSpeed;

        // Variables used for projectile cooldown
        protected DateTime _lastArrowShotTime = DateTime.MinValue;
        protected DateTime _lastFireballShotTime = DateTime.MinValue;

        protected TimeSpan ArrowCooldown = TimeSpan.FromSeconds(3);
        protected TimeSpan FireballCooldown = TimeSpan.FromSeconds(9);

        /// <summary>
        /// The character's remaining lives
        /// </summary>
        public int Lives { get; set; }

        // <summary>
        /// The character's type (player, ...)
        /// </summary>
        public Type CurrentType 
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
            _fltBaseSpeed = 1f;

            ArrowCooldown = TimeSpan.FromSeconds(ArrowCooldown.TotalSeconds / GAMESPEED);
            FireballCooldown = TimeSpan.FromSeconds(FireballCooldown.TotalSeconds / GAMESPEED);
        }

        /// <summary>
        /// Update the character's position
        /// </summary>
        virtual public void Update()
        {
            // Variable used for multiplying the speed of the movement
            double dblMultiplicator = 1;

                       
            // Get the current CFrame
            CFrame currentCFrame = (CFrame)this;

            // Check to see if the character is gonna clip in anything
            bool[] tab_blnColliding = _colCollisionHandler.CheckForCollisions(currentCFrame, _fltSpeed.X, _fltSpeed.Y);
            

            // Change the multiplicator for double-axis movement
            if (_fltSpeed.X != 0 && _fltSpeed.Y != 0)
            {
                dblMultiplicator = 0.7;           
            }

            // Use the speed variables to change the character's position if the requirements are met.
            if (!tab_blnColliding[0])
<<<<<<< Updated upstream
                FloatX += (float)(_fltXSpeed * dblMultiplicator);
            
            if (!tab_blnColliding[1])
                FloatY += (float)(_fltYSpeed * dblMultiplicator);
=======
                X += (float)(_fltSpeed.X * dblMultiplicator);
            
            if (!tab_blnColliding[1])
                Y += (float)(_fltSpeed.Y * dblMultiplicator);
>>>>>>> Stashed changes
        }

        /// <summary>
        /// Move the character on both axis if they're alive
        /// </summary>
        /// <param name="x">The movement on the x axis</param>
        /// <param name="y">The movement on the y axis</param>
        public void Move(float x, float y)
        {
            if (Lives > 0)
            {
                _fltSpeed.X = x * _fltBaseSpeed;
                _fltSpeed.Y = y * _fltBaseSpeed;
            }
        }

        /// <summary>
        /// Shoot a projectile
        /// </summary>
        /// <param name="target">The projectile's target</param>
        /// <param name="type">The projectile type</param>
        /// <returns>A projectile if it shot, otherwise none</returns>
        virtual public Projectile? Shoot(PictureBox target, Projectile.Type type)
        {
            // Store the current time
            DateTime now = DateTime.Now;

            // Shoot an arrow from the player's position to the cursor's position if they are alive
            if (Lives > 0)
            {
                // Create variables used for the projectile's generation
                float fltProjectileX = FloatX;
                float fltProjectileY = FloatY;

                int intTargetX = target.Location.X;
                int intTargetY = target.Location.Y;

                int intProjectileLength = length;
                int intProjectileHeight = height;

                // Get the character's center
                float fltCharacterCenterX = FloatX + (length / 2f);
                float fltCharacterCenterY = FloatY + (height / 2f);

                // The projectile should start centered on the character
                fltProjectileX = fltCharacterCenterX;
                fltProjectileY = fltCharacterCenterY - (intProjectileHeight / 2f);

                // Resize the projectile if the aspect ratio is different
                if (strType == "arrow")
                {
                    // 8:29 aspect ratio
                    intProjectileLength = (intProjectileHeight * 8) / 29;
                }

                // Send the corresponding projectile if the character is allowed to
                if (type == Projectile.Type.Arrow && now - _lastArrowShotTime >= ArrowCooldown)
                {
                    _lastArrowShotTime = now;

<<<<<<< Updated upstream
                    return new Projectile(strType, fltProjectileX, fltProjectileY, intProjectileLength, intProjectileHeight, this, intTargetX, intTargetY, GAMESPEED);
=======
                    return new Projectile(type, X, Y, intProjectileLength, this, intTargetX, intTargetY, _GAMESPEED);
>>>>>>> Stashed changes
                }
                else if (type == Projectile.Type.Fireball && now - _lastFireballShotTime >= FireballCooldown)
                {
                    _lastFireballShotTime = now;

<<<<<<< Updated upstream
                    return new Projectile(strType, fltProjectileX, fltProjectileY, intProjectileLength, intProjectileHeight, this, intTargetX, intTargetY, GAMESPEED);
=======
                    return new Projectile(type, X, Y, intProjectileLength, this, intTargetX, intTargetY, _GAMESPEED);
>>>>>>> Stashed changes
                }
            }

            return null;
        }

        public virtual void Render(BufferedGraphics drawingSpace)
        {
            // Only draw the character if they're alive
            if (Lives > 0)
            {
                drawingSpace.Graphics.DrawImage(Resources.CharacterPlayer, FloatX, FloatY, length, height);
            }

            // Draw the lives of the character
            for (int i = 0; i < Lives; i++)
            {
                // Draw the PlayerToken as many times as there are lives
                drawingSpace.Graphics.DrawImage(Resources.CharacterPlayer, (16 * i) + (8 * i) + 8, 32, 16, 16);
            }
        }

        public override string ToString()
        {
            if (Lives > 0)
                return $"{((int)((double)Lives)).ToString()} HP";
            else
                return "";
        }
    }
}
