using ShootMeUp.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootMeUp.Model
{
    static class Sprites
    {
        // Characters
        public static readonly Bitmap Player            = Resources.Character_Player;
        public static readonly Bitmap Zombie            = Resources.Character_Zombie;
        public static readonly Bitmap Skeleton          = Resources.Character_Skeleton;
        public static readonly Bitmap Pigman            = Resources.Character_Zombie_Pigman;
        public static readonly Bitmap BabyZombie        = Resources.Character_Zombie;
        public static readonly Bitmap Blaze             = Resources.Character_Blaze;
        public static readonly Bitmap SpiderJockey      = Resources.Character_SpiderJockey;
        public static readonly Bitmap Dragon            = Resources.Character_Dragon;
        public static readonly Bitmap Wither            = Resources.Character_Wither;

        // Projectiles
        public static readonly Bitmap Arrow             = Resources.Projectile_Arrow;
        public static readonly Bitmap Fireball          = Resources.Projectile_Fireball;
        public static readonly Bitmap WitherSkull       = Resources.Projectile_WitherSkull;
        public static readonly Bitmap DragonFireball    = Resources.Projectile_DragonFireball;

        // Obstacles
        public static readonly Bitmap Dirt              = Resources.Obstacle_Dirt;
        public static readonly Bitmap Wooden_Planks     = Resources.Obstacle_Wood_Planks;
        public static readonly Bitmap Cobblestone       = Resources.Obstacle_Cobblestone;
        public static readonly Bitmap Spawner           = Resources.Obstacle_Spawner;
        public static readonly Bitmap Bedrock           = Resources.Obstacle_Bedrock;
        public static readonly Bitmap Barrier           = Resources.Obstacle_Barrier;
        public static readonly Bitmap Bush              = Resources.Obstacle_Bush;

        // Floor
        public static readonly Bitmap Grass             = Resources.Floor_Grass;
        public static readonly Bitmap Stone             = Resources.Floor_Stone;
        public static readonly Bitmap Sand              = Resources.Floor_Sand;

        // Misc
        public static readonly Bitmap PlayerHeart       = Resources.Misc_PlayerHeart;
        public static readonly Bitmap EmptyHeart        = Resources.Misc_EmptyHeart;
        public static readonly Bitmap EnemyHeart        = Resources.Misc_EnemyHeart;
        public static readonly Bitmap BossHeart         = Resources.Misc_BossHeart;
    }
}
