using System.Threading.Tasks;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// The hidden treasure chest easter egg. Sits in the background behind the barrel/crate pile
    /// and gives itself away only by a slow gold glint. While the player stands directly in front
    /// of it, <see cref="Current"/> is set and the PUNCH button turns into OPEN (TouchInputUI);
    /// pressing it swaps the closed model for the gold-filled one, bursts sparks and asks the
    /// EasterEggManager for the merch-store popup. Re-closes at the start of every run.
    /// </summary>
    public class SecretChest : MonoBehaviour
    {
        /// <summary>The chest the player can open right now, or null.</summary>
        public static SecretChest Current { get; private set; }

        public bool Opened { get; private set; }

        private Transform _closedVisual, _openVisual;
        private Light _glint;
        private float _openPop;

        public static async Task<SecretChest> Build()
        {
            var go = new GameObject("SecretChest");
            go.transform.position = GameConfig.ChestPosition;
            go.transform.rotation = Quaternion.Euler(0f, GameConfig.ChestYaw, 0f);
            var chest = go.AddComponent<SecretChest>();

            chest._closedVisual = await GlbLoader.AttachVisual(GameConfig.ModelChest, go.transform, GameConfig.ChestHeight, GameConfig.Leather);
            // The open chest's raised lid makes it taller; scale so the box itself matches the closed one.
            chest._openVisual = await GlbLoader.AttachVisual(GameConfig.ModelChestOpen, go.transform, GameConfig.ChestHeight * 1.35f, GameConfig.ChestGold);
            if (chest == null) return null;
            if (chest._openVisual != null) chest._openVisual.gameObject.SetActive(false);

            var glintGo = new GameObject("ChestGlint");
            glintGo.transform.SetParent(go.transform, false);
            glintGo.transform.localPosition = new Vector3(0f, 0.9f, -0.7f); // toward the camera
            chest._glint = glintGo.AddComponent<Light>();
            chest._glint.type = LightType.Point;
            chest._glint.color = GameConfig.ChestGold;
            chest._glint.range = 2.2f;
            chest._glint.intensity = 0f;
            return chest;
        }

        public void ResetChest()
        {
            Opened = false;
            _openPop = 0f;
            transform.localScale = Vector3.one;
            if (_closedVisual != null) _closedVisual.gameObject.SetActive(true);
            if (_openVisual != null) _openVisual.gameObject.SetActive(false);
        }

        public void Open()
        {
            if (Opened) return;
            Opened = true;
            if (Current == this) Current = null;

            if (_closedVisual != null) _closedVisual.gameObject.SetActive(false);
            if (_openVisual != null) _openVisual.gameObject.SetActive(true);
            _openPop = 1f;

            Vector3 fx = transform.position + Vector3.up * 0.6f;
            HitSparks.Burst(fx, 1f, true);
            HitSparks.Burst(fx, -1f, true);
            Impact.Trauma(0.2f);
            GameManager.Instance?.Sfx?.Play(SfxManager.ComboUp);
            GameManager.Instance?.EasterEggs?.OnChestOpened();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool live = gm != null && gm.State == GameState.Playing && gm.Player != null;

            bool inFront = live && !Opened &&
                           Mathf.Abs(gm.Player.transform.position.x - transform.position.x) <= GameConfig.ChestInteractRange;
            if (inFront) Current = this;
            else if (Current == this) Current = null;

            if (_glint != null)
            {
                // Mostly dark with a quick sparkle every few seconds - easy to miss mid-fight.
                float target = 0f;
                if (!Opened)
                {
                    float phase = Mathf.Repeat(Time.time, 3.2f);
                    target = phase < 0.35f ? Mathf.Sin(phase / 0.35f * Mathf.PI) * 3.5f : 0.25f;
                    if (inFront) target = Mathf.Max(target, 1.6f + 0.4f * Mathf.Sin(Time.time * 6f));
                }
                else if (_openPop > 0f)
                {
                    target = 5f * _openPop; // treasure flare that fades after opening
                }
                _glint.intensity = target;
            }

            if (_openPop > 0f)
            {
                // Pop the whole root: its pivot is on the floor, so the chest bounces in place.
                _openPop = Mathf.MoveTowards(_openPop, 0f, Time.unscaledDeltaTime / 0.6f);
                transform.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(_openPop * Mathf.PI));
            }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
