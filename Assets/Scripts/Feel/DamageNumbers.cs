using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Pooled world-anchored damage / score popups on the overlay canvas. Numbers rise,
    /// scale-pop 1.3 -> 1.0, then fade. Animates on unscaled time so popups read through
    /// hit-stop but freeze on pause. Built in code by Bootstrap (Build(canvas)).
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        public static DamageNumbers Instance { get; private set; }

        private const int PoolSize = 24;
        private const float Life = 0.7f;
        private const float RiseMeters = 0.9f;

        private struct Entry
        {
            public Text Text;
            public RectTransform Rect;
            public Vector3 WorldPos;
            public float Age;
            public bool Active;
        }

        private Entry[] _pool;
        private RectTransform _canvasRect;
        private int _next;

        public void Build(Transform canvas)
        {
            Instance = this;
            _canvasRect = canvas as RectTransform;
            var root = UIFactory.FullScreen("DamageNumbers", canvas);
            root.gameObject.AddComponent<Canvas>();      // own draw layer, no re-batch of the HUD
            _pool = new Entry[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var t = UIFactory.Label("Dmg" + i, root, "", 30, TextAnchor.MiddleCenter, Color.white,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(200f, 50f));
                t.gameObject.SetActive(false);
                _pool[i] = new Entry { Text = t, Rect = t.rectTransform };
            }
            transform.SetParent(root, false);
        }

        public static void Spawn(Vector3 worldPos, string msg, Color color, int fontSize = 30)
        {
            var self = Instance;
            if (self == null || self._pool == null) return;
            int i = self._next;
            self._next = (self._next + 1) % PoolSize;

            ref Entry e = ref self._pool[i];
            e.WorldPos = worldPos;
            e.Age = 0f;
            e.Active = true;
            e.Text.text = msg;
            e.Text.color = color;
            e.Text.fontSize = fontSize;
            e.Text.gameObject.SetActive(true);
        }

        public static void Damage(Vector3 worldPos, float amount, bool heavy)
        {
            Spawn(worldPos, Mathf.RoundToInt(amount).ToString(),
                heavy ? GameConfig.EmberOrange : Color.white, heavy ? 36 : 30);
        }

        public static void Score(Vector3 worldPos, int points)
        {
            Spawn(worldPos, "+" + points, new Color(1f, 0.9f, 0.35f), 32);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Paused || gm.State == GameState.Promo)) return;
            var cam = UnityEngine.Camera.main;
            if (cam == null || _pool == null) return;

            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < PoolSize; i++)
            {
                ref Entry e = ref _pool[i];
                if (!e.Active) continue;
                e.Age += dt;
                if (e.Age >= Life)
                {
                    e.Active = false;
                    e.Text.gameObject.SetActive(false);
                    continue;
                }

                float t = e.Age / Life;
                Vector3 world = e.WorldPos + Vector3.up * (RiseMeters * t);
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z < 0f) { e.Text.gameObject.SetActive(false); e.Active = false; continue; }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 local);
                e.Rect.anchoredPosition = local;

                float pop = e.Age < 0.08f ? Mathf.Lerp(1.3f, 1f, e.Age / 0.08f) : 1f;
                e.Rect.localScale = Vector3.one * pop;

                Color c = e.Text.color;
                c.a = t > 0.7f ? 1f - (t - 0.7f) / 0.3f : 1f;
                e.Text.color = c;
            }
        }
    }
}
