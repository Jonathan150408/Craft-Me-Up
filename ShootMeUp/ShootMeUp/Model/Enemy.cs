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
        private TimeSpan DamageCooldown;

        /// <summary>
        /// The last time where the enemy hurt or sent a projectile towards the player
        /// </summary>
        public DateTime LastDamageTime = DateTime.MinValue;

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
        public Enemy(float x, float y, int length, Type type, int GAMESPEED, Character Target) : base(x, y, length, type, GAMESPEED)
        {
            _Target = Target;

            Position.X = x;
            Position.Y = y;

            // Set up the enemy depending on the current type
            switch (type)
            {
                case Type.Zombie:
                    ScoreValue = 1;
                    Lives = 10;

                    _fltBaseSpeed = 0.4f;
                    break;
                case Type.Skeleton:
                    ScoreValue = 3;
                    Lives = 5;

                    _fltBaseSpeed = -0.5f;

                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Arrow_Small;
                    break;
                case Type.Baby_Zombie:
                    ScoreValue = 4;
                    Lives = 3;

                    _fltBaseSpeed = 1.5f;

                    break;
                case Type.Blaze:
                    ScoreValue = 6;
                    Lives = 10;

                    _fltBaseSpeed = -0.25f;

                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Fireball_Small;
                    break;
                case Type.Zombie_Pigman:
                    ScoreValue = 5;
                    Lives = 20;

                    _fltBaseSpeed = 0.2f;

                    break;
                case Type.SpiderJockey:
                    ScoreValue = 20;
                    Lives = 25;

                    _fltBaseSpeed = 0.75f;

                    CanCollide = false;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Arrow_Jockey;
                    break;
                case Type.WitherSkeleton:
                    ScoreValue = 50;
                    Lives = 35;

                    _fltBaseSpeed = 0.5f;
                    break;
                case Type.Wither:
                    ScoreValue = 100;
                    Lives = 50;

                    _fltBaseSpeed = 0.2f;

                    CanCollide = false;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.WitherSkull;
                    break;
                case Type.Dragon:
                    ScoreValue = 250;
                    Lives = 100;

                    _fltBaseSpeed = 0.5f;

                    CanCollide = false;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.DragonFireball;

                    break;
                default:
                    _ProjectileType = Projectile.Type.Undefined;
                    break;
            }

            // Change the damage cooldown depending on the projectile type
            switch (_ProjectileType)
            {
                case Projectile.Type.Arrow_Small:
                case Projectile.Type.Arrow_Big:
                    DamageCooldown = TimeSpan.FromSeconds(6);
                    break;
                case Projectile.Type.Fireball_Small:
                case Projectile.Type.Fireball_Big:
                    DamageCooldown = TimeSpan.FromSeconds(12);
                    break;
                case Projectile.Type.WitherSkull:
                    DamageCooldown = TimeSpan.FromSeconds(4);
                    break;
                case Projectile.Type.DragonFireball:
                    DamageCooldown = TimeSpan.FromSeconds(10);
                    break;
                default:
                    // No projectile, check the enemy type
                    switch (type)
                    {
                        case Type.Baby_Zombie:
                            DamageCooldown = TimeSpan.FromSeconds(3);
                            break;
                        case Type.Zombie_Pigman:
                            DamageCooldown = TimeSpan.FromSeconds(8);
                            break;
                        default:
                            DamageCooldown = TimeSpan.FromSeconds(5);

                            break;
                    }
                    break;
            }

            // Divide the speed by GAMESPEED
            DamageCooldown = TimeSpan.FromSeconds(DamageCooldown.TotalSeconds / GAMESPEED);

            LastDamageTime = DateTime.Now;
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
                // Skip the current obstacle if it has no collisions or is invincible
                if (!obstacle.CanCollide || obstacle.Invincible)
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

            // Calculate direction to target
            float deltaX = _Target.Position.X - Position.X;
            float deltaY = _Target.Position.Y - Position.Y;

            float length = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length != 0)
            {
                deltaX /= length;
                deltaY /= length;
            }

            // Apply game speed and base speed
            float speedX = deltaX * _GAMESPEED * _fltBaseSpeed;
            float speedY = deltaY * _GAMESPEED * _fltBaseSpeed;

            // Move smoothly along X and Y axes
            Position.X = MoveAxis(Position.X, Position.Y, speedX, true);
            Position.Y = MoveAxis(Position.X, Position.Y, speedY, false);

            // Store the current speed for reference
            _fltSpeed.X = speedX;
            _fltSpeed.Y = speedY;

            // Handle attacking the player or obstacle
            HandleAttackOrShoot();
        }

        /// <summary>
        /// Handles damage and shooting logic after movement
        /// </summary>
        private void HandleAttackOrShoot()
        {
            if (!_blnShoots)
            {
                if (DateTime.Now < _nextUpdateTime) return;

                bool blnPlayerCollision = CheckPlayerCollision();
                Obstacle? obstacleHit = GetCollidingObstacle();

                if (blnPlayerCollision || (obstacleHit != null && !obstacleHit.Invincible))
                    _nextUpdateTime = DateTime.Now + DamageCooldown;

                if (blnPlayerCollision)
                    Damage((CFrame)_Target);
                else if (obstacleHit != null && !obstacleHit.Invincible)
                    Damage((CFrame)obstacleHit);
            }
            else
            {
                TimeSpan TimeSinceLastDamage = DateTime.Now - LastDamageTime;
                bool blnCanShoot = TimeSinceLastDamage > DamageCooldown;

                if (!blnCanShoot)
                    return;

                if (_Target.Lives <= 0) return;

                Projectile? proj = Shoot();
                if (proj != null)
                {
                    ShootMeUp.Projectiles.Add(proj);
                    
                    LastDamageTime = DateTime.Now;
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
            // Get the enemy's damage
            int intDamage = 1;
            switch (CharType)
            {
                case Type.WitherSkeleton:
                    intDamage = 5;
                    break;
            }

            if (singularCFrame is Character player)
            {
                player.Lives -= intDamage;
            }
            else if (singularCFrame is Obstacle obstacle)
            {
                obstacle.Health -= intDamage;

            }
        }
    }
}
