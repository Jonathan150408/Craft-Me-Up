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
        /// <summary>
        /// The character's health
        /// </summary>
        protected int _intHealth;

        /// <summary>
        /// The character's speed in the X direction
        /// </summary>
        protected float _fltXSpeed;

        /// <summary>
        /// The character's speed in the Y direction
        /// </summary>
        protected float _fltYSpeed;

        /// <summary>
        /// The character's type (player, ...)
        /// </summary>
        protected string _strType;

        /// <summary>
        /// The character's base speed
        /// </summary>
        protected float _fltBaseSpeed;

        /// <summary>
        /// A collision handler to check for collisions
        /// </summary>
        protected CollisionHandler _colCollisionHandler;        
        
        /// <summary>
        /// The character's remaining lives
        /// </summary>
        public int Lives
        {
            get { return _intHealth; }
            set { _intHealth = value; }
        }

        // <summary>
        /// The character's type (player, ...)
        /// </summary>
        public string Type 
        {
            get { return _strType; }
        }

        /// <summary>
        /// The character's base speed
        /// </summary>
        public float BaseSpeed
        {
            get { return _fltBaseSpeed; }
        }

        // Variables used for projectile cooldown
        protected DateTime _lastArrowShotTime = DateTime.MinValue;
        protected DateTime _lastFireballShotTime = DateTime.MinValue;

        protected TimeSpan ArrowCooldown = TimeSpan.FromSeconds(3);
        protected TimeSpan FireballCooldown = TimeSpan.FromSeconds(9);


        /// <summary>
        /// The character's constructor
        /// </summary>
        /// <param name="x">Its starting X position</param>
        /// <param name="y">Its starting Y position</param>
        /// <param name="length">The length of the character</param>
        /// <param name="strType">The character's type (player, zombie, skeleton, ...)</param>
        /// <param name="GAMESPEED">The game's speed</param>
        public Character(int x, int y, int length, string strType, int GAMESPEED) : base(x, y, length)
        {
            _intHealth = 10;
            _colCollisionHandler = new CollisionHandler();
            _strType = strType;
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
            bool[] tab_blnColliding = _colCollisionHandler.CheckForCollisions(currentCFrame, _fltXSpeed, _fltYSpeed);
            

            // Change the multiplicator for double-axis movement
            if (_fltXSpeed != 0 && _fltYSpeed != 0)
            {
                dblMultiplicator = 0.7;           
            }

            // Use the speed variables to change the character's position if the requirements are met.
            if (!tab_blnColliding[0])
                X += (float)(_fltXSpeed * dblMultiplicator);
            
            if (!tab_blnColliding[1])
                Y += (float)(_fltYSpeed * dblMultiplicator);
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
                _fltXSpeed = x * BaseSpeed;
                _fltYSpeed = y * BaseSpeed;
            }
        }

        virtual public Projectile? Shoot(Point clientPos, string strType, int GAMESPEED)
        {
            // Store the current time
            DateTime now = DateTime.Now;

            // Shoot an arrow from the player's position to the cursor's position if they are alive
            if (Lives > 0)
            {

                int intTargetX = clientPos.X;
                int intTargetY = clientPos.Y;

                int intProjectileLength = Size;

                // Send the corresponding projectile if the character is allowed to
                if (strType == "arrow" && now - _lastArrowShotTime >= ArrowCooldown)
                {
                    _lastArrowShotTime = now;

                    return new Projectile(strType, X, Y, intProjectileLength, this, intTargetX, intTargetY, GAMESPEED);
                }
                else if (strType == "fireball" && now - _lastFireballShotTime >= FireballCooldown)
                {
                    _lastFireballShotTime = now;

                    return new Projectile(strType, X, Y, intProjectileLength, this, intTargetX, intTargetY, GAMESPEED);
                }
            }

            return null;
        }

        public virtual void Render(BufferedGraphics drawingSpace)
        {
            // Only draw the character if they're alive
            if (Lives > 0)
            {
                drawingSpace.Graphics.DrawImage(Resources.CharacterPlayer, X, Y, Size, Size);
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
