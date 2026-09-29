using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Single source of truth for every tunable number in IRONHOLD: ENDLESS SIEGE.
    /// Code-first by design (a static class rather than a ScriptableObject) so the
    /// project has no fragile .asset files to author; tweak balance right here.
    /// All values come from the design brief (sections 5-9).
    /// </summary>
    public static class GameConfig
    {
        // ---- Plane / camera framing (3D-on-2D rule: all actors live at Z = 0) ----
        public const float PlayZ = 0f;                 // every gameplay actor is locked to this Z
        public const float LaneMinX = -13f;            // player movement clamp (left)
        public const float LaneMaxX = 13f;             // player movement clamp (right)
        public const float SpawnLeftX = -16f;          // off-screen gate edge
        public const float SpawnRightX = 16f;          // off-screen breach edge
        public const float CharacterHeight = 1.8f;     // GLBs are normalised to this height on load

        // ---- Player ----
        public const float PlayerMaxHP = 100f;
        public const float PlayerMoveSpeed = 4.5f;     // metres / second
        public const float PlayerMaxStamina = 100f;
        public const float StaminaRegenPerSec = 20f;
        public const float StaminaRegenDelay = 0.8f;   // seconds after last action before regen resumes
        public const float PlayerInvulnTime = 0.5f;    // i-frames after taking a hit
        public const float PlayerDeathDelay = 1.2f;    // seconds from death to Game Over screen
        public const float LowHpThreshold = 20f;       // announcer "you're bleeding" trigger

        // ---- PUNCH (light melee) ----
        public const float PunchReach = 1.1f;
        public const float PunchDamage = 8f;
        public const float PunchStamina = 6f;
        public const float PunchStartup = 0.08f;
        public const float PunchActive = 0.10f;
        public const float PunchRecovery = 0.22f;
        public const float PunchKnockback = 0.4f;

        // ---- SWORD (heavy melee) ----
        public const float SwordReach = 1.8f;
        public const float SwordDamage = 26f;
        public const float SwordStamina = 18f;
        public const float SwordStartup = 0.22f;
        public const float SwordActive = 0.14f;
        public const float SwordRecovery = 0.45f;
        public const float SwordKnockback = 1.2f;
        public const int SwordMaxTargets = 3;          // cleave

        // ---- BLOCK (stance) ----
        public const float BlockDamageMultiplier = 0.2f;   // 80% reduction from the front
        public const float BlockStaminaPerSec = 4f;        // drain while held
        public const float BlockHitStaminaCost = 10f;      // extra cost per blocked hit
        public const float GuardBreakStun = 0.9f;          // stun when stamina hits 0 while blocking
        public const float BlockFrontDot = 0f;             // attacker must be in front half (dot >= 0)

        public const float HitboxHalfHeight = 0.9f;        // OverlapBox vertical half-extent
        public const float HitboxHalfWidth = 0.6f;         // OverlapBox depth (Z) half-extent

        // ---- Combat interactivity (buffer, cancels, chain, dodge, knockdown) ----
        public const float InputBufferSeconds = 0.35f;     // any-phase input buffer lifetime
        public const float ChainCancelOnHitPct = 0.30f;    // recovery % where a LANDED attack cancels into the next
        public const float ChainCancelWhiffPct = 0.60f;    // recovery % on whiff (rewards hit-confirms)
        public const float DodgeCancelPct = 0.25f;         // recovery % where dodge may cancel
        public const float BlockCancelPct = 0.50f;         // recovery % where block may cancel
        public const float ChainResetAfter = 0.7f;         // chain memory after an attack ends
        public const float StaminaRefundPerHit = 2f;       // aggression economy: refund per landed hit
        public const float PlayerHitstun = 0.25f;          // real hitstun when hurt during Startup

        public const float DodgeDuration = 0.42f;
        public const float DodgeDistance = 2.6f;
        public const float DodgeIframeStart = 0.06f;       // seconds into the roll
        public const float DodgeIframeEnd = 0.30f;
        public const float DodgeStamina = 12f;
        public const float DodgeCooldown = 0.15f;
        public const float DodgeAttackCancelPct = 0.80f;   // roll % where attack may cancel

        public const float KnockdownDuration = 0.9f;       // lying time (one OTG hit allowed)
        public const float GetupDuration = 0.35f;          // invulnerable while getting up
        public const float OTGDamageMult = 0.5f;
        public const float LaunchPopVelocity = 5.5f;       // vertical m/s on Launch hits

        // ---- Parry (block pressed just before impact) ----
        public const float ParryWindow = 0.15f;            // seconds between block press and the hit
        public const float ParryStagger = 1.2f;            // attacker stagger on parry
        public const float ParryStaggerBrute = 0.6f;

        // ---- Brute charge ----
        public const float ChargeMinDistance = 5f;         // only from far away
        public const float ChargeTelegraph = 0.8f;
        public const float ChargeSpeed = 6f;
        public const float ChargeMaxDuration = 2.5f;
        public const float ChargeDamage = 22f;
        public const float ChargeSelfStagger = 1.5f;       // blocked charge = big punish window
        public const float ChargeCooldown = 6f;

        // ---- Enemy behavior flavor ----
        public const float SkeletonLungeWindow = 0.15f;    // last part of windup lunges forward
        public const float SkeletonLungeSpeed = 8f;
        public const float GruntFeintChance = 0.2f;        // cancels windup at 70%, re-attacks
        public const float GruntFeintReattack = 0.5f;

        // ---- Elites + wave modifiers ----
        public const int EliteFirstWave = 8;
        public const float EliteChance = 0.10f;
        public const float EliteHpMult = 1.6f;
        public const float EliteDamageMult = 1.25f;
        public const float EliteScale = 1.15f;
        public const int FrenzyWaveEvery = 5;              // every 5th wave: faster, squishier
        public const float FrenzySpeedMult = 1.2f;
        public const float FrenzyHpMult = 0.8f;

        // ---- Style meter (score fighting, not just kills) ----
        public const float StylePerHit = 1f;
        public const float StylePerKill = 3f;
        public const float StylePerParry = 2f;
        public const float StyleDecayWindow = 2.5f;        // refreshed by any style event
        public const float StyleDrainPerSec = 10f;         // drain rate once the window lapses
        public static readonly float[] StyleTierThresholds = { 5f, 12f, 22f, 35f }; // D->C/B/A/S
        public static readonly string[] StyleTierNames = { "D", "C", "B", "A", "S" };

        // ---- Enemy coordination (attack tokens) ----
        public const int MaxAttackTokens = 2;              // simultaneous attackers (Brute costs 2)
        public const int MaxAttackTokensLate = 3;
        public const int TokenEscalationWave = 10;
        public const float TokenCooldown = 1.0f;           // per-enemy wait after releasing a token
        public const float WindupStaggerGap = 0.4f;        // min gap between two windup starts
        public const float RingDistance = 1.2f;            // hold distance past AttackRange when tokenless
        public const float TelegraphFlashWindow = 0.25f;   // ember pulses in the last part of windup

        // ---- Feel: hit-stop ----
        public const float HitstopScale = 0.05f;           // timeScale during a stop
        public const float HitstopLight = 0.045f;
        public const float HitstopHeavy = 0.085f;
        public const float HitstopKill = 0.11f;
        public const float HitstopGuardBreak = 0.14f;
        public const float HitstopMax = 0.16f;             // never freeze longer than this

        // ---- Feel: trauma camera ----
        public const float TraumaLightHit = 0.20f;
        public const float TraumaHeavyHit = 0.35f;
        public const float TraumaKill = 0.25f;
        public const float TraumaPlayerHurt = 0.45f;
        public const float TraumaGuardBreak = 0.55f;
        public const float ShakeMaxOffset = 0.25f;         // metres at trauma 1
        public const float ShakeMaxRoll = 1.2f;            // degrees at trauma 1
        public const float ShakeFreq = 25f;
        public const float TraumaDecayPerSec = 1.5f;
        public const float CameraLookAhead = 1.2f;         // metres ahead of facing
        public const float CameraLookAheadSmooth = 0.4f;
        public const float KillPunchInZ = 1.5f;            // dolly toward the action on kills
        public const float KillPunchOutTime = 0.4f;

        // ---- Feel: flashes, knockback impulses, pose smoothing ----
        public const float HitFlashTime = 0.09f;
        public const float KnockbackImpulseScale = 6f;     // knockback metres -> initial m/s
        public const float KnockbackDamping = 8f;          // v *= exp(-damping * dt)
        public const float KnockbackUpPop = 2.0f;          // extra vertical m/s on heavy hits
        public const float PoseSmoothPosK = 12f;           // exp smoothing (framerate independent)
        public const float PoseSmoothRotK = 10f;

        // ---- Scoring (section 9) ----
        public const float ComboDecaySeconds = 3.0f;       // no-kill window before combo resets
        public const int WaveClearBonusPerWave = 200;      // 200 * waveNumber
        public const string BestScoreKey = "IRONHOLD_BEST_SCORE";

        /// <summary>Combo multiplier tiers: index by combo count. x1/x2/x3/x4/x5.</summary>
        public static int ComboMultiplier(int combo)
        {
            if (combo >= 15) return 5;
            if (combo >= 10) return 4;
            if (combo >= 6) return 3;
            if (combo >= 3) return 2;
            return 1;
        }

        // ---- Waves (section 8) ----
        public const int MaxAliveHardCap = 14;             // lower to 8-10 for weak devices
        public const float WaveBreather = 2.5f;            // seconds between waves
        public const int BruteFirstWave = 4;

        public static int EnemiesInWave(int w) => 4 + Mathf.FloorToInt(w * 1.5f);
        public static int ConcurrentAlive(int w) => Mathf.Min(6 + w, MaxAliveHardCap);
        public static float SpawnInterval(int w) => Mathf.Max(0.45f, 1.6f - 0.08f * w);

        /// <summary>HP / damage scalar applied from wave 5 onward (speed is never scaled).</summary>
        public static float WaveStatScale(int w) => w < 5 ? 1f : 1f + 0.05f * (w - 4);

        // ---- Boss (a lone boss wave after every 3 regular waves: waves 4, 8, 12...) ----
        public const int BossWaveEvery = 4;
        public static bool IsBossWave(int w) => w > 0 && w % BossWaveEvery == 0;
        // HP is NOT wave-scaled so the speed-kill challenge is equally fair on every boss wave.
        // Sustained optimal DPS (chained uppercuts / jab->finisher, stamina-limited) is ~36-45,
        // so a flawless player lands it in ~7-9s; casual, unhurried pressing takes 12s+.
        public const float BossMaxHP = 320f;
        public const float BossDamage = 26f;           // wave-scaled like everything else
        public const float BossVisualScale = 1.45f;
        public const int BossPoints = 1500;

        // ---- Easter eggs: fanbase endpoints (promo popups for the artist) ----
        public const string PromoPatreonUrl = "https://www.patreon.com/";
        public const string PromoMerchUrl = "https://www.shopify.com/";
        public const string PromoComicUrl = "https://www.marvel.com/comics/issue/116194/moon_knight_fist_of_khonshu_2024";

        public const float BossSpeedKillSeconds = 10f;   // fight clock starts on the boss's first hit
        public const float PromoRevealDelay = 1.2f;      // unscaled beat between the trigger and the popup
        public const float SecretComboMaxGap = 0.8f;     // seconds allowed between two combo presses

        public enum ComboInput { Left, Right, Punch, Sword, Block, Roll }

        /// <summary>The secret 8-press combo: tap-tap back and forth, double guard, roll, sword.</summary>
        public static readonly ComboInput[] SecretCombo =
        {
            ComboInput.Left, ComboInput.Right, ComboInput.Left, ComboInput.Right,
            ComboInput.Block, ComboInput.Block, ComboInput.Roll, ComboInput.Sword,
        };

        // ---- Easter egg: hidden chest (tucked behind the barrel/crate pile on the right) ----
        public static readonly Vector3 ChestPosition = new Vector3(10.5f, 0f, 2.35f);
        public const float ChestHeight = 0.75f;
        public const float ChestYaw = 0f;                // flip by 180 if the model faces the wall
        public const float ChestInteractRange = 0.7f;    // |player.x - chest.x| to swap PUNCH -> OPEN
        public static readonly Color ChestGold = new Color(1f, 0.78f, 0.28f);

        // ---- Locked palette (section 11) ----
        public static readonly Color StoneGrey = new Color(0.431f, 0.416f, 0.388f);   // #6E6A63
        public static readonly Color Iron = new Color(0.290f, 0.306f, 0.341f);        // #4A4E57
        public static readonly Color EmberOrange = new Color(0.910f, 0.454f, 0.231f); // #E8743B
        public static readonly Color NightBlue = new Color(0.106f, 0.133f, 0.200f);   // #1B2233
        public static readonly Color Bone = new Color(0.847f, 0.812f, 0.753f);        // #D8CFC0
        public static readonly Color Leather = new Color(0.478f, 0.294f, 0.180f);     // #7A4B2E

        // ---- Resource paths (everything the code loads at runtime) ----
        public const string TexFloor = "Art/Textures/Floor_Flagstone";
        public const string TexWall = "Art/Textures/Wall_Stone";
        public const string TexSkybox = "Art/Textures/Skybox_Night";
        public const string TexButton = "Art/UI/Button_Round";
        public const string TexPanel = "Art/UI/Panel_Stone";
        public const string TexTitle = "Art/UI/Menu/TitleImage_16x9";
        public const string TexIcon = "Art/Icon/AppIcon_1024";
        public const string VoFolder = "Audio/VO/";
        public const string SfxFolder = "Audio/SFX/";
        public const string MusicFolder = "Audio/Music/";

        // ---- GLB model file names (in StreamingAssets/Models, loaded by GlbLoader) ----
        public const string ModelHero = "Hero";
        public const string ModelGrunt = "Grunt";
        public const string ModelSkeleton = "Skeleton";
        public const string ModelBrute = "Brute";
        public const string ModelCrate = "Crate";
        public const string ModelBarrel = "Barrel";
        public const string ModelBrazier = "Brazier";
        public const string ModelChest = "Chest";           // "Chest Closed" by Quaternius (CC0, poly.pizza)
        public const string ModelChestOpen = "ChestOpen";   // "Chest Gold" by Quaternius (CC0, poly.pizza)

        // ================= Attack definitions (Phase 2 combat) =================

        /// <summary>One attack's full tuning. Timings stay AUTHORITATIVE over clips:
        /// the skeletal layer warps clip time so the contact frame lands at Active start.</summary>
        public struct AttackDef
        {
            public string Name;
            public float Startup, Active, Recovery;   // seconds
            public float Damage, Reach, Knockback, Stamina;
            public int MaxTargets;
            public HitType Hit;
            public string ClipKey;                    // ActorAnimator clip registry key
            public bool IsHeavyFx;                    // heavy-tier hit-stop / trauma / sfx

            public AttackDef(string name, float startup, float active, float recovery,
                float damage, float reach, float knockback, float stamina,
                int maxTargets, HitType hit, string clipKey, bool isHeavyFx)
            {
                Name = name; Startup = startup; Active = active; Recovery = recovery;
                Damage = damage; Reach = reach; Knockback = knockback; Stamina = stamina;
                MaxTargets = maxTargets; Hit = hit; ClipKey = clipKey; IsHeavyFx = isHeavyFx;
            }
        }

        /// <summary>3-hit light chain: jab -> hook -> uppercut (launcher).</summary>
        public static readonly AttackDef[] HeroLightChain =
        {
            new AttackDef("Jab",      0.10f, 0.08f, 0.26f,  6f, 1.10f, 0.3f, 4f, 1, HitType.Light,     "Punch1", false),
            new AttackDef("Hook",     0.12f, 0.08f, 0.30f,  8f, 1.15f, 0.5f, 4f, 1, HitType.Light,     "Punch2", false),
            new AttackDef("Uppercut", 0.16f, 0.10f, 0.42f, 14f, 1.20f, 1.0f, 6f, 1, HitType.Launch,    "Punch3", true),
        };

        /// <summary>Raw sword press (neutral) — the original heavy, unchanged balance.</summary>
        public static readonly AttackDef HeroSword =
            new AttackDef("Sword", 0.22f, 0.14f, 0.45f, 26f, 1.8f, 1.2f, 18f, 3, HitType.Heavy, "Sword", true);

        /// <summary>Sword pressed inside a light's cancel window — faster windup, knocks down.</summary>
        public static readonly AttackDef HeroSwordFinisher =
            new AttackDef("SwordFinisher", 0.15f, 0.14f, 0.45f, 30f, 1.8f, 1.6f, 18f, 3, HitType.Knockdown, "Sword", true);

        // ================= Skeletal clip registry (Phase 1 animation) =================

        /// <summary>
        /// One skeletal clip: registry key + source GLB in StreamingAssets/Models/Anim
        /// (File == null means "use the base model's baked clip"). ActiveStart/EndNorm map the
        /// clip's visual contact window onto the attack's Active phase (measure with the
        /// debug clip cycler, then hard-code here — the code-first version of anim events).
        /// Higgsfield animation_action_ids used per file are noted inline.
        /// </summary>
        public struct ClipDef
        {
            public string Key;
            public string File;
            public WrapMode Wrap;
            public float Fade;              // default crossfade seconds
            public float ActiveStartNorm;   // normalized time where the hit lands
            public float ActiveEndNorm;

            public ClipDef(string key, string file, WrapMode wrap, float fade,
                float activeStartNorm = 0.40f, float activeEndNorm = 0.60f)
            {
                Key = key; File = file; Wrap = wrap; Fade = fade;
                ActiveStartNorm = activeStartNorm; ActiveEndNorm = activeEndNorm;
            }
        }

        public const string AnimSubFolder = "Anim";   // under StreamingAssets/Models

        public static readonly ClipDef[] HeroClips =
        {
            new ClipDef("Idle",      "Hero_Idle",      WrapMode.Loop,         0.20f), // 89 Combat_Stance
            new ClipDef("Walk",      "Hero_Walk",      WrapMode.Loop,         0.15f), // 21 Walk_Fight_Forward
            new ClipDef("Punch1",    "Hero_Punch1",    WrapMode.Once,         0.03f, 0.35f, 0.55f), // 191 Left_Jab_from_Guard
            new ClipDef("Punch2",    "Hero_Punch2",    WrapMode.Once,         0.03f, 0.35f, 0.55f), // 193 Left_Hook_from_Guard
            new ClipDef("Punch3",    "Hero_Punch3",    WrapMode.Once,         0.03f, 0.40f, 0.60f), // 194 Right_Uppercut_from_Guard
            new ClipDef("Sword",     "Hero_Sword",     WrapMode.Once,         0.03f, 0.40f, 0.60f), // 102 Sword_Judgment
            new ClipDef("Block",     "Hero_Block",     WrapMode.Loop,         0.08f), // 138 Block1
            new ClipDef("Roll",      "Hero_Roll",      WrapMode.Once,         0.05f), // 158 Roll_Dodge
            new ClipDef("Hit",       "Hero_Hit",       WrapMode.Once,         0.03f), // 178 Hit_Reaction
            new ClipDef("Knockdown", "Hero_Knockdown", WrapMode.ClampForever, 0.05f), // 187 Knock_Down
            new ClipDef("Die",       "Hero_Die",       WrapMode.ClampForever, 0.05f), // 8 Dead
        };

        public static readonly ClipDef[] GruntClips =
        {
            new ClipDef("Idle",   null,          WrapMode.Loop,         0.20f), // baked idle in Grunt.glb
            new ClipDef("Walk",   "Grunt_Walk",  WrapMode.Loop,         0.15f), // 630 ForwardLeft_Run_Fight_inplace
            new ClipDef("Attack", "Grunt_Attack",WrapMode.Once,         0.03f, 0.45f, 0.60f), // 90 Counterstrike
            new ClipDef("Hit",    "Grunt_Hit",   WrapMode.Once,         0.03f), // 173 Slap_Reaction
            new ClipDef("Die",    "Grunt_Die",   WrapMode.ClampForever, 0.05f), // 190 Knock_Down_1
        };

        public static readonly ClipDef[] SkeletonClips =
        {
            new ClipDef("Idle",   null,             WrapMode.Loop,         0.20f),
            new ClipDef("Walk",   "Skeleton_Walk",  WrapMode.Loop,         0.15f), // 112 Monster_Walk
            new ClipDef("Attack", "Skeleton_Attack",WrapMode.Once,         0.03f, 0.45f, 0.60f), // 97 Left_Slash
            new ClipDef("Hit",    "Skeleton_Hit",   WrapMode.Once,         0.03f), // 172 Electrocution_Reaction
            new ClipDef("Die",    "Skeleton_Die",   WrapMode.ClampForever, 0.05f), // 188 Fall_Dead_from_Abdominal_Injury
        };

        public static readonly ClipDef[] BruteClips =
        {
            new ClipDef("Idle",   null,           WrapMode.Loop,         0.20f),
            new ClipDef("Walk",   "Brute_Walk",   WrapMode.Loop,         0.15f), // 119 Slow_Orc_Walk
            new ClipDef("Attack", "Brute_Attack", WrapMode.Once,         0.03f, 0.50f, 0.65f), // 128 Heavy_Hammer_Swing
            new ClipDef("Hit",    "Brute_Hit",    WrapMode.Once,         0.03f), // 171 Hit_Reaction_to_Waist
            new ClipDef("Die",    "Brute_Die",    WrapMode.ClampForever, 0.05f), // 181 Electrocuted_Fall
            new ClipDef("Charge", "Brute_Charge", WrapMode.Loop,         0.10f), // 512 Male_Head_Down_Charge
        };
    }
}
