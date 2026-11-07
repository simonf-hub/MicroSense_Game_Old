using System.Collections.Generic;
using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PressureButton : MonoBehaviour
    {
        [SerializeField] private LinkedDoor _linkedDoor;
        [SerializeField] private Transform _plateVisual;
        [SerializeField] private SpriteRenderer _indicator;
        [SerializeField] private Vector3 _depressedLocalPosition = new Vector3(0f, -0.12f, 0f);
        [SerializeField] private Color _pressedColor = new Color(0.55f, 1f, 0.72f);

        private readonly HashSet<PlayerController> _playersOnButton = new HashSet<PlayerController>();
        private Vector3 _releasedLocalPosition;
        private Color _releasedColor;

        public bool IsPressed => _playersOnButton.Count > 0;
        public LinkedDoor LinkedDoor => _linkedDoor;

        private void Awake()
        {
            if (_plateVisual != null)
            {
                _releasedLocalPosition = _plateVisual.localPosition;
            }

            if (_indicator != null)
            {
                _releasedColor = _indicator.color;
            }

            ApplyVisualState();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && _playersOnButton.Add(player))
            {
                ApplyPressedState();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && _playersOnButton.Remove(player))
            {
                ApplyPressedState();
            }
        }

        public void Configure(
            LinkedDoor linkedDoor,
            Transform plateVisual,
            SpriteRenderer indicator,
            Vector3 depressedLocalPosition)
        {
            _linkedDoor = linkedDoor;
            _plateVisual = plateVisual;
            _indicator = indicator;
            _depressedLocalPosition = depressedLocalPosition;

            if (_plateVisual != null)
            {
                _releasedLocalPosition = _plateVisual.localPosition;
            }

            if (_indicator != null)
            {
                _releasedColor = _indicator.color;
            }
        }

        public void ResetButton()
        {
            _playersOnButton.Clear();
            ApplyPressedState();
        }

        private void ApplyPressedState()
        {
            _linkedDoor?.SetButtonPressed(this, IsPressed);
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_plateVisual != null)
            {
                _plateVisual.localPosition = IsPressed
                    ? _releasedLocalPosition + _depressedLocalPosition
                    : _releasedLocalPosition;
            }

            if (_indicator != null)
            {
                _indicator.color = IsPressed ? _pressedColor : _releasedColor;
            }
        }

        private void OnDisable()
        {
            if (_playersOnButton.Count == 0)
            {
                return;
            }

            _playersOnButton.Clear();
            ApplyPressedState();
        }
    }
}
