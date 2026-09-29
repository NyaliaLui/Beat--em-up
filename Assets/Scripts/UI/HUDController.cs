using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// In-game HUD (section 13): HP + stamina bars, WAVE / SCORE / COMBO readouts, the centre
    /// wave banner, the pause button and the on-screen controls. Visible only while Playing or
    /// Paused; reads live values from the systems each frame.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private RectTransform _root;
        private RectTransform _controls;
        private Text _hpValue, _waveText, _scoreText, _comboText, _banner;
        private Image _hpFill, _staminaFill, _ghostFill, _vignette, _decayFill, _bossFill;
        private RectTransform _bossBar;
        private float _bannerTimer;
        private float _ghost = 1f;
        private float _comboPop;
        private int _lastMult = 1;

        public void Build(Transform canvas, PlayerController player, Sprite panelSprite, Sprite buttonSprite)
        {
            _root = UIFactory.FullScreen("HUD", canvas);

            // Low-HP vignette: first child so everything draws over it.
            _vignette = UIFactory.Image("Vignette", _root, null, new Color(0.55f, 0.04f, 0.04f, 0f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _vignette.rectTransform.offsetMin = Vector2.zero;
            _vignette.rectTransform.offsetMax = Vector2.zero;

            // HP
            UIFactory.Label("HpLabel", _root, "HP", 26, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -34), new Vector2(120, 30));
            _hpFill = UIFactory.Bar("HpBar", _root, new Color(0.78f, 0.22f, 0.20f, 1f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -64), new Vector2(360, 30));

            // Damage-lag ghost: sits between the dark background and the red fill, drains slowly.
            _ghostFill = UIFactory.Image("HpGhost", _hpFill.transform.parent, null, new Color(1f, 0.9f, 0.8f, 0.55f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            _ghostFill.rectTransform.offsetMin = Vector2.zero;
            _ghostFill.rectTransform.offsetMax = Vector2.zero;
            _hpFill.transform.SetAsLastSibling();
            _hpValue = UIFactory.Label("HpValue", _hpFill.transform.parent, "100 / 100", 20, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _hpValue.rectTransform.offsetMin = Vector2.zero; _hpValue.rectTransform.offsetMax = Vector2.zero;

            // Stamina
            UIFactory.Label("StaminaLabel", _root, "STAMINA", 20, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.6f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -104), new Vector2(140, 24));
            _staminaFill = UIFactory.Bar("StaminaBar", _root, GameConfig.EmberOrange,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -130), new Vector2(300, 18));

            // Wave (top centre)
            _waveText = UIFactory.Label("WaveText", _root, "WAVE 1", 32, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(300, 44));

            // Boss HP (under the wave readout, only while a boss is alive)
            _bossBar = UIFactory.Rect("BossBar", _root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -80), new Vector2(640, 60));
            UIFactory.Label("BossLabel", _bossBar, "WAR-CHIEF", 24, TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.45f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(640, 28));
            _bossFill = UIFactory.Bar("BossHp", _bossBar, new Color(0.72f, 0.12f, 0.10f, 1f),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(640, 22));
            _bossBar.gameObject.SetActive(false);

            // Score + combo (top right)
            _scoreText = UIFactory.Label("ScoreText", _root, "SCORE 0", 32, TextAnchor.MiddleRight, Color.white,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -34), new Vector2(360, 40));
            _comboText = UIFactory.Label("ComboText", _root, "x1", 28, TextAnchor.MiddleRight, new Color(1, 1, 1, 0.45f),
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -78), new Vector2(360, 36));

            // Style decay bar: a thin drain under the combo label showing the refresh window.
            _decayFill = UIFactory.Bar("StyleDecay", _root, GameConfig.EmberOrange,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -100), new Vector2(200, 6));

            // Pause button (top right corner)
            var pause = UIFactory.RoundButton("BtnPause", _root, "II", 34,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-60, -54), 84f, buttonSprite, Color.white);
            pause.OnDown = () => GameManager.Instance?.TogglePause();

            // Centre wave banner (fades out)
            _banner = UIFactory.Label("Banner", _root, "", 90, TextAnchor.MiddleCenter, GameConfig.EmberOrange,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(900, 140));
            SetAlpha(_banner, 0f);

            // On-screen controls.
            _controls = UIFactory.FullScreen("Controls", _root);
            var input = _controls.gameObject.AddComponent<TouchInputUI>();
            input.Build(player, buttonSprite);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateChanged += OnStateChanged;
                if (gm.Waves != null) gm.Waves.WaveStarted += ShowBanner;
            }
            OnStateChanged(gm != null ? gm.State : GameState.Menu);
        }

        private void OnStateChanged(GameState s)
        {
            bool inGame = s == GameState.Playing || s == GameState.Paused || s == GameState.Promo;
            if (_root != null) _root.gameObject.SetActive(inGame);
            if (_controls != null) _controls.gameObject.SetActive(s == GameState.Playing);
        }

        private void ShowBanner(int wave)
        {
            if (_banner == null) return;
            bool boss = GameConfig.IsBossWave(wave);
            _banner.text = boss ? "BOSS" : "WAVE " + wave;
            _banner.color = boss ? new Color(0.9f, 0.2f, 0.15f) : GameConfig.EmberOrange;
            _bannerTimer = 1.8f;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || _root == null || !_root.gameObject.activeSelf) return;

            if (gm.PlayerHealth != null)
            {
                float hp = gm.PlayerHealth.Normalized;
                _hpFill.rectTransform.anchorMax = new Vector2(hp, 1f);
                _hpValue.text = $"{Mathf.CeilToInt(Mathf.Max(0, gm.PlayerHealth.HP))} / {Mathf.CeilToInt(gm.PlayerHealth.Max)}";

                // Ghost bar chases the real value slowly so damage leaves a readable trace.
                _ghost = hp < _ghost ? Mathf.MoveTowards(_ghost, hp, 0.4f * Time.unscaledDeltaTime) : hp;
                _ghostFill.rectTransform.anchorMax = new Vector2(_ghost, 1f);

                // Low-HP vignette pulse.
                bool low = gm.PlayerHealth.HP > 0f && gm.PlayerHealth.HP <= GameConfig.LowHpThreshold;
                float targetA = low ? 0.10f + 0.05f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
                Color vc = _vignette.color;
                vc.a = Mathf.MoveTowards(vc.a, targetA, Time.unscaledDeltaTime * 0.8f);
                _vignette.color = vc;
            }
            if (gm.PlayerStamina != null)
                _staminaFill.rectTransform.anchorMax = new Vector2(gm.PlayerStamina.Normalized, 1f);
            if (gm.Waves != null)
            {
                _waveText.text = "WAVE " + gm.Waves.CurrentWave;
                var boss = gm.Waves.ActiveBoss;
                bool showBoss = boss != null && boss.Health != null && boss.IsAlive;
                if (_bossBar.gameObject.activeSelf != showBoss) _bossBar.gameObject.SetActive(showBoss);
                if (showBoss) _bossFill.rectTransform.anchorMax = new Vector2(boss.Health.Normalized, 1f);
            }
            if (gm.Score != null)
            {
                _scoreText.text = "SCORE " + gm.Score.Score.ToString("N0");
                int mult = gm.Score.Multiplier;
                if (mult > _lastMult) _comboPop = 1f;   // tier-up: scale pop
                _lastMult = mult;
                if (mult > 1)
                {
                    _comboText.text = gm.Score.TierName + "  x" + mult;
                    _comboText.color = GameConfig.EmberOrange;
                }
                else
                {
                    _comboText.text = "x1";
                    _comboText.color = new Color(1, 1, 1, 0.45f);
                }

                _comboPop = Mathf.MoveTowards(_comboPop, 0f, Time.unscaledDeltaTime / 0.15f);
                _comboText.rectTransform.localScale = Vector3.one * (1f + 0.4f * _comboPop);

                bool showDecay = mult > 1;
                _decayFill.transform.parent.gameObject.SetActive(showDecay);
                if (showDecay)
                    _decayFill.rectTransform.anchorMax = new Vector2(gm.Score.DecayFraction01, 1f);
            }

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.unscaledDeltaTime;
                SetAlpha(_banner, Mathf.Clamp01(_bannerTimer / 0.6f));
            }
        }

        private static void SetAlpha(Text t, float a)
        {
            if (t == null) return;
            Color c = t.color; c.a = a; t.color = c;
        }
    }
}
