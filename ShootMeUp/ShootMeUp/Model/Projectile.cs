using ShootMeUp.Properties;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace ShootMeUp.Model
{
    public class Projectile : CFrame
    {
        /// <summary>
        /// The character that shot the projectile
        /// </summary>
        private Character _shotBy;

        /// <summary>
        /// The amount of damage the projectile deals (in HP)
        /// </summary>
        private int _intDamage;

        /// <summary>
        /// The projectile's movement speed
        /// </summary>
        private float _fltMovementSpeed;

        /// <summary>
        ///  The projectile's speed in the X and Y axis
        /// </summary>
        private (float X, float Y) _fltSpeed;

        /// <summary>
        /// The X and Y position of the target
        /// </summary>
        private (float X, float Y) _fltTarget;

        /// <summary>
        /// The projectile's type (arrow, ...)
        /// </summary>
        public enum Type
        {
            Arrow_Small,
            Arrow_Big,
            Arrow_Jockey,
            Fireball_Small,
            Fireball_Big,
            WitherSkull,
            DragonFireball,
            Undefined
        }

        /// <summary>
        /// Whether or not the projectile is active or not
        /// </summary>
        public bool Active { get; set; }

        // <summary>
        /// The Projectile's current type
        /// </summary>
        public Type ProjType { get; private set; }

        /// <summary>
        /// The rotation angle (in degrees)
        /// </summary>
        public float RotationAngle { get; private set; }

        public Projectile(Type type, Character ShotBy, float fltTargetX, float fltTargetY, int GAMESPEED): base(0, 0)
        {
            ProjType = type;
            _shotBy = ShotBy;
            _fltTarget.X = fltTargetX;
            _fltTarget.Y = fltTargetY;

            Active = true;
            CanCollide = true;

            // Define the different properties depending on the projectile type
            switch (type)
            {
                case Type.Arrow_Big:
                    this.Size.Width = ShotBy.Size.Width;
                    this.Size.Height = ShotBy.Size.Height;
                    _intDamage = 2;
                    _fltMovementSpeed = 3;

                    break;
                case Type.Arrow_Small:
                    this.Size.Width = ShotBy.Size.Width;
                    this.Size.Height = ShotBy.Size.Height;
                    _intDamage = 1;
                    _fltMovementSpeed = 2;

                    break;
                case Type.Arrow_Jockey:
                    this.Size.Width = ShotBy.Size.Width / 2;
                    this.Size.Height = ShotBy.Size.Height / 2;
                    _intDamage = 2;
                    _fltMovementSpeed = 1.8f;

                    break;
                case Type.Fireball_Big:
                    this.Size.Width = ShotBy.Size.Width;
                    this.Size.Height = ShotBy.Size.Height;
                    _intDamage = 5;
                    _fltMovementSpeed = 1;

                    break;
                case Type.Fireball_Small:
                    this.Size.Width = ShotBy.Size.Width;
                    this.Size.Height = ShotBy.Size.Height;
                    _intDamage = 2;
                    _fltMovementSpeed = 0.75f;

                    break;
                case Type.WitherSkull:
                    this.Size.Width = ShotBy.Size.Width / 4;
                    this.Size.Height = ShotBy.Size.Height / 4;
                    _intDamage = 3;
                    _fltMovementSpeed = 1f;

                    break;
                case Type.DragonFireball:
                    this.Size.Width = ShotBy.Size.Width / 4;
                    this.Size.Height = ShotBy.Size.Height / 4;
                    _intDamage = 5;
                    _fltMovementSpeed = 1;

                    CanCollide = false;

                    break;
                default:
                    this.Size.Width = ShotBy.Size.Width;
                    this.Size.Height = ShotBy.Size.Height;
                    _intDamage = 0;
                    _fltMovementSpeed = -1;
                    break;
            }

            // Center projectile on shooter
            float shooterCenterX = ShotBy.Position.X + ShotBy.Size.Width / 2f;
            float shooterCenterY = ShotBy.Position.Y + ShotBy.Size.Height / 2f;

            Position.X = shooterCenterX - Size.Width / 2f;
            Position.Y = shooterCenterY - Size.Height / 2f;

            // Multiply the movement speed by the game speed
            _fltMovementSpeed *= GAMESPEED;

            // Calculate direction to target
            float projCenterX = Position.X + Size.Width / 2f;
            float projCenterY = Position.Y + Size.Height / 2f;

            float deltaX = _fltTarget.X - projCenterX;
            float deltaY = _fltTarget.Y - projCenterY;

            // Calculate rotation angle in degrees
            // We add 90 here because the image faces upwards
            RotationAngle = (float)(Math.Atan2(deltaY, deltaX) * (180.0 / Math.PI)) + 90;

            // Normalize direction
            float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length != 0)
            {
                deltaX /= length;
                deltaY /= length;
            }

            // Store movement speed in X/Y components
            _fltSpeed.X = (deltaX * _fltMovementSpeed);
            _fltSpeed.Y = (deltaY * _fltMovementSpeed);
        }

        /// <summary>
        /// Update the projectile's position
        /// </summary>
        public void Update()
        {
            // Check to see if the projectile is gonna hit anything
            CFrame? Hit = GetColliding();

            // Move the arrow if it wouldn't hit anything
            if (Hit == null)
            {
                Position.X += _fltSpeed.X * ShootMeUp.DeltaTime;
                Position.Y += _fltSpeed.Y * ShootMeUp.DeltaTime;
            }
            else
            {
                // Mark the projectile as inactive
                Active = false;

                if (Hit is Character)
                {
                    Character characterHit = (Character)Hit;

                    // Deal damage to the character
                    characterHit.Lives -= _intDamage;
                }
                else
                {
                    Obstacle obstacleHit = (Obstacle)Hit;

                    // Deal damage to the obstacle
                    obstacleHit.Health -= _intDamage;
                }
            }
        }

        public CFrame? GetColliding()
        {
            // Create hypothetical CFrames to simulate movement along each axis independently
            float stepX = _fltSpeed.X * ShootMeUp.DeltaTime;
            float stepY = _fltSpeed.Y * ShootMeUp.DeltaTime;

            CFrame cfrX = new(Position.X + stepX, Position.Y, Size.Width, Size.Height);
            CFrame cfrY = new(Position.X, Position.Y + stepY, Size.Width, Size.Height);

            // Create a list that contains both obstacles (only if CanCollide is true) and characters
            List<CFrame> listCFrames = new List<CFrame>();
            listCFrames = [.. ShootMeUp.Characters.Cast<CFrame>()];
            if (CanCollide)
                listCFrames.AddRange([.. ShootMeUp.Obstacles.Cast<CFrame>()]);

            foreach (CFrame singularCFrame in listCFrames)
            {
                // Skip the ignored character
                if (singularCFrame == (CFrame)_shotBy)
                    continue;

                // Check to see if the current CFrame is an obstacle
                if (singularCFrame is Obstacle)
                {
                    Obstacle obstacle = (Obstacle)singularCFrame;

                    // Skip the current obstacle if it has no collisions
                    if (!obstacle.CanCollide)
                        continue;
                }


                if (ShootMeUp.IsOverlapping(cfrX, singularCFrame))
                {
                    return singularCFrame;
                }

                if (ShootMeUp.IsOverlapping(cfrY, singularCFrame))
                {
                    return singularCFrame;
                }
            }

            return null;
        }
    }
}