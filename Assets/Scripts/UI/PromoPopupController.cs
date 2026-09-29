using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Easter-egg reward popup: a stone panel with a headline, a short pitch and a button that
    /// opens one of the artist's "fanbase endpoints" (Patreon, merch store, comic release) in the
    /// device browser. The fight is frozen underneath (GameState.Promo) until the player either
    /// follows the link or dismisses it; both close the popup and resume the run.
    /// </summary>
    public class PromoPopupController : MonoBehaviour
    {
        public struct Promo
        {
            public string Title;
            public string Body;
            public string LinkLabel;
            public string Url;

            public Promo(string title, string body, string linkLabel, string url)
            {
                Title = title; Body = body; LinkLabel = linkLabel; Url = url;
            }
        }

        private RectTransform _root;
        private RectTransform _card;
        private Text _title, _body, _linkLabel;
        private Promo _current;
        private float _popT;

        public void Build(Transform canvas)
        {
            _root = UIFactory.FullScreen("Promo", canvas);
            UIFactory.Image("PromoScrim", _root, null, new Color(0f, 0f, 0f, 0.72f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, raycast: true)
                .rectTransform.offsetMax = Vector2.zero;

            // Flat night-blue card: the stone panel sprite is framed for small menus and stretches badly here.
            var card = UIFactory.Image("PromoCard", _root, null, new Color(0.07f, 0.09f, 0.14f, 0.97f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 640),
                raycast: true);
            _card = card.rectTransform;

            // Gold trim along the top edge so the card reads as a reward, not a pause menu.
            UIFactory.Image("PromoTrim", _card, null, GameConfig.ChestGold,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f));
            UIFactory.Image("PromoTrimLow", _card, null, GameConfig.ChestGold,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 4f));

            _title = UIFactory.Label("PromoTitle", _card, "", 64, TextAnchor.MiddleCenter, GameConfig.ChestGold,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1080, 90));

            _body = UIFactory.Label("PromoBody", _card, "", 34, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 45), new Vector2(1020, 260));
            _body.horizontalOverflow = HorizontalWrapMode.Wrap;
            _body.lineSpacing = 1.1f;

            var link = UIFactory.PanelButton("PromoLink", _card, "", 40,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-230, 60), new Vector2(520, 110), null);
            link.GetComponent<Image>().color = GameConfig.ChestGold;
            _linkLabel = link.GetComponentInChildren<Text>();
            _linkLabel.color = GameConfig.NightBlue;
            link.OnDown = OpenLink;

            var later = UIFactory.PanelButton("PromoLater", _card, "MAYBE LATER", 34,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(300, 60), new Vector2(380, 110), null);
            later.OnDown = Close;

            var gm = GameManager.Instance;
            if (gm != null) gm.StateChanged += OnStateChanged;
            _root.gameObject.SetActive(false);
        }

        /// <summary>Called by GameManager.OpenPromo once the fight is frozen.</summary>
        public void Show(Promo promo)
        {
            _current = promo;
            _title.text = promo.Title;
            _body.text = promo.Body;
            _linkLabel.text = promo.LinkLabel;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling(); // above HUD, damage numbers and every other overlay
            _popT = 0f;
            GameManager.Instance?.Sfx?.Play(SfxManager.ComboUp);
        }

        private void OpenLink()
        {
            GameManager.Instance?.Sfx?.Play(SfxManager.UiButton);
            if (!string.IsNullOrEmpty(_current.Url)) Application.OpenURL(_current.Url);
            Close();
        }

        private void Close()
        {
            GameManager.Instance?.ClosePromo();
        }

        private void OnStateChanged(GameState s)
        {
            if (s != GameState.Promo && _root != null) _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_card == null || !_root.gameObject.activeSelf || _popT >= 1f) return;
            // Unscaled pop-in (the game is frozen at timeScale 0 underneath).
            _popT = Mathf.Min(1f, _popT + Time.unscaledDeltaTime / 0.25f);
            float k = 1f - Mathf.Pow(1f - _popT, 3f);
            _card.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
        }
    }
}
