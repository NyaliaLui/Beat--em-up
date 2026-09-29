using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Builds an enemy GameObject in code: CharacterController + EnemyHealth + EnemyBase,
    /// configured and initialised. No prefabs required.
    /// </summary>
    public static class Spawner
    {
        public static EnemyBase Spawn(EnemyStats stats, Transform player, float spawnX, WaveManager waves)
        {
            var go = new GameObject("Enemy_" + stats.Type);

            var cc = go.AddComponent<CharacterController>();
            cc.height = GameConfig.CharacterHeight;
            cc.radius = stats.IsBoss ? 0.55f : 0.4f;
            cc.center = new Vector3(0f, GameConfig.CharacterHeight * 0.5f, 0f);
            cc.slopeLimit = 60f;
            cc.stepOffset = 0.3f;

            go.AddComponent<EnemyHealth>();
            var eb = go.AddComponent<EnemyBase>();
            eb.Init(stats, waves, player, spawnX);
            return eb;
        }
    }
}
