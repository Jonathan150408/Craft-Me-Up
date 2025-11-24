using ShootMeUp.Helpers;
using ShootMeUp.Properties;
using System.Numerics;

namespace ShootMeUp.Model
{
    /// <summary>
    /// The enemy class, with more attributes than the regular character
    /// </summary>
    public class Enemy : Character
    {
        /// <summary>
        /// Whether or not the enemy can shoot
        /// </summary>
        private bool _blnShoots;

        /// <summary>
        /// The enemy's projectile type
        /// </summary>
        private Projectile.Type _ProjectileType;

        /// <summary>
        /// The enemy's target
        /// </summary>
        private Character _Target;

        /// <summary>
        /// The time until the enemy's next update
        /// </summary>
        private DateTime _nextUpdateTime = DateTime.MinValue;

        /// <summary>
        /// How long the cooldown lasts after damaging a player
        /// </summary>
        private TimeSpan DamageCooldown = TimeSpan.FromSeconds(5);

        /// <summary>
        /// The score that the enemy gives when it dies
        /// </summary>
        public int ScoreValue { get; private set; }

        /// <summary>
        /// The shooting enemy's constructor
        /// </summary>
        /// <param name="x">Its starting X position</param>
        /// <param name="y">Its starting Y position</param>
        /// <param name="length">The length of the character</param>
        /// <param name="type">The enemy's type (zombie, skeleton, ...)</param>
        /// <param name="GAMESPEED">The game's speed</param>
        /// <param name="Target">The enemy's target</param>
        public Enemy(float x, float y, int length, Character.Type type, int GAMESPEED, Character Target) : base(x, y, length, type, GAMESPEED)
        {
            _Target = Target;

            Position.X = x;
            Position.Y = y;

            // Set up the enemy depending on the current type
            switch (type)
            {
                case Character.Type.Zombie:
                    ScoreValue = 1;
                    Lives = 10;
                    _fltBaseSpeed = 3;

                    Image = Resources.EnemyZombie;
                    break;
                case Character.Type.Skeleton:
                    ScoreValue = 3;
                    Lives = 5;
                    _fltBaseSpeed = -2;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Arrow;

                    Image = Resources.EnemySkeleton;
                    break;
                case Character.Type.Baby_Zombie:
                    ScoreValue = 2;
                    Lives = 3;
                    _fltBaseSpeed = 4;

                    DamageCooldown = TimeSpan.FromSeconds(3);

                    Image = Resources.EnemyZombie;
                    break;
                case Character.Type.Blaze:
                    ScoreValue = 5;
                    Lives = 10;
                    _fltBaseSpeed = -1;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Fireball;

                    Image = Resources.EnemyBlaze;
                    break;
                case Character.Type.Zombie_Pigman:
                    ScoreValue = 5;
                    Lives = 20;
                    _fltBaseSpeed = 1;

                    DamageCooldown = TimeSpan.FromSeconds(8);

                    Image = Resources.EnemyZombiePigman;
                    break;
                default:
                    break;
            }
            
            DamageCooldown = TimeSpan.FromSeconds(DamageCooldown.TotalSeconds / GAMESPEED);
            ArrowCooldown = TimeSpan.FromSeconds(20 / GAMESPEED);
            FireballCooldown = TimeSpan.FromSeconds(20 / GAMESPEED);
            _lastArrowShotTime = DateTime.Now;
            _lastFireballShotTime = DateTime.Now;
        }

        public bool CheckPlayerCollision()
        {
            bool blnColliding = false;

            // Create hypothetical CFrames to simulate movement along each axis independently
            CFrame cfrX = new(Position.X + _fltSpeed.X, Position.Y, this.Size.Width, this.Size.Height);
            CFrame cfrY = new(Position.X, Position.Y + _fltSpeed.Y, this.Size.Width, this.Size.Height);

            // Check for collision
            if (ShootMeUp.IsOverlapping(cfrX, _Target))
            {
                blnColliding = true;
            }

            if (ShootMeUp.IsOverlapping(cfrY, _Target))
            {
                blnColliding = true;
            }

            return blnColliding;
        }

        public Obstacle? GetCollidingObstacle()
        {
            // Create hypothetical CFrames to simulate movement along each axis independently
            CFrame cfrX = new(Position.X + _fltSpeed.X, Position.Y, this.Size.Width, this.Size.Height);
            CFrame cfrY = new(Position.X, Position.Y + _fltSpeed.Y, this.Size.Width, this.Size.Height);

            foreach (Obstacle obstacle in ShootMeUp.Obstacles)
            {
                // Skip the current obstacle if it has no collisions
                if (!obstacle.CanCollide)
                    continue;


                if (ShootMeUp.IsOverlapping(cfrX, obstacle))
                {
                    return obstacle;
                }

                if (ShootMeUp.IsOverlapping(cfrY, obstacle))
                {
                    return obstacle;
                }
            }

            return null;
        }

        /// <summary>
        /// Move the enemy to the player
        /// </summary>
        public void Move()
        {
            if (Lives <= 0) return;

            // Calculate direction to target as floats
            float deltaX = _Target.Position.X - Position.X;
            float deltaY = _Target.Position.Y - Position.Y;

            float length = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (length != 0)
            {
                deltaX /= length; // normalize
                deltaY /= length;
            }

            // Apply game speed and base speed
            float speedX = deltaX * _GAMESPEED * _fltBaseSpeed;
            float speedY = deltaY * _GAMESPEED * _fltBaseSpeed;

            (bool collX, bool collY) blnColliding = CheckObstacleCollision();

            // Move only if no collision
            if (!blnColliding.collX) Position.X += speedX;
            if (!blnColliding.collY) Position.Y += speedY;

            // Update speed for reference
            _fltSpeed.X = (int)speedX;
            _fltSpeed.Y = (int)speedY;

            // Only deal contact damage if the enemy isn't a shooter
            if (!_blnShoots)
            {
                // Skip the attack check if the enemy is on damage cooldown
                if (DateTime.Now < _nextUpdateTime && !_blnShoots)
                    return;

                // Get the current CFrame
                CFrame currentCFrame = (CFrame)this;

                // Get the character or obstacle in front of the enemy
                bool blnPlayerCollision = CheckPlayerCollision();
                Obstacle? obstacleHit = GetCollidingObstacle();

                // Set the cooldown to the next update if there's anything in front of the enemy
                if (blnPlayerCollision || (obstacleHit != null && !obstacleHit.Invincible))
                {
                    // Set the cooldown to the next update
                    _nextUpdateTime = DateTime.Now + DamageCooldown;
                }

                // Deal damage to the player or the obstacle in front of the enemy
                if (blnPlayerCollision)
                {
                    // Damage the player
                    Damage((CFrame)_Target);

                }
                else if (obstacleHit != null && !obstacleHit.Invincible)
                {
                    Damage((CFrame)obstacleHit);

                    // Set the cooldown to the next update
                    _nextUpdateTime = DateTime.Now + DamageCooldown;
                }
            }
            else
            {
                // Skip the update if the enemy is on damage cooldown
                if ((_ProjectileType == Projectile.Type.Arrow && DateTime.Now - _lastArrowShotTime < ArrowCooldown) || (_ProjectileType == Projectile.Type.Fireball && DateTime.Now - _lastFireballShotTime < FireballCooldown))
                    return;

                // Stop trying to shoot if the player doesn't exist
                if (_Target.Lives <= 0)
                    return;

                // Shoot an arrow using the enemy's shoot method and add it to the projetile list
                Projectile? possibleProjectile = Shoot();
                //
                if (possibleProjectile != null)
                {
                    ShootMeUp.Projectiles.Add(possibleProjectile);

                    // Record the shot time
                    if (_ProjectileType == Projectile.Type.Arrow)
                        _lastArrowShotTime = DateTime.Now;
                    else if (_ProjectileType == Projectile.Type.Fireball)
                        _lastFireballShotTime = DateTime.Now;
                }
            }
        }

        /// <summary>
        /// Shoot a projectile
        /// </summary>
        public Projectile? Shoot()
        {
            
            // Store the current time
            DateTime now = DateTime.Now;

            // Shoot an arrow from the player's position to the cursor's position if they are alive
            if (Lives > 0)
            {
                float fltTargetX = _Target.Position.X;
                float fltTargetY = _Target.Position.Y;

                // Slow the projectile down by dividing its GAMESPEED reference by 2
                int intFakeGameSpeed = _GAMESPEED/2;

                // Fire a new projectile if possible
                return new(_ProjectileType, this, fltTargetX, fltTargetY, intFakeGameSpeed);
            }

            return null;
        }

        /// <summary>
        /// Damage the given CFrame if it's a character or an obstacle
        /// </summary>
        /// <param name="singularCFrame">The given CFrame</param>
        public void Damage(CFrame singularCFrame)
        {
            if (singularCFrame is Character player)
            {
                player.Lives -= 1;
            }
            else if (singularCFrame is Obstacle obstacle)
            {
                obstacle.Health -= 1;

            }
        }

        //// Center the text above the obstacle
        //drawingSpace.Graphics.DrawString($"{this}", TextHelpers.drawFont, TextHelpers.writingBrush, centeredX, Y - 16);

    }
}
