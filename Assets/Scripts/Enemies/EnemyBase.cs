using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// One class drives all three enemy types (data lives in EnemyStats). FSM:
    /// Approach -> WindUp -> strike -> Cooldown, plus Stagger, Launched / Knockdown / Getup
    /// and Dead. Attack permission comes from the AttackDirector token system so only a
    /// couple of enemies swing at once while the rest hold a shuffling ring just out of
    /// range. Wind-ups telegraph with ember flashes; hits knock back with a decaying
    /// impulse; light enemies can be launched or floored. Animates through the skeletal
    /// ActorAnimator when the Higgsfield clips are bound, procedurally otherwise.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class EnemyBase : MonoBehaviour
    {
        private enum State { Approach, WindUp, Cooldown, Stagger, Launched, Knockdown, Getup, ChargeTelegraph, Charging, Dead }

        public float FacingYOffset = 0f;

        private EnemyStats _stats;
        private WaveManager _waves;
        private Transform _player;
        private CharacterController _cc;
        private EnemyHealth _health;
        private Transform _visual;
        private HitFlash _flash;
        private ActorAnimator _skeletal;

        private State _state = State.Approach;
        private float _phaseT;
        private float _phaseDuration;
        private float _attackCd;
        private float _staggerT;
        private float _hitFlinch;
        private bool _hitRetrigger;
        private float _deathT;
        private bool _deathClipStarted;
        private float _vY;
        private float _kbVel;
        private float _facing = -1f;
        private bool _moving;
        private bool _notifiedDeath;
        private bool _hasToken;
        private float _tokenCd;
        private float _ringSeed;
        private float _chargeCd;
        private bool _feint;
        private CombatSystem _playerCombat;

        public bool IsAlive => _health != null && _health.IsAlive;
        public bool IsKnockedDown => _state == State.Knockdown;
        public bool IsBoss => _stats != null && _stats.IsBoss;
        public EnemyHealth Health => _health;

        private static int s_SeedCounter;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _ringSeed = (s_SeedCounter++ & 0xFF) * 2.39f; // decorrelate ring shuffle between enemies
        }

        public void Init(EnemyStats stats, WaveManager waves, Transform player, float spawnX)
        {
            _stats = stats;
            _waves = waves;
            _player = player;
            _health = GetComponent<EnemyHealth>();
            _health.Init(stats.MaxHP);

            _state = State.Approach;
            _attackCd = 0f;
            _chargeCd = GameConfig.ChargeCooldown * 0.5f; // never charge straight off the spawn gate
            _facing = (player != null && player.position.x < spawnX) ? -1f : 1f;
            _playerCombat = player != null ? player.GetComponent<CombatSystem>() : null;
            TeleportTo(new Vector3(spawnX, 0.1f, GameConfig.PlayZ));

            // Visual loads asynchronously; the enemy is fully functional before it appears.
            AttachAndStore(stats);
        }

        private async void AttachAndStore(EnemyStats stats)
        {
            _visual = await GlbLoader.AttachVisual(stats.Model, transform, GameConfig.CharacterHeight, stats.FallbackColor);
            if (this == null || _visual == null) return;

            if (stats.IsBoss)
            {
                // The boss towers over the lane and glows blood-red.
                _visual.localScale *= GameConfig.BossVisualScale;
                var glowGo = new GameObject("BossGlow");
                glowGo.transform.SetParent(transform, false);
                glowGo.transform.localPosition = new Vector3(0f, 1.8f, -0.6f);
                var glow = glowGo.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = new Color(1f, 0.2f, 0.12f);
                glow.range = 6f;
                glow.intensity = 2.4f;
            }
            else if (stats.IsElite)
            {
                // Elites read at a glance: bigger silhouette + an ember glow.
                _visual.localScale *= GameConfig.EliteScale;
                var glowGo = new GameObject("EliteGlow");
                glowGo.transform.SetParent(transform, false);
                glowGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                var glow = glowGo.AddComponent<Light>();
                glow.type = LightType.Point;
                glow.color = GameConfig.EmberOrange;
                glow.range = 4f;
                glow.intensity = 1.6f;
            }

            if (_flash == null) _flash = gameObject.AddComponent<HitFlash>();
            _flash.Init(_visual);

            if (stats.ClipSet != null)
            {
                await AnimLibrary.PreloadSet(stats.ClipSet);
                if (this == null || _visual == null) return;
                var animator = new ActorAnimator();
                // All-or-nothing: a partially bound set would leave states invisible.
                if (animator.TryBind(_visual, stats.ClipSet) &&
                    animator.Has("Attack") && animator.Has("Hit") && animator.Has("Die"))
                {
                    _skeletal = animator;
                }
            }
        }

        private void TeleportTo(Vector3 pos)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = pos;
            if (_cc != null) _cc.enabled = true;
        }

        private void Update()
        {
            if (_stats == null) return;
            bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

            if (_state == State.Dead) { AnimateVisual(); return; }
            if (!playing) { ApplyGravity(0f); AnimateVisual(); return; }

            float dt = Time.deltaTime;
            if (_hitFlinch > 0f) _hitFlinch -= dt;
            if (_attackCd > 0f) _attackCd -= dt;
            if (_tokenCd > 0f) _tokenCd -= dt;
            if (_chargeCd > 0f) _chargeCd -= dt;

            float move = 0f;
            float dx = (_player != null ? _player.position.x : transform.position.x) - transform.position.x;
            float dist = Mathf.Abs(dx);
            bool canTurn = _state == State.Approach || _state == State.Cooldown;
            if (dist > 0.01f && canTurn) _facing = Mathf.Sign(dx);

            switch (_state)
            {
                case State.Approach:
                {
                    // Brutes far from the player open with a telegraphed head-down charge.
                    if (_stats.IsBruteLike && _chargeCd <= 0f && dist > GameConfig.ChargeMinDistance)
                    {
                        _chargeCd = GameConfig.ChargeCooldown;
                        EnterPhase(State.ChargeTelegraph, GameConfig.ChargeTelegraph);
                        GameManager.Instance?.Sfx?.Play(SfxManager.EnemyWindup, 1f);
                        break;
                    }

                    bool inRange = dist <= _stats.AttackRange;
                    bool wantsAttack = _attackCd <= 0f && _tokenCd <= 0f;
                    if (!_hasToken && wantsAttack && dist <= _stats.AttackRange + GameConfig.RingDistance + 1.2f)
                        _hasToken = AttackDirector.RequestToken(this, _stats.TokenCost);

                    if (_hasToken)
                    {
                        if (!inRange)
                        {
                            move = _facing * _stats.Speed;
                            _moving = true;
                        }
                        else
                        {
                            _moving = false;
                            if (_attackCd <= 0f)
                            {
                                EnterPhase(State.WindUp, _stats.WindUp);
                                _feint = _stats.Type == EnemyType.Grunt && Random.value < GameConfig.GruntFeintChance;
                                GameManager.Instance?.Sfx?.Play(SfxManager.EnemyWindup, 0.7f);
                            }
                        }
                    }
                    else
                    {
                        // No token: hold a loose shuffling ring just outside attack range.
                        float hold = _stats.AttackRange + GameConfig.RingDistance
                                     + Mathf.Sin(Time.time * 1.3f + _ringSeed) * 0.4f;
                        if (dist > hold + 0.1f) { move = _facing * _stats.Speed; _moving = true; }
                        else if (dist < hold - 0.5f) { move = -_facing * _stats.Speed * 0.5f; _moving = true; }
                        else _moving = false;
                    }
                    break;
                }

                case State.WindUp:
                    _moving = false;
                    _phaseT -= dt;

                    // Grunt feint: bail at 70% of the wind-up, hop back, come again quickly.
                    if (_feint && _phaseT <= _phaseDuration * 0.3f)
                    {
                        _feint = false;
                        _kbVel = -_facing * 3f;
                        EnterPhase(State.Cooldown, GameConfig.GruntFeintReattack);
                        _attackCd = GameConfig.GruntFeintReattack; // keeps its token, re-attacks fast
                        break;
                    }

                    // Skeleton lunge-step: closes 1.2 m during the very last part of the wind-up.
                    if (_stats.Type == EnemyType.Skeleton && _phaseT <= GameConfig.SkeletonLungeWindow)
                        move = _facing * GameConfig.SkeletonLungeSpeed;

                    // Ember telegraph pulses through the last part of the wind-up.
                    if (_phaseT <= GameConfig.TelegraphFlashWindow &&
                        Mathf.Repeat(Time.time, 1f / 6f) < 0.5f / 6f)
                    {
                        _flash?.TelegraphPulse();
                    }
                    if (_phaseT <= 0f)
                    {
                        DoStrike();
                        ReleaseTokenIfHeld();
                        EnterPhase(State.Cooldown, Mathf.Max(0.15f, _stats.AttackCadence - _stats.WindUp));
                        _attackCd = _stats.AttackCadence;
                    }
                    break;

                case State.ChargeTelegraph:
                    _moving = false;
                    _phaseT -= dt;
                    if (Mathf.Repeat(Time.time, 1f / 6f) < 0.5f / 6f) _flash?.TelegraphPulse();
                    if (_phaseT <= 0f) EnterPhase(State.Charging, GameConfig.ChargeMaxDuration);
                    break;

                case State.Charging:
                {
                    _moving = true;
                    _phaseT -= dt;
                    move = _facing * GameConfig.ChargeSpeed;

                    bool contact = dist <= 1.3f;
                    if (contact)
                    {
                        // A front block stonewalls the charge: no damage, Brute staggers wide open.
                        bool wall = _playerCombat != null && _playerCombat.IsBlocking &&
                                    Mathf.Approximately(Mathf.Sign(transform.position.x - _player.position.x),
                                        GameManager.Instance != null ? GameManager.Instance.Player.FacingSign : 1f);
                        if (wall)
                        {
                            HitSparks.Parry(_player.position + Vector3.up * 1.2f, -_facing);
                            GameManager.Instance?.Sfx?.Play(SfxManager.BlockImpact);
                            Impact.Trauma(0.35f);
                            _kbVel = -_facing * 4f;
                            _state = State.Stagger;
                            _staggerT = GameConfig.ChargeSelfStagger;
                        }
                        else
                        {
                            MeleeHitbox.ApplyMelee(transform.position, _facing, 1.4f,
                                GameConfig.ChargeDamage, 1.6f, HitType.Knockdown, Faction.Enemy, 1, this);
                            EnterPhase(State.Cooldown, 0.8f);
                            _attackCd = _stats.AttackCadence;
                        }
                    }
                    else if (_phaseT <= 0f)
                    {
                        EnterPhase(State.Cooldown, 0.6f); // whiffed into open air
                    }
                    break;
                }

                case State.Cooldown:
                    _moving = false;
                    _phaseT -= dt;
                    if (_phaseT <= 0f) _state = State.Approach;
                    break;

                case State.Stagger:
                    _moving = false;
                    _staggerT -= dt;
                    if (_staggerT <= 0f) _state = State.Approach;
                    break;

                case State.Launched:
                    _moving = false;
                    _phaseT -= dt;
                    if ((_vY <= 0f && _cc != null && _cc.isGrounded) || _phaseT <= 0f)
                        EnterPhase(State.Knockdown, GameConfig.KnockdownDuration);
                    break;

                case State.Knockdown:
                    _moving = false;
                    _phaseT -= dt;
                    if (_phaseT <= 0f) EnterPhase(State.Getup, GameConfig.GetupDuration);
                    break;

                case State.Getup:
                    _moving = false;
                    _phaseT -= dt;
                    if (_phaseT <= 0f) _state = State.Approach;
                    break;
            }

            // Decaying knockback impulse rides on top of whatever the FSM wants.
            move += _kbVel;
            _kbVel *= Mathf.Exp(-GameConfig.KnockbackDamping * dt);
            if (Mathf.Abs(_kbVel) < 0.01f) _kbVel = 0f;

            ApplyGravity(move);
            AnimateVisual();
        }

        private void EnterPhase(State s, float duration)
        {
            _state = s;
            _phaseDuration = Mathf.Max(0.0001f, duration);
            _phaseT = _phaseDuration;
        }

        private void ApplyGravity(float horizontal)
        {
            _vY += Physics.gravity.y * Time.deltaTime;
            if (_cc != null && _cc.isGrounded && _vY < 0f) _vY = -1f;
            if (_cc != null && _cc.enabled)
            {
                _cc.Move(new Vector3(horizontal * Time.deltaTime, _vY * Time.deltaTime, 0f));
                Vector3 p = transform.position;
                p.z = GameConfig.PlayZ;
                transform.position = p;
            }
        }

        private void DoStrike()
        {
            MeleeHitbox.ApplyMelee(
                transform.position, _facing, _stats.AttackRange + 0.2f, _stats.AttackDamage,
                0.3f, _stats.IsBruteLike ? HitType.Knockdown : HitType.Light,
                Faction.Enemy, 1, this);
        }

        /// <summary>The player parried this enemy's hit: wide-open punish stagger.</summary>
        public void ApplyParryStagger()
        {
            if (_state == State.Dead) return;
            ReleaseTokenIfHeld();
            _kbVel = -_facing * 2f;
            _state = State.Stagger;
            _staggerT = _stats.IsBruteLike ? GameConfig.ParryStaggerBrute : GameConfig.ParryStagger;
            _flash?.FlashWhite(0.15f);
        }

        private void ReleaseTokenIfHeld()
        {
            if (!_hasToken) return;
            _hasToken = false;
            _tokenCd = GameConfig.TokenCooldown;
            AttackDirector.ReleaseToken(this);
        }

        public void ReactToHit(in DamageInfo info, bool stillAlive)
        {
            // Knockback as a decaying impulse away from the attacker.
            float dir = Mathf.Sign(transform.position.x - info.SourcePos.x);
            if (dir == 0f) dir = -_facing;
            _kbVel = dir * info.Knockback * GameConfig.KnockbackImpulseScale;
            _hitFlinch = 0.15f;
            _hitRetrigger = true;
            _flash?.FlashWhite();
            HitSparks.Burst(transform.position + Vector3.up * 1.1f, dir, info.IsHeavy);

            if (!stillAlive) return;

            bool isBrute = _stats.IsBruteLike;
            // Hyper-armor identity: mid-windup AND mid-charge Brutes shrug off staggers.
            bool bruteWindupLock = isBrute && (_state == State.WindUp || _state == State.Charging);

            // Launch / knockdown floor the light enemies; Brutes never leave their feet.
            if (!isBrute && info.Hit == HitType.Launch)
            {
                ReleaseTokenIfHeld();
                _vY = _state == State.Launched ? GameConfig.LaunchPopVelocity * 0.6f : GameConfig.LaunchPopVelocity;
                EnterPhase(State.Launched, 2f); // safety timeout; landing switches to Knockdown
                return;
            }
            if (!isBrute && info.Hit == HitType.Knockdown && _state != State.Launched)
            {
                ReleaseTokenIfHeld();
                EnterPhase(State.Knockdown, GameConfig.KnockdownDuration);
                return;
            }
            if (_state == State.Launched || _state == State.Knockdown || _state == State.Getup) return;

            if (!isBrute && info.IsHeavy) _vY = Mathf.Max(_vY, GameConfig.KnockbackUpPop);

            bool staggers = info.IsHeavy || _stats.StaggersFromLight;
            if (staggers && !bruteWindupLock)
            {
                ReleaseTokenIfHeld();
                _state = State.Stagger;
                _staggerT = _stats.StaggerTime;
            }
        }

        public void OnKilled(in DamageInfo info)
        {
            if (_notifiedDeath) return;
            _notifiedDeath = true;
            _state = State.Dead;
            _deathT = 0f;
            _deathClipStarted = false;
            ReleaseTokenIfHeld();
            if (_cc != null) _cc.enabled = false;

            Impact.EnemyKilled(info.IsHeavy);
            var gm = GameManager.Instance;
            gm?.RegisterEnemyKilled(_stats.Points, transform.position + Vector3.up * 1.9f);
            gm?.Sfx?.Play(_stats.DeathSfxKey);
            _waves?.NotifyEnemyDead(this);

            Destroy(gameObject, 1.4f);
        }

        private void AnimateVisual()
        {
            if (_visual == null) return;
            if (_skeletal != null && _skeletal.Ready) TickSkeletal();
            else AnimateProcedural();
        }

        // ---- Skeletal path ----

        private void TickSkeletal()
        {
            float dt = Time.deltaTime;
            float face = _facing >= 0f ? 1f : -1f;
            Quaternion facingRot = Quaternion.Euler(0f, (face > 0f ? 90f : -90f) + FacingYOffset, 0f);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, facingRot,
                1f - Mathf.Exp(-GameConfig.PoseSmoothRotK * dt));

            Vector3 juice = Vector3.zero;
            if (_hitFlinch > 0f && _state != State.Dead)
                juice.x = -face * 0.08f * Mathf.Clamp01(_hitFlinch / 0.15f);
            _visual.localPosition = Vector3.Lerp(_visual.localPosition, juice,
                1f - Mathf.Exp(-GameConfig.PoseSmoothPosK * dt));

            switch (_state)
            {
                case State.Dead:
                    if (!_deathClipStarted)
                    {
                        _deathClipStarted = true;
                        _skeletal.Retrigger("Die");
                        float len = _skeletal.ClipLength("Die");
                        if (len > 0f) _skeletal.SetStateSpeed("Die", len / 1.15f); // finish before Destroy
                    }
                    return;

                case State.Launched:
                    _skeletal.Scrub("Hit", 0.5f);
                    return;

                case State.Knockdown:
                    _skeletal.Scrub("Die", Mathf.Min(1f, (1f - _phaseT / _phaseDuration) * 2.5f));
                    return;

                case State.Getup:
                    _skeletal.Scrub("Die", _phaseT / _phaseDuration); // fall clip in reverse
                    return;

                case State.Stagger:
                    _skeletal.Scrub("Hit", 0.7f);
                    return;

                case State.WindUp:
                {
                    var def = FindClip("Attack");
                    _skeletal.ScrubWindow("Attack", 0f, def.ActiveStartNorm, 1f - _phaseT / _phaseDuration);
                    return;
                }

                case State.Cooldown:
                {
                    var def = FindClip("Attack");
                    float followT = (1f - _phaseT / _phaseDuration) * (_phaseDuration / Mathf.Min(_phaseDuration, 0.5f));
                    _skeletal.ScrubWindow("Attack", def.ActiveStartNorm, 1f, followT);
                    return;
                }

                case State.ChargeTelegraph:
                    if (_skeletal.Has("Charge")) _skeletal.Scrub("Charge", 0.05f);
                    else _skeletal.CrossFade("Idle");
                    return;

                case State.Charging:
                    if (_skeletal.Has("Charge")) _skeletal.CrossFade("Charge");
                    else _skeletal.CrossFade("Walk");
                    return;
            }

            if (_hitFlinch > 0f)
            {
                if (_hitRetrigger) { _skeletal.Retrigger("Hit"); _hitRetrigger = false; }
                return;
            }
            if (_moving) { _skeletal.CrossFade("Walk"); return; }
            _skeletal.CrossFade("Idle");
        }

        private GameConfig.ClipDef FindClip(string key)
        {
            var set = _stats.ClipSet;
            if (set != null)
            {
                for (int i = 0; i < set.Length; i++)
                    if (set[i].Key == key) return set[i];
            }
            return new GameConfig.ClipDef(key, null, WrapMode.Once, 0.05f);
        }

        // ---- Procedural fallback (the original puppet + new floor states) ----

        private void AnimateProcedural()
        {
            float face = _facing >= 0f ? 1f : -1f;
            Quaternion facingRot = Quaternion.Euler(0f, (face > 0f ? 90f : -90f) + FacingYOffset, 0f);
            Vector3 pos = Vector3.zero;
            Quaternion rot = facingRot;

            if (_state == State.Dead)
            {
                _deathT += Time.deltaTime;
                float k = Mathf.Clamp01(_deathT / 0.7f);
                rot = facingRot * Quaternion.Euler(0f, 0f, -90f * k);
                pos += new Vector3(0f, -0.2f * k, 0f);
            }
            else if (_state == State.Launched)
            {
                rot = facingRot * Quaternion.Euler(0f, 0f, -55f);
                pos += new Vector3(-face * 0.1f, 0f, 0f);
            }
            else if (_state == State.Knockdown || _state == State.Getup)
            {
                float k = _state == State.Knockdown
                    ? Mathf.Min(1f, (1f - _phaseT / _phaseDuration) * 2.5f)
                    : _phaseT / _phaseDuration;
                rot = facingRot * Quaternion.Euler(0f, 0f, -80f * k);
                pos += new Vector3(0f, -0.35f * k, 0f);
            }
            else if (_hitFlinch > 0f)
            {
                float k = Mathf.Clamp01(_hitFlinch / 0.15f);
                pos += new Vector3(-face * 0.15f * k, 0f, 0f);
                rot = facingRot * Quaternion.Euler(-10f * k, 0f, 0f);
            }
            else if (_state == State.Stagger)
            {
                rot = facingRot * Quaternion.Euler(-14f, 0f, 0f);
                pos += new Vector3(-face * 0.1f, 0f, 0f);
            }
            else if (_state == State.WindUp)
            {
                rot = facingRot * Quaternion.Euler(Mathf.Lerp(0f, 18f, 1f - _phaseT / Mathf.Max(0.0001f, _stats.WindUp)), 0f, 0f);
            }
            else if (_state == State.ChargeTelegraph)
            {
                rot = facingRot * Quaternion.Euler(24f, 0f, 0f); // head down, coiled
                pos += new Vector3(-face * 0.15f, 0f, 0f);
            }
            else if (_state == State.Charging)
            {
                rot = facingRot * Quaternion.Euler(30f, 0f, 0f);
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.06f;
                pos += new Vector3(face * 0.2f, bob, 0f);
            }
            else if (_state == State.Cooldown && _phaseT > _phaseDuration - 0.2f)
            {
                // brief follow-through lunge right after the strike
                pos += new Vector3(face * 0.3f, 0f, 0f);
                rot = facingRot * Quaternion.Euler(-12f, 0f, 0f);
            }
            else if (_moving)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 9f + transform.position.x)) * 0.05f;
                pos += new Vector3(0f, bob, 0f);
                rot = facingRot * Quaternion.Euler(5f, 0f, 0f);
            }

            // Exponential smoothing: identical feel at 30 and 120 fps.
            float posK = 1f - Mathf.Exp(-GameConfig.PoseSmoothPosK * Time.deltaTime);
            float rotK = 1f - Mathf.Exp(-GameConfig.PoseSmoothRotK * Time.deltaTime);
            _visual.localPosition = Vector3.Lerp(_visual.localPosition, pos, posK);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, rot, rotK);
        }
    }
}
