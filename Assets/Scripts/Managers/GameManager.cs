using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MicroSense
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField, Range(1, 2)] private int _maxAbilitiesPerPlayer = 1;
        [SerializeField] private string _levelName = "01 - Burning Throat Caverns";
        [SerializeField] private string _objective = "Reach the membrane gate together";
        [SerializeField] private string _nextSceneName;
        [SerializeField, Min(0f)] private float _levelTransitionDelay = 1.5f;
        [SerializeField] private PlayerController[] _players;
        [SerializeField] private AbilityPickup[] _pickups;
        [SerializeField] private AbilitySlotUpgrade[] _abilitySlotUpgrades;
        [SerializeField] private GemCollectible[] _gems;
        [SerializeField] private HiddenPlatform[] _hiddenPlatforms;
        [SerializeField] private BreakablePlatform[] _breakables;
        [SerializeField] private PressureButton[] _pressureButtons;
        [SerializeField] private LinkedDoor[] _linkedDoors;
        [SerializeField] private LevelExit _levelExit;
        [SerializeField, Min(0f)] private float _fallResetHeight = -10f;

        private Vector3[] _spawnPositions;
        private Coroutine _levelTransition;

        public static GameManager Instance { get; private set; }

        public int MaxAbilitiesPerPlayer => Mathf.Clamp(_maxAbilitiesPerPlayer, 1, 2);
        public string LevelName => _levelName;
        public string Objective => _objective;
        public bool LevelComplete { get; private set; }
        public PlayerController[] Players => _players;
        public AbilitySlotUpgrade[] AbilitySlotUpgrades => _abilitySlotUpgrades;
        public int TotalGems
        {
            get
            {
                int total = 0;
                if (_gems == null)
                {
                    return total;
                }

                foreach (GemCollectible gem in _gems)
                {
                    if (gem != null)
                    {
                        total++;
                    }
                }

                return total;
            }
        }
        public int CollectedGems
        {
            get
            {
                int collected = 0;
                if (_gems == null)
                {
                    return collected;
                }

                foreach (GemCollectible gem in _gems)
                {
                    if (gem != null && gem.IsCollected)
                    {
                        collected++;
                    }
                }

                return collected;
            }
        }
        public bool AllPlayersHaveFirstAbility
        {
            get
            {
                if (_players == null || _players.Length == 0)
                {
                    return false;
                }

                foreach (PlayerController player in _players)
                {
                    if (player == null || player.AbilityController.AbilityCount == 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _maxAbilitiesPerPlayer = Mathf.Clamp(_maxAbilitiesPerPlayer, 1, 2);
            if (_gems == null || _gems.Length == 0)
            {
                _gems = FindObjectsByType<GemCollectible>();
            }

            _spawnPositions = new Vector3[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                _spawnPositions[i] = _players[i].transform.position;
            }
        }

        private void Start()
        {
            ResetLevel();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetLevel();
                return;
            }

            foreach (PlayerController player in _players)
            {
                if (player.transform.position.y < _fallResetHeight)
                {
                    ResetLevel();
                    break;
                }
            }
        }

        public void Configure(
            int maxAbilitiesPerPlayer,
            string levelName,
            string objective,
            PlayerController[] players,
            AbilityPickup[] pickups,
            AbilitySlotUpgrade[] abilitySlotUpgrades,
            GemCollectible[] gems,
            HiddenPlatform[] hiddenPlatforms,
            BreakablePlatform[] breakables,
            PressureButton[] pressureButtons,
            LinkedDoor[] linkedDoors,
            LevelExit levelExit)
        {
            _maxAbilitiesPerPlayer = Mathf.Clamp(maxAbilitiesPerPlayer, 1, 2);
            _levelName = levelName;
            _objective = objective;
            _players = players;
            _pickups = pickups;
            _abilitySlotUpgrades = abilitySlotUpgrades;
            _gems = gems;
            _hiddenPlatforms = hiddenPlatforms;
            _breakables = breakables;
            _pressureButtons = pressureButtons;
            _linkedDoors = linkedDoors;
            _levelExit = levelExit;
        }

        public bool TryUnlockAbilitySlot(PlayerAbilityController player)
        {
            return player != null
                && AllPlayersHaveFirstAbility
                && player.TryUnlockAbilitySlot(MaxAbilitiesPerPlayer);
        }

        public void CompleteLevel()
        {
            if (LevelComplete)
            {
                return;
            }

            LevelComplete = true;
            foreach (PlayerController player in _players)
            {
                player.Body.linearVelocity = Vector2.zero;
            }

            if (!string.IsNullOrWhiteSpace(_nextSceneName))
            {
                _levelTransition = StartCoroutine(LoadNextLevel());
            }
        }

        public void ResetLevel()
        {
            if (_levelTransition != null)
            {
                StopCoroutine(_levelTransition);
                _levelTransition = null;
            }

            LevelComplete = false;

            for (int i = 0; i < _players.Length; i++)
            {
                _players[i].AbilityController.ResetForLevel();
                _players[i].ResetPlayer(_spawnPositions[i]);
            }

            if (_pickups != null)
            {
                foreach (AbilityPickup pickup in _pickups)
                {
                    if (pickup != null)
                    {
                        pickup.ResetPickup();
                    }
                }
            }

            if (_abilitySlotUpgrades != null)
            {
                foreach (AbilitySlotUpgrade upgrade in _abilitySlotUpgrades)
                {
                    if (upgrade != null)
                    {
                        upgrade.ResetUpgrade();
                    }
                }
            }

            if (_gems != null)
            {
                foreach (GemCollectible gem in _gems)
                {
                    if (gem != null)
                    {
                        gem.ResetGem();
                    }
                }
            }

            if (_hiddenPlatforms != null)
            {
                foreach (HiddenPlatform platform in _hiddenPlatforms)
                {
                    if (platform != null)
                    {
                        platform.ResetPlatform();
                    }
                }
            }

            if (_breakables != null)
            {
                foreach (BreakablePlatform breakable in _breakables)
                {
                    if (breakable != null)
                    {
                        breakable.ResetPlatform();
                    }
                }
            }

            if (_pressureButtons != null)
            {
                foreach (PressureButton button in _pressureButtons)
                {
                    if (button != null)
                    {
                        button.ResetButton();
                    }
                }
            }

            if (_linkedDoors != null)
            {
                foreach (LinkedDoor door in _linkedDoors)
                {
                    if (door != null)
                    {
                        door.ResetDoor();
                    }
                }
            }

            if (_levelExit != null)
            {
                _levelExit.ResetExit();
            }
        }

        private IEnumerator LoadNextLevel()
        {
            yield return new WaitForSecondsRealtime(_levelTransitionDelay);

            if (!Application.CanStreamedLevelBeLoaded(_nextSceneName))
            {
                Debug.LogError($"Cannot load next level '{_nextSceneName}'. Add it to Build Settings.", this);
                _levelTransition = null;
                yield break;
            }

            SceneManager.LoadScene(_nextSceneName);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
