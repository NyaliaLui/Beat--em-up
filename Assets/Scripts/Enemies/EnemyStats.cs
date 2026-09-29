using UnityEngine;

namespace Ironhold
{
    public enum EnemyType { Grunt, Skeleton, Brute, Boss }

    /// <summary>
    /// Per-type enemy tuning (section 7). A plain code-first class (no ScriptableObject .asset to
    /// author); For() returns a copy scaled by the wave's HP/damage multiplier.
    /// </summary>
    public class EnemyStats
    {
        public EnemyType Type;
        public float MaxHP;
        public float Speed;          // m/s (never wave-scaled)
        public float AttackDamage;
        public float AttackRange;    // m
        public float AttackCadence;  // seconds between attacks
        public float WindUp;         // telegraph before the strike lands
        public int Points;
        public float StaggerTime;
        public bool StaggersFromLight; // PUNCH staggers Grunt/Skeleton but not the Brute
        public string Model;
        public Color FallbackColor;
        public int TokenCost;          // AttackDirector permission cost (Brute hogs 2)
        public bool IsElite;           // bigger, tougher, glowing, double points
        public bool IsBoss => Type == EnemyType.Boss;
        /// <summary>Brute and Boss share the heavy archetype: hyper-armor, charge, never floored.</summary>
        public bool IsBruteLike => Type == EnemyType.Brute || Type == EnemyType.Boss;
        public GameConfig.ClipDef[] ClipSet; // skeletal clips for this archetype

        /// <summary>Promote to an elite variant (wave 8+, ~10% of spawns).</summary>
        public EnemyStats MakeElite()
        {
            IsElite = true;
            MaxHP *= GameConfig.EliteHpMult;
            AttackDamage *= GameConfig.EliteDamageMult;
            Points *= 2;
            return this;
        }

        /// <summary>FRENZY wave modifier: faster but squishier.</summary>
        public EnemyStats ApplyFrenzy()
        {
            Speed *= GameConfig.FrenzySpeedMult;
            MaxHP *= GameConfig.FrenzyHpMult;
            return this;
        }

        public static EnemyStats For(EnemyType type, float scale)
        {
            switch (type)
            {
                case EnemyType.Skeleton:
                    return new EnemyStats
                    {
                        Type = type, MaxHP = 18f * scale, Speed = 3.0f, AttackDamage = 7f * scale,
                        // 0.45s windup: 0.35 is unreadable on a phone (cadence unchanged, so DPS holds)
                        AttackRange = 1.0f, AttackCadence = 1.0f, WindUp = 0.45f, Points = 120,
                        StaggerTime = 0.25f, StaggersFromLight = true, TokenCost = 1,
                        Model = GameConfig.ModelSkeleton, FallbackColor = GameConfig.Bone,
                        ClipSet = GameConfig.SkeletonClips
                    };
                case EnemyType.Brute:
                    return new EnemyStats
                    {
                        Type = type, MaxHP = 90f * scale, Speed = 1.5f, AttackDamage = 22f * scale,
                        AttackRange = 1.5f, AttackCadence = 2.0f, WindUp = 0.6f, Points = 300,
                        StaggerTime = 0.3f, StaggersFromLight = false, TokenCost = 2,
                        Model = GameConfig.ModelBrute, FallbackColor = new Color(0.32f, 0.40f, 0.30f),
                        ClipSet = GameConfig.BruteClips
                    };
                case EnemyType.Boss:
                    // A war-chief Brute: same rig and clips, bigger, hits harder, fixed HP.
                    return new EnemyStats
                    {
                        Type = type, MaxHP = GameConfig.BossMaxHP, Speed = 1.8f, AttackDamage = GameConfig.BossDamage * scale,
                        AttackRange = 1.9f, AttackCadence = 1.8f, WindUp = 0.6f, Points = GameConfig.BossPoints,
                        StaggerTime = 0.3f, StaggersFromLight = false, TokenCost = 2,
                        Model = GameConfig.ModelBrute, FallbackColor = new Color(0.45f, 0.16f, 0.14f),
                        ClipSet = GameConfig.BruteClips
                    };
                default: // Grunt
                    return new EnemyStats
                    {
                        Type = EnemyType.Grunt, MaxHP = 30f * scale, Speed = 2.2f, AttackDamage = 10f * scale,
                        AttackRange = 1.2f, AttackCadence = 1.4f, WindUp = 0.5f, Points = 100,
                        StaggerTime = 0.3f, StaggersFromLight = true, TokenCost = 1,
                        Model = GameConfig.ModelGrunt, FallbackColor = new Color(0.38f, 0.46f, 0.34f),
                        ClipSet = GameConfig.GruntClips
                    };
            }
        }

        public string DeathSfxKey => Type switch
        {
            EnemyType.Skeleton => SfxManager.EnemyDieSkeleton,
            EnemyType.Brute or EnemyType.Boss => SfxManager.EnemyDieBrute,
            _ => SfxManager.EnemyDieGrunt
        };
    }
}
