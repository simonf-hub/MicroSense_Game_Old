using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MicroSense
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerAbilityController : MonoBehaviour
    {
        [FormerlySerializedAs("_currentAbility")]
        [SerializeField] private AbilityType _primaryAbility;
        [SerializeField] private AbilityType _secondaryAbility;
        [SerializeField, Range(1, 2)] private int _unlockedAbilitySlots = 1;
        [SerializeField] private SightAbility _sight;
        [SerializeField] private SoundAbility _sound;
        [SerializeField] private TasteAbility _taste;
        [SerializeField] private TouchAbility _touch;

        private PlayerController _player;

        public event Action<AbilityType> AbilityChanged;

        public AbilityType CurrentAbility => _primaryAbility;
        public AbilityType PrimaryAbility => _primaryAbility;
        public AbilityType SecondaryAbility => _secondaryAbility;
        public int UnlockedAbilitySlots => _unlockedAbilitySlots;
        public int AbilityCount => (_primaryAbility != AbilityType.None ? 1 : 0)
            + (_secondaryAbility != AbilityType.None ? 1 : 0);

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _unlockedAbilitySlots = Mathf.Clamp(_unlockedAbilitySlots, 1, 2);
        }

        private void Update()
        {
            if (!_player.CanReceiveInput)
            {
                return;
            }

            if (_player.InputBindings.PrimaryPressedThisFrame)
            {
                ActivateAbility(_primaryAbility);
            }

            if (_unlockedAbilitySlots >= 2 && _player.InputBindings.SecondaryPressedThisFrame)
            {
                ActivateAbility(_secondaryAbility);
            }
        }

        public void Configure(
            SightAbility sight,
            SoundAbility sound,
            TasteAbility taste,
            TouchAbility touch)
        {
            _sight = sight;
            _sound = sound;
            _taste = taste;
            _touch = touch;
        }

        public bool TryAcquire(AbilityType ability)
        {
            if (ability == AbilityType.None || HasAbility(ability))
            {
                return false;
            }

            if (_primaryAbility == AbilityType.None)
            {
                _primaryAbility = ability;
                AbilityChanged?.Invoke(ability);
                return true;
            }

            if (_unlockedAbilitySlots < 2 || _secondaryAbility != AbilityType.None)
            {
                return false;
            }

            _secondaryAbility = ability;
            AbilityChanged?.Invoke(ability);
            return true;
        }

        public bool TryUnlockAbilitySlot(int maximumAbilitySlots)
        {
            int maximum = Mathf.Clamp(maximumAbilitySlots, 1, 2);
            if (_unlockedAbilitySlots >= maximum)
            {
                return false;
            }

            _unlockedAbilitySlots++;
            AbilityChanged?.Invoke(AbilityType.None);
            return true;
        }

        public bool HasAbility(AbilityType ability)
        {
            return ability != AbilityType.None
                && (_primaryAbility == ability || _secondaryAbility == ability);
        }

        public bool TryHandleJumpAction()
        {
            return HasAbility(AbilityType.Taste) && _taste.TryHandleJumpAction();
        }

        public void ResetForLevel()
        {
            ResetTemporaryStates();
            _primaryAbility = AbilityType.None;
            _secondaryAbility = AbilityType.None;
            _unlockedAbilitySlots = 1;
            AbilityChanged?.Invoke(AbilityType.None);
        }

        public void ResetTemporaryActionsOnly()
        {
            ResetTemporaryStates();
        }

        private void ActivateAbility(AbilityType ability)
        {
            switch (ability)
            {
                case AbilityType.Sight:
                    ResetTemporaryStates();
                    _sight.Activate();
                    break;
                case AbilityType.Sound:
                    ResetTemporaryStates();
                    _sound.Activate();
                    break;
                case AbilityType.Taste:
                    _touch.ResetState();
                    _taste.ToggleAiming();
                    break;
                case AbilityType.Touch:
                    _taste.ResetState();
                    _touch.ToggleClimb();
                    break;
            }
        }

        private void ResetTemporaryStates()
        {
            _taste.ResetState();
            _touch.ResetState();
        }

        private void OnDisable()
        {
            if (_taste != null)
            {
                _taste.ResetState();
            }

            if (_touch != null)
            {
                _touch.ResetState();
            }
        }
    }
}
