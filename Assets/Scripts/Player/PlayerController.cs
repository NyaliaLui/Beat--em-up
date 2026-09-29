using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// The player knight. Receives the touch-button calls, moves left/right along X only
    /// (camera is side-locked), integrates decaying knockback impulses and the dodge roll,
    /// and drives the visual: through the skeletal ActorAnimator once the Higgsfield clips
    /// are bound, or through the original procedural puppet as an always-works fallback.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float FacingSign { get; private set; } = 1f;
        public float FacingYOffset = 0f; // flip by 180 if the model ends up facing backwards

        private CharacterController _cc;
        private CombatSystem _combat;
        private Transform _visual;
        private HitFlash _flash;
        private ActorAnimator _skeletal;

        private bool _leftHeld, _rightHeld;
        private int _lastPressed = 1;
        private bool _moving;
        private bool _dead;
        private float _deathT;
        private float _hitTimer;
        private bool _hitRetrigger;
        private float _vY;
        private float _kbVel; // decaying knockback impulse along X (m/s)
        private float _stepAccum; // metres walked since the last footstep sfx

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        // CombatSystem resolved in Start to avoid the circular Awake-order issue (see CombatSystem).
        private void Start()
        {
            _combat = GetComponent<CombatSystem>();
        }

        public void SetVisual(Transform visual)
        {
            _visual = visual;
            if (_flash == null) _flash = gameObject.AddComponent<HitFlash>();
            _flash.Init(visual);
            BindSkeletal();
        }

        /// <summary>Debug-cycler access to the live animator (null while procedural).</summary>
        public ActorAnimator DebugAnimator => _skeletal;

        private async void BindSkeletal()
        {
            await AnimLibrary.PreloadSet(GameConfig.HeroClips);
            if (this == null || _visual == null) return;
            var animator = new ActorAnimator();
            if (animator.TryBind(_visual, GameConfig.HeroClips)) _skeletal = animator;
        }

        public void ResetActor()
        {
            _dead = false;
            _deathT = 0f;
            _hitTimer = 0f;
            _hitRetrigger = false;
            _leftHeld = _rightHeld = false;
            _lastPressed = 1;
            FacingSign = 1f;
            _vY = 0f;
            _kbVel = 0f;
            _skeletal?.ResetToIdle();
            TeleportTo(new Vector3(0f, 0.1f, GameConfig.PlayZ));
        }

        private void TeleportTo(Vector3 pos)
        {
            if (_cc != null) _cc.enabled = false;
            transform.position = pos;
            if (_cc != null) _cc.enabled = true;
        }

        // ---- Touch input (wired by TouchInputUI) ----
        private bool InputAllowed => !_dead && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

        public void OnMoveLeftDown() { if (!InputAllowed) return; _leftHeld = true; _lastPressed = -1; Combo(GameConfig.ComboInput.Left); }
        public void OnMoveLeftUp() { _leftHeld = false; }
        public void OnMoveRightDown() { if (!InputAllowed) return; _rightHeld = true; _lastPressed = 1; Combo(GameConfig.ComboInput.Right); }
        public void OnMoveRightUp() { _rightHeld = false; }
        public void OnPunch() { if (!InputAllowed) return; _combat.TryAttack(false); Combo(GameConfig.ComboInput.Punch); }
        public void OnSword() { if (!InputAllowed) return; _combat.TryAttack(true); Combo(GameConfig.ComboInput.Sword); }
        public void OnDodge() { if (!InputAllowed) return; _combat.TryDodge(ComputeMoveInput()); Combo(GameConfig.ComboInput.Roll); }
        public void OnBlockDown() { if (!InputAllowed) return; _combat.SetBlock(true); Combo(GameConfig.ComboInput.Block); }
        public void OnBlockUp() { _combat.SetBlock(false); }

        // Every press also feeds the secret-combo easter egg.
        private static void Combo(GameConfig.ComboInput input) => GameManager.Instance?.EasterEggs?.RecordInput(input);

        private int ComputeMoveInput()
        {
            if (_leftHeld && _rightHeld) return _lastPressed;
            if (_leftHeld) return -1;
            if (_rightHeld) return 1;
            return 0;
        }

        private void Update()
        {
            bool playing = GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;

            if (_dead)
            {
                AnimateVisual();
                return;
            }

            int move = playing ? ComputeMoveInput() : 0;
            if (_combat != null && !_combat.CanMove) move = 0;
            _moving = move != 0;
            if (move != 0) FacingSign = move;

            float horizontal = move * GameConfig.PlayerMoveSpeed;

            // Dodge roll: ease-out translation along the roll direction, facing locked to it.
            if (_combat != null && _combat.IsDodging)
            {
                float t = _combat.DodgeT01;
                horizontal = _combat.DodgeDir * 2f * GameConfig.DodgeDistance * (1f - t) / GameConfig.DodgeDuration;
                FacingSign = _combat.DodgeDir;
                _moving = false;
            }

            // Decaying knockback impulse.
            horizontal += _kbVel;
            _kbVel *= Mathf.Exp(-GameConfig.KnockbackDamping * Time.deltaTime);
            if (Mathf.Abs(_kbVel) < 0.01f) _kbVel = 0f;

            // Horizontal move + gravity, locked to the X lane and Z plane.
            _vY += Physics.gravity.y * Time.deltaTime;
            if (_cc.isGrounded && _vY < 0f) _vY = -1f;
            Vector3 delta = new Vector3(horizontal * Time.deltaTime, _vY * Time.deltaTime, 0f);
            _cc.Move(delta);

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, GameConfig.LaneMinX, GameConfig.LaneMaxX);
            p.z = GameConfig.PlayZ;
            transform.position = p;

            // Footsteps by distance walked (0.9 m stride).
            if (_moving)
            {
                _stepAccum += Mathf.Abs(move * GameConfig.PlayerMoveSpeed) * Time.deltaTime;
                if (_stepAccum >= 0.9f)
                {
                    _stepAccum = 0f;
                    GameManager.Instance?.Sfx?.Play(SfxManager.Footstep, 0.35f);
                }
            }

            AnimateVisual();
        }

        public void OnHitReaction(in DamageInfo info, bool blocked)
        {
            float dir = Mathf.Sign(transform.position.x - info.SourcePos.x);
            if (dir == 0f) dir = -FacingSign;
            _kbVel = dir * info.Knockback * GameConfig.KnockbackImpulseScale * (blocked ? 0.3f : 1f);
            if (!blocked && info.IsHeavy) _vY = Mathf.Max(_vY, GameConfig.KnockbackUpPop);
            _hitTimer = 0.18f;
            _hitRetrigger = !blocked;
            if (blocked) HitSparks.Blocked(transform.position + Vector3.up * 1.1f, dir);
            else _flash?.FlashWhite();
        }

        public void OnDeath()
        {
            _dead = true;
            _deathT = 0f;
        }

        private void AnimateVisual()
        {
            if (_visual == null) return;
            if (_skeletal != null && _skeletal.Ready) TickSkeletal();
            else AnimateProcedural();
        }

        // ---- Skeletal path: clips carry the pose; code keeps facing + flinch juice ----

        private void TickSkeletal()
        {
            float dt = Time.deltaTime;
            float face = FacingSign >= 0f ? 1f : -1f;
            Quaternion facingRot = Quaternion.Euler(0f, (face > 0f ? 90f : -90f) + FacingYOffset, 0f);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation, facingRot,
                1f - Mathf.Exp(-GameConfig.PoseSmoothRotK * dt));

            // Additive flinch offset composes with bone animation (small, decays fast).
            Vector3 juice = Vector3.zero;
            if (_hitTimer > 0f && !_dead)
            {
                _hitTimer -= dt;
                juice.x = -face * 0.1f * Mathf.Clamp01(_hitTimer / 0.18f);
            }
            _visual.localPosition = Vector3.Lerp(_visual.localPosition, juice,
                1f - Mathf.Exp(-GameConfig.PoseSmoothPosK * dt));

            if (_dead) { _skeletal.CrossFade("Die"); return; }
            if (_combat == null) { _skeletal.CrossFade("Idle"); return; }

            if (_combat.IsKnockedDown)
            {
                // Fall fast and hold; the getup plays the knockdown clip backwards.
                if (_combat.Action == CombatSystem.ActionState.Getup)
                    _skeletal.Scrub("Knockdown", 1f - _combat.GetupT01);
                else
                    _skeletal.Scrub("Knockdown", Mathf.Min(1f, _combat.KnockdownT01 * 2.5f));
                return;
            }
            if (_hitTimer > 0f && !_combat.IsAttacking)
            {
                if (_hitRetrigger) { _skeletal.Retrigger("Hit"); _hitRetrigger = false; }
                return;
            }
            if (_combat.IsDodging) { _skeletal.Scrub("Roll", _combat.DodgeT01); return; }
            if (_combat.IsAttacking)
            {
                _skeletal.ScrubAttack(_combat.CurrentAttack.ClipKey, _combat.Phase, _combat.PhaseT01);
                return;
            }
            if (_combat.IsBlocking) { _skeletal.CrossFade("Block"); return; }
            if (_combat.IsStunned) { _skeletal.Scrub("Hit", 0.9f); return; }
            if (_moving) { _skeletal.CrossFade("Walk"); return; }
            _skeletal.CrossFade("Idle");
        }

        // ---- Procedural fallback: the original whole-mesh puppet (always works) ----

        private void AnimateProcedural()
        {
            float face = FacingSign >= 0f ? 1f : -1f;
            Quaternion facingRot = Quaternion.Euler(0f, (face > 0f ? 90f : -90f) + FacingYOffset, 0f);
            Vector3 pos = Vector3.zero;
            Quaternion rot = facingRot;

            if (_dead)
            {
                _deathT += Time.deltaTime;
                float k = Mathf.Clamp01(_deathT / 0.8f);
                rot = facingRot * Quaternion.Euler(0f, 0f, -88f * k);
                pos += new Vector3(0f, -0.15f * k, 0f);
                _visual.localPosition = pos;
                _visual.localRotation = rot;
                return;
            }

            if (_combat != null && _combat.IsKnockedDown)
            {
                float k = _combat.Action == CombatSystem.ActionState.Getup
                    ? 1f - _combat.GetupT01
                    : Mathf.Min(1f, _combat.KnockdownT01 * 2.5f);
                rot = facingRot * Quaternion.Euler(0f, 0f, -80f * k);
                pos += new Vector3(0f, -0.35f * k, 0f);
            }
            else if (_combat != null && _combat.IsDodging)
            {
                // Forward roll: full spin over the dodge, slight crouch.
                rot = facingRot * Quaternion.Euler(0f, 0f, -360f * _combat.DodgeT01);
                pos += new Vector3(0f, -0.25f * Mathf.Sin(_combat.DodgeT01 * Mathf.PI), 0f);
            }
            else if (_hitTimer > 0f)
            {
                _hitTimer -= Time.deltaTime;
                float k = Mathf.Clamp01(_hitTimer / 0.18f);
                pos += new Vector3(-face * 0.18f * k, 0f, 0f);
                rot = facingRot * Quaternion.Euler(-10f * k, 0f, 0f);
            }
            else if (_combat != null && _combat.IsAttacking)
            {
                float lunge = 0f, pitch = 0f;
                float heavyPitch = _combat.CurrentHeavy ? -14f : -7f;
                switch (_combat.Phase)
                {
                    case CombatSystem.AttackPhase.Startup:
                        lunge = Mathf.Lerp(0f, -0.07f, _combat.PhaseT01);
                        break;
                    case CombatSystem.AttackPhase.Active:
                        lunge = Mathf.Lerp(0f, 0.38f, _combat.PhaseT01);
                        pitch = heavyPitch;
                        break;
                    case CombatSystem.AttackPhase.Recovery:
                        lunge = Mathf.Lerp(0.38f, 0f, _combat.PhaseT01);
                        pitch = Mathf.Lerp(heavyPitch, 0f, _combat.PhaseT01);
                        break;
                }
                pos += new Vector3(face * lunge, 0f, 0f);
                rot = facingRot * Quaternion.Euler(pitch, 0f, 0f);
            }
            else if (_combat != null && _combat.IsBlocking)
            {
                pos += new Vector3(-face * 0.12f, 0f, 0f);
                rot = facingRot * Quaternion.Euler(0f, 0f, face * 10f);
            }
            else if (_combat != null && _combat.IsStunned)
            {
                rot = facingRot * Quaternion.Euler(8f, 0f, 0f);
            }
            else if (_moving)
            {
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.05f;
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
