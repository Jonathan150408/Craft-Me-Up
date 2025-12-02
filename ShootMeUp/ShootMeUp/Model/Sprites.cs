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
        public static readonly Bitmap Player        = Resources.CharacterPlayer;
        public static readonly Bitmap Zombie        = Resources.EnemyZombie;
        public static readonly Bitmap Skeleton      = Resources.EnemySkeleton;
        public static readonly Bitmap Pigman        = Resources.EnemyZombiePigman;
        public static readonly Bitmap BabyZombie    = Resources.EnemyZombie;
        public static readonly Bitmap Blaze         = Resources.EnemyBlaze;

        // Projectiles
        public static readonly Bitmap Arrow     = Resources.ProjectileArrow;
        public static readonly Bitmap Fireball  = Resources.ProjectileFireball;

        // Obstacles
        public static readonly Bitmap Dirt = Resources.ObstacleWeak;
        public static readonly Bitmap Wooden_Planks = Resources.ObstacleNormal;
        public static readonly Bitmap Cobblestone   = Resources.ObstacleStrong;
        public static readonly Bitmap Spawner       = Resources.ObstacleSpawner;
        public static readonly Bitmap Bedrock       = Resources.ObstacleUnbreakable;
        public static readonly Bitmap Barrier       = Resources.ObstacleBorder;
        public static readonly Bitmap Bush          = Resources.ObstacleBush;

        // Floor
        public static readonly Bitmap Grass = Resources.FloorGrass;
        public static readonly Bitmap Stone = Resources.FloorStone;
        public static readonly Bitmap Sand  = Resources.FloorSand;
    }
}
