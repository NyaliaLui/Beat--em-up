using UnityEngine;
using UnityEngine.UI;

namespace Ironhold
{
    /// <summary>
    /// Builds the on-screen controls (section 4) under its own RectTransform and wires each
    /// button to a named PlayerController method. Movement (LEFT/RIGHT) sits bottom-left,
    /// actions (PUNCH/BLOCK/SWORD) bottom-right. LEFT/RIGHT/BLOCK are holds; PUNCH/SWORD are taps.
    /// </summary>
    public class TouchInputUI : MonoBehaviour
    {
        private Image _punchImage;
        private Text _punchLabel;
        private bool _openMode;

        public void Build(PlayerController player, Sprite buttonSprite)
        {
            const float d = 150f;

            // Bottom-left: movement.
            var left = UIFactory.RoundButton("BtnLeft", transform, "<", 64,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(150f, 150f), d, buttonSprite, Color.white);
            left.OnDown = player.OnMoveLeftDown;
            left.OnUp = player.OnMoveLeftUp;

            var right = UIFactory.RoundButton("BtnRight", transform, ">", 64,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(320f, 150f), d, buttonSprite, Color.white);
            right.OnDown = player.OnMoveRightDown;
            right.OnUp = player.OnMoveRightUp;

            // Bottom-right: actions.
            var punch = UIFactory.RoundButton("BtnPunch", transform, "PUNCH", 30,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-150f, 120f), d, buttonSprite, Color.white);
            // In front of the hidden chest PUNCH becomes OPEN (see SecretChest / Update below).
            punch.OnDown = () =>
            {
                var chest = SecretChest.Current;
                if (chest != null) chest.Open();
                else player.OnPunch();
            };
            _punchImage = punch.GetComponent<Image>();
            _punchLabel = punch.GetComponentInChildren<Text>();

            var block = UIFactory.RoundButton("BtnBlock", transform, "BLOCK", 30,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-325f, 205f), d, buttonSprite, new Color(0.75f, 0.85f, 1f, 1f));
            block.OnDown = player.OnBlockDown;
            block.OnUp = player.OnBlockUp;

            var sword = UIFactory.RoundButton("BtnSword", transform, "SWORD", 30,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-150f, 300f), d, buttonSprite, GameConfig.EmberOrange);
            sword.OnDown = player.OnSword;

            // Dodge roll: below BLOCK, thumb-reachable next to PUNCH.
            var dodge = UIFactory.RoundButton("BtnDodge", transform, "ROLL", 30,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-325f, 80f), d, buttonSprite, new Color(0.7f, 1f, 0.75f, 1f));
            dodge.OnDown = player.OnDodge;
        }

        private void Update()
        {
            bool open = SecretChest.Current != null;
            if (open == _openMode || _punchLabel == null) return;
            _openMode = open;
            _punchLabel.text = open ? "OPEN" : "PUNCH";
            if (_punchImage != null && _punchImage.sprite != null)
            {
                // Keep TouchButton's current press alpha; only swap the tint.
                Color c = open ? GameConfig.ChestGold : Color.white;
                c.a = _punchImage.color.a;
                _punchImage.color = c;
            }
        }
    }
}
