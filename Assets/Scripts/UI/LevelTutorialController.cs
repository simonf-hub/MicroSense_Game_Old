using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace MicroSense
{
    public sealed class LevelTutorialController : MonoBehaviour
    {
        private enum TutorialLevel
        {
            LevelOne,
            LevelTwo,
            LevelThree
        }

        private readonly struct TutorialMessage
        {
            public TutorialMessage(
                string section,
                string title,
                string body,
                Color accent,
                VideoClip video)
            {
                Section = section;
                Title = title;
                Body = body;
                Accent = accent;
                Video = video;
            }

            public string Section { get; }
            public string Title { get; }
            public string Body { get; }
            public Color Accent { get; }
            public VideoClip Video { get; }
        }

        [Header("Level")]
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private TutorialLevel _level;
        [SerializeField] private bool _pauseWhileOpen = true;

        [Header("Guide Videos")]
        [SerializeField] private VideoClip _movementGuide;
        [SerializeField] private VideoClip _sightGuide;
        [SerializeField] private VideoClip _soundGuide;
        [SerializeField] private VideoClip _tasteGuide;
        [SerializeField] private VideoClip _touchGuide;

        private readonly Queue<TutorialMessage> _pendingMessages = new Queue<TutorialMessage>();
        private readonly HashSet<AbilityType> _explainedAbilities = new HashSet<AbilityType>();
        private readonly Dictionary<PlayerAbilityController, Action<AbilityType>> _abilityHandlers =
            new Dictionary<PlayerAbilityController, Action<AbilityType>>();
        private readonly List<AbilitySlotUpgrade> _subscribedUpgrades = new List<AbilitySlotUpgrade>();

        private TutorialMessage _currentMessage;
        private VideoPlayer _videoPlayer;
        private RenderTexture _videoTexture;
        private bool _isOpen;
        private bool _upgradeExplained;
        private bool _pausedByTutorial;
        private float _timeScaleBeforePopup = 1f;
        private Texture2D _overlayTexture;
        private Texture2D _panelTexture;
        private Texture2D _buttonTexture;
        private Texture2D _videoBackgroundTexture;
        private GUIStyle _panelStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _loadingStyle;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = GetComponent<GameManager>();
            }

            ConfigureVideoPlayer();
        }

        private void Start()
        {
            if (_gameManager == null || _gameManager.Players == null)
            {
                enabled = false;
                return;
            }

            SubscribeToAbilities();
            SubscribeToUpgrades();
            ResetTutorial();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetTutorial();
                return;
            }

            if (_isOpen
                && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.spaceKey.wasPressedThisFrame))
            {
                CloseCurrentMessage();
            }
        }

        private void ConfigureVideoPlayer()
        {
            _videoTexture = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32)
            {
                name = "Tutorial Video"
            };
            _videoTexture.Create();

            _videoPlayer = gameObject.AddComponent<VideoPlayer>();
            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = true;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.skipOnDrop = true;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _videoTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            _videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            _videoPlayer.prepareCompleted += HandleVideoPrepared;
        }

        private void SubscribeToAbilities()
        {
            foreach (PlayerController player in _gameManager.Players)
            {
                if (player == null || player.AbilityController == null)
                {
                    continue;
                }

                PlayerController subscribedPlayer = player;
                Action<AbilityType> handler = ability => HandleAbilityAcquired(subscribedPlayer, ability);
                player.AbilityController.AbilityChanged += handler;
                _abilityHandlers[player.AbilityController] = handler;
            }
        }

        private void SubscribeToUpgrades()
        {
            if (_level != TutorialLevel.LevelThree || _gameManager.AbilitySlotUpgrades == null)
            {
                return;
            }

            foreach (AbilitySlotUpgrade upgrade in _gameManager.AbilitySlotUpgrades)
            {
                if (upgrade == null)
                {
                    continue;
                }

                upgrade.Collected += HandleUpgradeCollected;
                _subscribedUpgrades.Add(upgrade);
            }
        }

        public void ResetTutorial()
        {
            _pendingMessages.Clear();
            _explainedAbilities.Clear();
            _upgradeExplained = false;
            ClosePopup();

            if (_level == TutorialLevel.LevelOne)
            {
                EnqueueMovementMessage();
            }
        }

        private void EnqueueMovementMessage()
        {
            PlayerController[] players = _gameManager.Players;
            if (players.Length < 2 || players[0] == null || players[1] == null)
            {
                return;
            }

            string body =
                "Both cells must reach the membrane gate together.\n\n"
                + FormatMovementLine(players[0])
                + "\n"
                + FormatMovementLine(players[1])
                + "\n\nExplore both sides of the cavern. The camera keeps both players in view.";

            EnqueueMessage(new TutorialMessage(
                "LEVEL 1  /  MOVEMENT",
                "Move Together",
                body,
                new Color(0.35f, 0.92f, 0.82f),
                _movementGuide));
        }

        private void HandleAbilityAcquired(PlayerController player, AbilityType ability)
        {
            if (!ShouldExplain(ability) || !_explainedAbilities.Add(ability))
            {
                return;
            }

            PlayerAbilityController abilities = player.AbilityController;
            bool isPrimary = abilities.PrimaryAbility == ability;
            Key abilityKey = isPrimary
                ? player.InputBindings.AbilityPrimary
                : player.InputBindings.AbilitySecondary;
            string slotName = isPrimary ? "SLOT 1" : "SLOT 2";

            switch (ability)
            {
                case AbilityType.Sight:
                    EnqueueSightMessage(player, slotName, abilityKey);
                    break;
                case AbilityType.Sound:
                    EnqueueSoundMessage(player, slotName, abilityKey);
                    break;
                case AbilityType.Taste:
                    EnqueueTasteMessage(player, slotName, abilityKey);
                    break;
                case AbilityType.Touch:
                    EnqueueTouchMessage(player, slotName, abilityKey);
                    break;
            }
        }

        private bool ShouldExplain(AbilityType ability)
        {
            switch (_level)
            {
                case TutorialLevel.LevelOne:
                    return ability == AbilityType.Sight || ability == AbilityType.Sound;
                case TutorialLevel.LevelTwo:
                case TutorialLevel.LevelThree:
                    return ability == AbilityType.Taste || ability == AbilityType.Touch;
                default:
                    return false;
            }
        }

        private void EnqueueSightMessage(PlayerController player, string slotName, Key abilityKey)
        {
            EnqueueMessage(new TutorialMessage(
                "ABILITY FOUND  /  SIGHT",
                "Reveal Hidden Paths",
                $"{player.PlayerName} absorbed SIGHT into {slotName}.\n\n"
                + $"Face the direction you want to scan, then press [{FormatKey(abilityKey)}].\n\n"
                + "Sight reveals hidden tissue platforms ahead. Revealed platforms become visible and solid so both players can cross.",
                new Color(0.55f, 1f, 0.78f),
                _sightGuide));
        }

        private void EnqueueSoundMessage(PlayerController player, string slotName, Key abilityKey)
        {
            EnqueueMessage(new TutorialMessage(
                "ABILITY FOUND  /  SOUND",
                "Break Calcified Barriers",
                $"{player.PlayerName} absorbed SOUND into {slotName}.\n\n"
                + $"Press [{FormatKey(abilityKey)}] to release a sound pulse.\n\n"
                + "Sound breaks nearby calcified barriers. Move close to a cracked barrier before releasing the pulse.",
                new Color(1f, 0.8f, 0.3f),
                _soundGuide));
        }

        private void EnqueueTasteMessage(PlayerController player, string slotName, Key abilityKey)
        {
            PlayerInputBindings input = player.InputBindings;
            EnqueueMessage(new TutorialMessage(
                "ABILITY FOUND  /  TASTE",
                "Carry and Launch Your Teammate",
                $"{player.PlayerName} absorbed TASTE into {slotName}.\n\n"
                + $"Stand near your teammate and press [{FormatKey(abilityKey)}] to aim. "
                + $"Press [{FormatKey(input.Up)}] to carry them.\n\n"
                + $"Adjust the launch angle with [{FormatKey(input.Left)}] and [{FormatKey(input.Right)}], "
                + $"then press [{FormatKey(input.Up)}] again to launch.",
                new Color(1f, 0.43f, 0.58f),
                _tasteGuide));
        }

        private void EnqueueTouchMessage(PlayerController player, string slotName, Key abilityKey)
        {
            PlayerInputBindings input = player.InputBindings;
            EnqueueMessage(new TutorialMessage(
                "ABILITY FOUND  /  TOUCH",
                "Climb Living Walls",
                $"{player.PlayerName} absorbed TOUCH into {slotName}.\n\n"
                + $"Move beside a climbable tissue wall and press [{FormatKey(abilityKey)}] to attach.\n\n"
                + $"Climb with [{FormatKey(input.Up)}] and [{FormatKey(input.Down)}]. "
                + $"Press [{FormatKey(abilityKey)}] again to let go.",
                new Color(0.72f, 0.55f, 1f),
                _touchGuide));
        }

        private void HandleUpgradeCollected(PlayerAbilityController abilities)
        {
            if (_upgradeExplained || abilities == null)
            {
                return;
            }

            PlayerController player = abilities.GetComponent<PlayerController>();
            if (player == null)
            {
                return;
            }

            _upgradeExplained = true;
            EnqueueMessage(new TutorialMessage(
                "ABILITY SLOT UPGRADE",
                "A Second Ability Slot",
                $"{player.PlayerName} unlocked SLOT 2.\n\n"
                + "This player can now collect one additional ability without replacing Slot 1.\n\n"
                + $"Use [{FormatKey(player.InputBindings.AbilitySecondary)}] to activate the ability stored in SLOT 2. "
                + $"SLOT 1 still uses [{FormatKey(player.InputBindings.AbilityPrimary)}].",
                new Color(0.35f, 0.88f, 1f),
                null));
        }

        private static string FormatMovementLine(PlayerController player)
        {
            PlayerInputBindings input = player.InputBindings;
            return $"{player.PlayerName}: move [{FormatKey(input.Left)}] [{FormatKey(input.Right)}], "
                + $"jump [{FormatKey(input.Up)}]";
        }

        private static string FormatKey(Key key)
        {
            switch (key)
            {
                case Key.LeftArrow:
                    return "Left Arrow";
                case Key.RightArrow:
                    return "Right Arrow";
                case Key.UpArrow:
                    return "Up Arrow";
                case Key.DownArrow:
                    return "Down Arrow";
                default:
                    return key.ToString();
            }
        }

        private void EnqueueMessage(TutorialMessage message)
        {
            _pendingMessages.Enqueue(message);
            if (!_isOpen)
            {
                ShowNextMessage();
            }
        }

        private void ShowNextMessage()
        {
            if (_pendingMessages.Count == 0)
            {
                ClosePopup();
                return;
            }

            _currentMessage = _pendingMessages.Dequeue();
            _isOpen = true;
            PlayVideo(_currentMessage.Video);

            if (_pauseWhileOpen && !_pausedByTutorial)
            {
                _timeScaleBeforePopup = Time.timeScale;
                Time.timeScale = 0f;
                _pausedByTutorial = true;
            }
        }

        private void PlayVideo(VideoClip clip)
        {
            _videoPlayer.Stop();
            _videoPlayer.clip = clip;
            if (clip != null)
            {
                _videoPlayer.Prepare();
            }
        }

        private void HandleVideoPrepared(VideoPlayer source)
        {
            if (_isOpen && _currentMessage.Video != null && source.clip == _currentMessage.Video)
            {
                source.Play();
            }
        }

        private void CloseCurrentMessage()
        {
            _isOpen = false;
            ShowNextMessage();
        }

        private void ClosePopup()
        {
            _isOpen = false;
            if (_videoPlayer != null)
            {
                _videoPlayer.Stop();
                _videoPlayer.clip = null;
            }

            if (_pausedByTutorial)
            {
                Time.timeScale = _timeScaleBeforePopup;
                _pausedByTutorial = false;
            }
        }

        private void OnGUI()
        {
            if (!_isOpen)
            {
                return;
            }

            EnsureStyles();
            Matrix4x4 previousGuiMatrix = ResponsiveGui.ApplyScale(out Vector2 canvasSize);
            GUI.depth = -100;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, canvasSize.x, canvasSize.y), _overlayTexture);

            bool hasVideo = _currentMessage.Video != null;
            float panelWidth = Mathf.Min(hasVideo ? 960f : 680f, Mathf.Max(440f, canvasSize.x - 32f));
            float panelHeight = Mathf.Min(410f, Mathf.Max(300f, canvasSize.y - 32f));
            Rect panel = new Rect(
                (canvasSize.x - panelWidth) * 0.5f,
                (canvasSize.y - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panel, GUIContent.none, _panelStyle);
            GUI.color = _currentMessage.Accent;
            GUI.DrawTexture(new Rect(panel.x, panel.y, 7f, panel.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float contentX = panel.x + 34f;
            float contentWidth = panel.width - 68f;
            float textWidth = contentWidth;
            if (hasVideo)
            {
                float videoWidth = Mathf.Min(390f, contentWidth * 0.46f);
                float videoHeight = videoWidth * 9f / 16f;
                Rect videoFrame = new Rect(
                    panel.xMax - 34f - videoWidth,
                    panel.y + 108f,
                    videoWidth,
                    videoHeight);
                GUI.DrawTexture(videoFrame, _videoBackgroundTexture);
                GUI.DrawTexture(videoFrame, _videoTexture, ScaleMode.ScaleToFit, false);
                if (!_videoPlayer.isPrepared)
                {
                    GUI.Label(videoFrame, "Loading guide...", _loadingStyle);
                }

                textWidth = contentWidth - videoWidth - 28f;
            }

            GUI.Label(new Rect(contentX, panel.y + 25f, contentWidth, 22f), _currentMessage.Section, _sectionStyle);
            GUI.Label(new Rect(contentX, panel.y + 52f, contentWidth, 42f), _currentMessage.Title, _titleStyle);
            GUI.Label(new Rect(contentX, panel.y + 108f, textWidth, panel.height - 180f), _currentMessage.Body, _bodyStyle);

            Rect button = new Rect(panel.xMax - 154f, panel.yMax - 56f, 120f, 34f);
            if (GUI.Button(button, "CONTINUE", _buttonStyle))
            {
                CloseCurrentMessage();
            }

            GUI.Label(
                new Rect(contentX, panel.yMax - 57f, contentWidth - 145f, 34f),
                "Press Enter or Space",
                _hintStyle);

            GUI.matrix = previousGuiMatrix;
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _overlayTexture = CreateTexture(new Color(0.01f, 0.018f, 0.025f, 0.76f));
            _panelTexture = CreateTexture(new Color(0.035f, 0.055f, 0.08f, 0.98f));
            _buttonTexture = CreateTexture(new Color(0.16f, 0.38f, 0.38f, 1f));
            _videoBackgroundTexture = CreateTexture(new Color(0.008f, 0.012f, 0.016f, 1f));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTexture }
            };
            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.55f, 0.78f, 0.78f) }
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.86f, 0.92f, 0.94f) }
            };
            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.58f, 0.68f, 0.72f) }
            };
            _loadingStyle = new GUIStyle(_hintStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    background = _buttonTexture,
                    textColor = Color.white
                },
                hover =
                {
                    background = _buttonTexture,
                    textColor = Color.white
                },
                active =
                {
                    background = _buttonTexture,
                    textColor = Color.white
                }
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<PlayerAbilityController, Action<AbilityType>> subscription in _abilityHandlers)
            {
                if (subscription.Key != null)
                {
                    subscription.Key.AbilityChanged -= subscription.Value;
                }
            }

            foreach (AbilitySlotUpgrade upgrade in _subscribedUpgrades)
            {
                if (upgrade != null)
                {
                    upgrade.Collected -= HandleUpgradeCollected;
                }
            }

            if (_videoPlayer != null)
            {
                _videoPlayer.prepareCompleted -= HandleVideoPrepared;
                _videoPlayer.targetTexture = null;
            }

            ClosePopup();
            if (_videoTexture != null)
            {
                _videoTexture.Release();
                Destroy(_videoTexture);
            }

            DestroyTexture(_overlayTexture);
            DestroyTexture(_panelTexture);
            DestroyTexture(_buttonTexture);
            DestroyTexture(_videoBackgroundTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
