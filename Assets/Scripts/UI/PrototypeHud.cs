using UnityEngine;

namespace MicroSense
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [Header("Ability Icons")]
        [SerializeField] private Sprite _sightIcon;
        [SerializeField] private Sprite _soundIcon;
        [SerializeField] private Sprite _tasteIcon;
        [SerializeField] private Sprite _touchIcon;

        private GUIStyle _panelStyle;
        private GUIStyle _playerStyle;
        private GUIStyle _centerStyle;
        private GUIStyle _objectiveStyle;
        private GUIStyle _gemStyle;
        private GUIStyle _completeStyle;
        private Texture2D _panelTexture;

        private void OnGUI()
        {
            if (_gameManager == null || _gameManager.Players == null || _gameManager.Players.Length < 2)
            {
                return;
            }

            EnsureStyles();
            Matrix4x4 previousGuiMatrix = ResponsiveGui.ApplyScale(out Vector2 canvasSize);

            const float margin = 14f;
            float topHeight = _gameManager.MaxAbilitiesPerPlayer > 1 || _gameManager.TotalGems > 0 ? 96f : 66f;
            float availableWidth = Mathf.Max(320f, canvasSize.x - margin * 2f);
            float playerWidth = Mathf.Clamp(availableWidth * 0.28f, 170f, 330f);
            float centerWidth = Mathf.Max(160f, availableWidth - playerWidth * 2f - margin * 2f);

            DrawPlayerPanel(new Rect(margin, margin, playerWidth, topHeight), _gameManager.Players[0]);
            DrawCenterPanel(new Rect(margin + playerWidth + margin, margin, centerWidth, topHeight));
            DrawPlayerPanel(
                new Rect(canvasSize.x - margin - playerWidth, margin, playerWidth, topHeight),
                _gameManager.Players[1]);

            GUI.Label(
                new Rect(margin, canvasSize.y - 38f, canvasSize.x - margin * 2f, 24f),
                "Reset [R]",
                _objectiveStyle);

            if (_gameManager.LevelComplete)
            {
                Rect overlay = new Rect(canvasSize.x * 0.5f - 230f, canvasSize.y * 0.5f - 72f, 460f, 144f);
                GUI.Box(overlay, GUIContent.none, _panelStyle);
                GUI.Label(new Rect(overlay.x + 20f, overlay.y + 24f, overlay.width - 40f, 48f), "LEVEL COMPLETE", _completeStyle);
                GUI.Label(new Rect(overlay.x + 20f, overlay.y + 82f, overlay.width - 40f, 28f), "Both cells reached the membrane gate", _objectiveStyle);
            }

            GUI.matrix = previousGuiMatrix;
        }

        private void DrawPlayerPanel(Rect rect, PlayerController player)
        {
            GUI.color = player.PlayerColor;
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 22f), player.PlayerName, _playerStyle);

            PlayerAbilityController abilities = player.AbilityController;
            DrawAbilitySlot(
                new Rect(rect.x + 14f, rect.y + 33f, rect.width - 28f, 26f),
                abilities.PrimaryAbility,
                player.InputBindings.AbilityPrimary,
                1,
                false);

            if (_gameManager.MaxAbilitiesPerPlayer > 1)
            {
                DrawAbilitySlot(
                    new Rect(rect.x + 14f, rect.y + 63f, rect.width - 28f, 26f),
                    abilities.SecondaryAbility,
                    player.InputBindings.AbilitySecondary,
                    2,
                    abilities.UnlockedAbilitySlots < 2);
            }
        }

        private void DrawAbilitySlot(
            Rect rect,
            AbilityType ability,
            UnityEngine.InputSystem.Key key,
            int slot,
            bool locked)
        {
            const float slotLabelWidth = 62f;
            GUI.Label(new Rect(rect.x, rect.y, slotLabelWidth, rect.height), $"SLOT\u00A0{slot}", _playerStyle);

            if (locked || ability == AbilityType.None)
            {
                string state = locked ? "LOCKED" : "EMPTY";
                GUI.Label(
                    new Rect(rect.x + slotLabelWidth, rect.y, rect.width - slotLabelWidth, rect.height),
                    $"{state} [{key}]",
                    _playerStyle);
                return;
            }

            Sprite icon = GetAbilityIcon(ability);
            if (icon == null)
            {
                GUI.Label(
                    new Rect(rect.x + slotLabelWidth, rect.y, rect.width - slotLabelWidth, rect.height),
                    $"{ability.ToString().ToUpperInvariant()} [{key}]",
                    _playerStyle);
                return;
            }

            Rect iconRect = new Rect(rect.x + slotLabelWidth, rect.y, rect.height, rect.height);
            DrawSprite(iconRect, icon);
            GUI.Label(
                new Rect(iconRect.xMax + 6f, rect.y, rect.xMax - iconRect.xMax - 6f, rect.height),
                $"[{key}]",
                _playerStyle);
        }

        private Sprite GetAbilityIcon(AbilityType ability)
        {
            switch (ability)
            {
                case AbilityType.Sight:
                    return _sightIcon;
                case AbilityType.Sound:
                    return _soundIcon;
                case AbilityType.Taste:
                    return _tasteIcon;
                case AbilityType.Touch:
                    return _touchIcon;
                default:
                    return null;
            }
        }

        private static void DrawSprite(Rect destination, Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect source = sprite.rect;
            Rect textureCoordinates = new Rect(
                source.x / texture.width,
                source.y / texture.height,
                source.width / texture.width,
                source.height / texture.height);

            float sourceAspect = source.width / source.height;
            float destinationAspect = destination.width / destination.height;
            if (destinationAspect > sourceAspect)
            {
                float width = destination.height * sourceAspect;
                destination.x += (destination.width - width) * 0.5f;
                destination.width = width;
            }
            else
            {
                float height = destination.width / sourceAspect;
                destination.y += (destination.height - height) * 0.5f;
                destination.height = height;
            }

            GUI.DrawTextureWithTexCoords(destination, texture, textureCoordinates, true);
        }

        private void DrawCenterPanel(Rect rect)
        {
            GUI.color = new Color(0.65f, 0.95f, 0.82f);
            GUI.Box(rect, GUIContent.none, _panelStyle);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 21f), _gameManager.LevelName, _centerStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 30f, rect.width - 20f, 21f), _gameManager.Objective, _objectiveStyle);
            if (_gameManager.TotalGems > 0)
            {
                GUI.Label(
                    new Rect(rect.x + 10f, rect.y + 54f, rect.width - 20f, 21f),
                    $"GEMS  {_gameManager.CollectedGems} / {_gameManager.TotalGems}",
                    _gemStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelTexture = new Texture2D(1, 1);
            _panelTexture.SetPixel(0, 0, new Color(0.035f, 0.055f, 0.08f, 0.9f));
            _panelTexture.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTexture }
            };
            _playerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _centerStyle = new GUIStyle(_playerStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15
            };
            _objectiveStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.82f, 0.9f, 0.93f) }
            };
            _gemStyle = new GUIStyle(_objectiveStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.86f, 1f) }
            };
            _completeStyle = new GUIStyle(_centerStyle)
            {
                fontSize = 28,
                normal = { textColor = new Color(0.65f, 1f, 0.82f) }
            };
        }

        public void Configure(
            GameManager gameManager,
            Sprite sightIcon = null,
            Sprite soundIcon = null,
            Sprite tasteIcon = null,
            Sprite touchIcon = null)
        {
            _gameManager = gameManager;

            if (sightIcon != null)
            {
                _sightIcon = sightIcon;
            }

            if (soundIcon != null)
            {
                _soundIcon = soundIcon;
            }

            if (tasteIcon != null)
            {
                _tasteIcon = tasteIcon;
            }

            if (touchIcon != null)
            {
                _touchIcon = touchIcon;
            }
        }

        private void OnDestroy()
        {
            if (_panelTexture != null)
            {
                Destroy(_panelTexture);
            }
        }
    }
}
