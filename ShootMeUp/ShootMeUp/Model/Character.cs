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
        protected (float X, float Y) _fltSpeed;

        /// <summary>
        /// The character's base speed
        /// </summary>
        protected float _fltBaseSpeed;

        // Variables used for projectile cooldown
        protected DateTime _lastArrowShotTime = DateTime.MinValue;
        protected DateTime _lastFireballShotTime = DateTime.MinValue;

        protected TimeSpan ArrowCooldown = TimeSpan.FromSeconds(1.5);
        protected TimeSpan FireballCooldown = TimeSpan.FromSeconds(4.5);

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
        public Character(float x, float y, int length, Character.Type type, int GAMESPEED) : base(x, y, length)
        {
            _GAMESPEED = GAMESPEED;
            Lives = 10;
            _Type = type;
            _fltBaseSpeed = 1;

            ArrowCooldown = TimeSpan.FromSeconds(ArrowCooldown.TotalSeconds / GAMESPEED);
            FireballCooldown = TimeSpan.FromSeconds(FireballCooldown.TotalSeconds / GAMESPEED);
        }

        protected (bool X, bool Y) CheckObstacleCollision()
        {
            (bool X, bool Y) blnColliding = (false, false);

            // Create hypothetical CFrames to simulate movement along each axis independently
            CFrame cfrX = new CFrame(Position.X + _fltSpeed.X, Position.Y, this.Size.Width, this.Size.Height);
            CFrame cfrY = new CFrame(Position.X, Position.Y + _fltSpeed.Y, this.Size.Width, this.Size.Height);

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
        public void Move(float x, float y)
        {
            if (Lives <= 0) return;

            _fltSpeed.X = x * _fltBaseSpeed;
            _fltSpeed.Y = y * _fltBaseSpeed;

            float targetX = Position.X + _fltSpeed.X;
            float targetY = Position.Y + _fltSpeed.Y;

            // Move along X axis
            if (_fltSpeed.X != 0)
                Position.X = MoveAxis(Position.X, Position.Y, _fltSpeed.X, true);

            // Move along Y axis
            if (_fltSpeed.Y != 0)
                Position.Y = MoveAxis(Position.X, Position.Y, _fltSpeed.Y, false);
        }

        /// <summary>
        /// Incrementally moves along one axis until just before collision
        /// </summary>
        /// <param name="currentX">Current X position</param>
        /// <param name="currentY">Current Y position</param>
        /// <param name="delta">Movement along this axis</param>
        /// <param name="isX">True if moving along X, false if along Y</param>
        /// <returns>The new position along the axis</returns>
        protected float MoveAxis(float currentX, float currentY, float delta, bool isX)
        {
            float sign = Math.Sign(delta);
            float remaining = Math.Abs(delta);

            while (remaining > 0)
            {
                // Move by 1 pixel at a time (or smaller step for faster objects)
                float step = Math.Min(1f, remaining);

                float testX = currentX + (isX ? step * sign : 0);
                float testY = currentY + (isX ? 0 : step * sign);

                // Create a test CFrame at this new position
                CFrame testFrame = new CFrame(testX, testY, Size.Width, Size.Height);

                bool colliding = false;
                foreach (Obstacle obstacle in ShootMeUp.Obstacles)
                {
                    if (!obstacle.CanCollide) continue;

                    if (ShootMeUp.IsOverlapping(testFrame, obstacle))
                    {
                        colliding = true;
                        break;
                    }
                }

                if (colliding)
                    break; // Stop moving along this axis

                // Otherwise, move
                currentX = testX;
                currentY = testY;
                remaining -= step;
            }

            return isX ? currentX : currentY;
        }

        /// <summary>
        /// Shoot a projectile
        /// </summary>
        /// <param name="target">The projectile's target</param>
        /// <param name="type">The projectile type</param>
        /// <returns>A projectile if it shot, otherwise none</returns>
        public Projectile? Shoot(CFrame target, Projectile.Type type)
        {
            // Store the current time
            DateTime now = DateTime.Now;

            // Shoot an arrow from the player's position to the cursor's position if they are alive
            if (Lives > 0)
            {
                // Send the corresponding projectile if the character is allowed to
                if (type == Projectile.Type.Arrow && now - _lastArrowShotTime >= ArrowCooldown)
                {
                    _lastArrowShotTime = now;

                    return new Projectile(type, this, target.Position.X, target.Position.Y, _GAMESPEED);
                }
                else if (type == Projectile.Type.Fireball_Big && now - _lastFireballShotTime >= FireballCooldown)
                {
                    _lastFireballShotTime = now;

                    return new Projectile(type, this, target.Position.X, target.Position.Y, _GAMESPEED);
                }
            }

            return null;
        }

        public override string ToString()
        {
            if (Lives > 0)
                return $"{Lives} HP";
            else
                return "";
        }
    }
}