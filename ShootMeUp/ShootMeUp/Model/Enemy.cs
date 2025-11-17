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
        public Enemy(int x, int y, int length, Character.Type type, int GAMESPEED, Character Target) : base(x, y, length, type, GAMESPEED)
        {
            _Target = Target;

            // Set up the enemy depending on the current type
            switch (type)
            {
                case Character.Type.Zombie:
                    ScoreValue = 1;
                    Lives = 10;
                    _intBaseSpeed = 3;

                    break;
                case Character.Type.Skeleton:
                    ScoreValue = 3;
                    Lives = 5;
                    _intBaseSpeed = -2;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Arrow;

                    break;
                case Character.Type.Baby_Zombie:
                    ScoreValue = 2;
                    Lives = 3;
                    _intBaseSpeed = 4;

                    DamageCooldown = TimeSpan.FromSeconds(3);

                    break;
                case Character.Type.Blaze:
                    ScoreValue = 5;
                    Lives = 10;
                    _intBaseSpeed = -1;
                    _blnShoots = true;
                    _ProjectileType = Projectile.Type.Fireball;

                    break;
                case Character.Type.Zombie_Pigman:
                    ScoreValue = 5;
                    Lives = 20;
                    _intBaseSpeed = 1;

                    DamageCooldown = TimeSpan.FromSeconds(8);

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
            CFrame cfrX = new CFrame(DisplayedImage.Location.X + _intSpeed.X, DisplayedImage.Location.Y, DisplayedImage.Width, DisplayedImage.Height);
            CFrame cfrY = new CFrame(DisplayedImage.Location.X, DisplayedImage.Location.Y + _intSpeed.Y, DisplayedImage.Width, DisplayedImage.Height);

            // Check for collision
            foreach (Character character in ShootMeUp.Characters)
            {
                if (ShootMeUp.IsOverlapping(cfrX, character))
                {
                    blnColliding = true;
                }

                if (ShootMeUp.IsOverlapping(cfrY, character))
                {
                    blnColliding = true;
                }

                // Early exit if collision detected
                if (blnColliding)
                    break;
            }

            return blnColliding;
        }

        public Obstacle? GetCollidingObstacle()
        {
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
        /// <param name="player"></param>
        public void Move(Character player)
        {
            // Calculate direction to target
            int deltaX = player.DisplayedImage.Location.X - DisplayedImage.Location.X;
            int deltaY = player.DisplayedImage.Location.Y - DisplayedImage.Location.Y;

            // Normalize direction
            int length = (int)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Divide the delta positions by the length if it isn't equal to 0
            if (length != 0)
            {
                deltaX /= length;
                deltaY /= length;
            }

            // Multiply the movement variables to match the game speed
            deltaX *= _GAMESPEED;
            deltaY *= _GAMESPEED;

            base.Move(deltaX, deltaY);

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
                (bool X, bool Y) blnObstacleColliding = CheckObstacleCollision();
                Obstacle? obstacleHit = GetCollidingObstacle();

                // Set the cooldown to the next update if there's anything in front of the enemy
                if (!blnPlayerCollision || (!(blnObstacleColliding.X && blnObstacleColliding.Y) && (obstacleHit != null && !obstacleHit.Invincible)))
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
                int intTargetX = _Target.DisplayedImage.Location.X;
                int intTargetY = _Target.DisplayedImage.Location.Y;

                // Slow the projectile down by dividing its GAMESPEED reference by 2
                int intFakeGameSpeed = _GAMESPEED/2;

                // Fire a new projectile if possible
                return new Projectile(_ProjectileType, this, intTargetX, intTargetY, intFakeGameSpeed);
            }

            return null;
        }

        /// <summary>
        /// Damage the given CFrame if it's a character or an obstacle
        /// </summary>
        /// <param name="singularCFrame">The given CFrame</param>
        public void Damage(CFrame singularCFrame)
        {
            if (singularCFrame is Character)
            {
                Character player = (Character)singularCFrame;

                player.Lives -= 1;
            }
            else if (singularCFrame is Obstacle)
            {
                Obstacle obstacle = (Obstacle)singularCFrame;

                obstacle.Health -= 1;

            }
        }

        //// Center the text above the obstacle
        //drawingSpace.Graphics.DrawString($"{this}", TextHelpers.drawFont, TextHelpers.writingBrush, centeredX, Y - 16);

    }
}
