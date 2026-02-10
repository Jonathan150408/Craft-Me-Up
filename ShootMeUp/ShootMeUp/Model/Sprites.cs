using ShootMeUp.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootMeUp.Model
{
    static class Sprites
    {
        // Characters
        public static Bitmap Player             => Resources.Character_Player;
        public static Bitmap Zombie             => Resources.Character_Zombie;
        public static Bitmap Skeleton           => Resources.Character_Skeleton;
        public static Bitmap Pigman             => Resources.Character_Zombie_Pigman;
        public static Bitmap BabyZombie         => Resources.Character_Zombie;
        public static Bitmap Blaze              => Resources.Character_Blaze;
        public static Bitmap SpiderJockey       => Resources.Character_SpiderJockey;
        public static Bitmap Dragon             => Resources.Character_Dragon;
        public static Bitmap Wither             => Resources.Character_Wither;

        // Projectiles
        public static Bitmap Arrow              => Resources.Projectile_Arrow;
        public static Bitmap Fireball           => Resources.Projectile_Fireball;
        public static Bitmap WitherSkull        => Resources.Projectile_WitherSkull;
        public static Bitmap DragonFireball     => Resources.Projectile_DragonFireball;

        // Obstacles
        public static Bitmap Dirt               => Resources.Obstacle_Dirt;
        public static Bitmap WoodenPlanks       => Resources.Obstacle_WoodPlanks;
        public static Bitmap Cobblestone        => Resources.Obstacle_Cobblestone;
        public static Bitmap Spawner            => Resources.Obstacle_Spawner;
        public static Bitmap Bedrock            => Resources.Obstacle_Bedrock;
        public static Bitmap Barrier            => Resources.Obstacle_Barrier;
        public static Bitmap Bush               => Resources.Obstacle_Bush;

        // Floor
        public static Bitmap Grass              => Resources.Floor_Grass;
        public static Bitmap Stone              => Resources.Floor_Stone;
        public static Bitmap Sand               => Resources.Floor_Sand;

        // Misc
        public static Bitmap PlayerHeart        => Resources.Misc_PlayerHeart;
        public static Bitmap EmptyHeart         => Resources.Misc_EmptyHeart;
        public static Bitmap EnemyHeart         => Resources.Misc_EnemyHeart;
        public static Bitmap BossHeart          => Resources.Misc_BossHeart;



        // Cache
        private static readonly ConcurrentDictionary<(Projectile.Type, int), Bitmap> _rotatedProjectiles = new();

        // Tint cache for floors
        private static readonly ConcurrentDictionary<Color, Bitmap> _tintedFloors = new();

        // Tiled floor cache: (floorType, size) -> bitmap
        private static readonly ConcurrentDictionary<(Obstacle.Type, int, int), Bitmap> _tiledFloors = new();

        public static Bitmap GetCharacterSprite(Character.Type type)
        {
            return type switch
            {
                Character.Type.Player => Player,
                Character.Type.Zombie => Zombie,
                Character.Type.Skeleton => Skeleton,
                Character.Type.Baby_Zombie => BabyZombie,
                Character.Type.Blaze => Blaze,
                Character.Type.Zombie_Pigman => Pigman,
                Character.Type.SpiderJockey => SpiderJockey,
                Character.Type.Wither => Wither,
                Character.Type.Dragon => Dragon,
                _ => Player
            };
        }

        private static Bitmap GetBaseObstacle(Obstacle.Type type)
        {
            return type switch
            {
                Obstacle.Type.Dirt => Dirt,
                Obstacle.Type.Wood => WoodenPlanks,
                Obstacle.Type.CobbleStone => Cobblestone,
                Obstacle.Type.Barrier => Barrier,
                Obstacle.Type.Bedrock => Bedrock,
                Obstacle.Type.Spawner => Spawner,
                Obstacle.Type.Bush => Bush,
                Obstacle.Type.Grass => GetGrass(Color.FromArgb(145, 189, 89)),
                Obstacle.Type.Stone => Stone,
                Obstacle.Type.Sand => Sand,
                _ => Player
            };
        }

        private static Bitmap GetBaseProjectile(Projectile.Type type)
        {
            return type switch
            {
                Projectile.Type.Arrow_Small or Projectile.Type.Arrow_Big or Projectile.Type.Arrow_Jockey => Arrow,
                Projectile.Type.Fireball_Small or Projectile.Type.Fireball_Big => Fireball,
                Projectile.Type.WitherSkull => WitherSkull,
                Projectile.Type.DragonFireball => DragonFireball,
                _ => Arrow
            };
        }

        private static Bitmap TileSprite(Bitmap baseSprite, int width, int height)
        {
            Bitmap tiled = new(width, height);

            using (Graphics g = Graphics.FromImage(tiled))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;

                for (int x = 0; x < width; x += ShootMeUp.OBSTACLE_SIZE)
                {
                    for (int y = 0; y < height; y += ShootMeUp.OBSTACLE_SIZE)
                    {
                        g.DrawImage(baseSprite, x, y, ShootMeUp.OBSTACLE_SIZE, ShootMeUp.OBSTACLE_SIZE);
                    }
                }
            }

            return tiled;
        }

        public static Bitmap GetObstacleSprite(Obstacle.Type type, int width, int height)
        {
            // Floor tiles need tiling
            if (type == Obstacle.Type.Grass || type == Obstacle.Type.Stone || type == Obstacle.Type.Sand)
            {
                return _tiledFloors.GetOrAdd((type, width, height), _ =>
                {
                    Bitmap baseSprite = GetBaseObstacle(type);
                    return TileSprite(baseSprite, width, height);
                });
            }

            return GetBaseObstacle(type);
        }

        private static Bitmap RotateSprite(Bitmap original, float angle)
        {
            Bitmap rotated = new Bitmap(original.Width, original.Height);
            rotated.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotated))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.TranslateTransform(original.Width / 2f, original.Height / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-original.Width / 2f, -original.Height / 2f);

                g.DrawImage(original, 0, 0);
            }

            return rotated;
        }

        public static Bitmap GetProjectileSprite(Projectile.Type type, float angle)
        {
            // some projectiles do not rotate
            if (type == Projectile.Type.WitherSkull || type == Projectile.Type.DragonFireball)
                return GetBaseProjectile(type);

            int keyAngle = (int)Math.Round(angle / 5f) * 5; // cache every 5 degrees
            return _rotatedProjectiles.GetOrAdd((type, keyAngle), _ =>
            {
                Bitmap baseSprite = GetBaseProjectile(type);
                return RotateSprite(baseSprite, keyAngle);
            });
        }

        /// <summary>
        /// Recolor a gray texture
        /// </summary>
        /// <param name="gray">Default gray image</param>
        /// <param name="tint">The tint</param>
        /// <returns>The same image, but recolored</returns>
        private static Bitmap ApplyTint(Bitmap gray, Color tint)
        {
            Bitmap result = new Bitmap(gray.Width, gray.Height);

            for (int x = 0; x < gray.Width; x++)
            {
                for (int y = 0; y < gray.Height; y++)
                {
                    Color px = gray.GetPixel(x, y);

                    // Multiply the grayscale pixel by the tint (Minecraft-style)
                    int r = (px.R * tint.R) / 255;
                    int g = (px.G * tint.G) / 255;
                    int b = (px.B * tint.B) / 255;

                    result.SetPixel(x, y, Color.FromArgb(px.A, r, g, b));
                }
            }

            return result;
        }

        public static Bitmap GetGrass(Color tint)
        {
            return _tintedFloors.GetOrAdd(tint, _ => ApplyTint(Grass, tint));
        }
    }
}
