using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Watches for the three easter eggs that promote the artist and queues the matching popup
    /// (each links to a "fanbase endpoint"):
    ///   1. Secret 8-press input combo  -> Patreon (support the artist directly).
    ///   2. Opening the hidden chest    -> merch store (rare drop).
    ///   3. Boss killed under 10 s      -> the comic release (relax with a good read).
    /// Each egg fires at most once per run. Popups wait a short unscaled beat after the trigger
    /// and only open during live play; if the fight isn't live (e.g. paused) they stay queued.
    /// </summary>
    public class EasterEggManager : MonoBehaviour
    {
        // Copy lives here so marketing text can be tuned in one place.
        private static readonly PromoPopupController.Promo PatreonPromo = new PromoPopupController.Promo(
            "SECRET TECHNIQUE UNLOCKED!",
            "Only true fans find that combo. The artist behind IRONHOLD shares sketches, process " +
            "and early pages on Patreon - support them directly and keep the art coming.",
            "SUPPORT ON PATREON", GameConfig.PromoPatreonUrl);

        private static readonly PromoPopupController.Promo MerchPromo = new PromoPopupController.Promo(
            "RARE DROP!",
            "Lucky you - that chest only shines for the sharp-eyed. The real loot is in the " +
            "artist's merch store: prints, apparel and limited drops. Go see what's in stock.",
            "CHECK OUT THE DROPS", GameConfig.PromoMerchUrl);

        private const string ComicTitle = "FLAWLESS TAKEDOWN!";
        private const string ComicBodyFormat =
            "Boss down in {0:0.0}s - that's serious skill. You've earned a breather: " +
            "kick back with a good read, Moon Knight: Fist of Khonshu (2024).";

        private enum Egg { Combo, Chest, BossSpeedKill }

        private struct Pending
        {
            public PromoPopupController.Promo Promo;
            public float Delay;
        }

        private readonly HashSet<Egg> _firedThisRun = new HashSet<Egg>();
        private readonly Queue<Pending> _queue = new Queue<Pending>();
        private float _queueDelay;

        private int _comboProgress;
        private float _lastComboInputTime = -999f;

        private SecretChest _chest;

        public void Configure(WaveManager waves, SecretChest chest)
        {
            _chest = chest;
            if (waves != null) waves.BossDefeated += OnBossDefeated;
        }

        public void ResetRun()
        {
            _firedThisRun.Clear();
            _queue.Clear();
            _comboProgress = 0;
            _chest?.ResetChest();
        }

        // ---- Egg 1: secret input combo (fed by PlayerController on every button press) ----

        public void RecordInput(GameConfig.ComboInput input)
        {
            var seq = GameConfig.SecretCombo;
            float now = Time.unscaledTime;
            if (now - _lastComboInputTime > GameConfig.SecretComboMaxGap) _comboProgress = 0;
            _lastComboInputTime = now;

            if (input == seq[_comboProgress]) _comboProgress++;
            else _comboProgress = input == seq[0] ? 1 : 0; // a wrong press may still start a new attempt

            if (_comboProgress < seq.Length) return;
            _comboProgress = 0;
            Trigger(Egg.Combo, PatreonPromo, 0.4f);
        }

        // ---- Egg 2: hidden chest ----

        public void OnChestOpened() => Trigger(Egg.Chest, MerchPromo, GameConfig.PromoRevealDelay);

        // ---- Egg 3: boss speed-kill ----

        private void OnBossDefeated(float fightSeconds)
        {
            if (fightSeconds >= GameConfig.BossSpeedKillSeconds) return;
            var promo = new PromoPopupController.Promo(ComicTitle,
                string.Format(ComicBodyFormat, fightSeconds), "READ THE ISSUE", GameConfig.PromoComicUrl);
            Trigger(Egg.BossSpeedKill, promo, GameConfig.PromoRevealDelay);
        }

        private void Trigger(Egg egg, PromoPopupController.Promo promo, float delay)
        {
            if (!_firedThisRun.Add(egg)) return;
            if (_queue.Count == 0) _queueDelay = delay;
            _queue.Enqueue(new Pending { Promo = promo, Delay = delay });
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (_queue.Count == 0 || gm == null || gm.State != GameState.Playing) return;

            _queueDelay -= Time.unscaledDeltaTime;
            if (_queueDelay > 0f) return;

            if (gm.OpenPromo(_queue.Peek().Promo))
            {
                _queue.Dequeue();
                if (_queue.Count > 0) _queueDelay = _queue.Peek().Delay;
            }
        }
    }
}
