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
    }
}
