using UnityEngine;

namespace Ironhold
{
    /// <summary>Enemy hit points + damage entry point. Delegates reaction/death to EnemyBase.</summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        public Faction Faction => Faction.Enemy;
        public bool IsAlive { get; private set; }
        public Transform Transform => transform;
        public float HP => _hp;
        public float Max { get; private set; }
        public float Normalized => Max > 0f ? Mathf.Clamp01(_hp / Max) : 0f;
        /// <summary>Scaled time of the first landed hit (-1 until hit) - the boss fight clock.</summary>
        public float FirstHitTime { get; private set; } = -1f;

        private EnemyBase _base;
        private float _hp;

        public void Init(float maxHp)
        {
            _hp = maxHp;
            Max = maxHp;
            FirstHitTime = -1f;
            IsAlive = true;
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || info.Attacker == Faction.Enemy) return;
            if (_base == null) _base = GetComponent<EnemyBase>();

            float amount = info.Amount;
            if (_base != null && _base.IsKnockedDown) amount *= GameConfig.OTGDamageMult; // on-the-ground hit

            if (FirstHitTime < 0f) FirstHitTime = Time.time;
            _hp -= amount;
            DamageNumbers.Damage(transform.position + Vector3.up * 1.7f, amount, info.IsHeavy);
            bool stillAlive = _hp > 0f;
            _base?.ReactToHit(info, stillAlive);

            if (!stillAlive)
            {
                IsAlive = false;
                _base?.OnKilled(info);
            }
        }
    }
}
