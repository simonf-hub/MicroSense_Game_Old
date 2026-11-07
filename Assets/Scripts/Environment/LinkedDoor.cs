using System.Collections.Generic;
using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class LinkedDoor : MonoBehaviour
    {
        [SerializeField] private Collider2D _solidCollider;
        [SerializeField] private SpriteRenderer[] _visuals;
        [SerializeField, Range(0f, 1f)] private float _openAlpha = 0.16f;

        private readonly HashSet<PressureButton> _activeButtons = new HashSet<PressureButton>();
        private Color[] _closedColors;

        public bool IsOpen => _activeButtons.Count > 0;

        private void Awake()
        {
            if (_solidCollider == null)
            {
                _solidCollider = GetComponent<Collider2D>();
            }

            if (_visuals == null || _visuals.Length == 0)
            {
                _visuals = GetComponentsInChildren<SpriteRenderer>(true);
            }

            CacheClosedColors();
            ApplyState();
        }

        public void Configure(Collider2D solidCollider, SpriteRenderer[] visuals, float openAlpha = 0.16f)
        {
            _solidCollider = solidCollider;
            _visuals = visuals;
            _openAlpha = Mathf.Clamp01(openAlpha);
            CacheClosedColors();
        }

        public void SetButtonPressed(PressureButton source, bool pressed)
        {
            if (source == null)
            {
                return;
            }

            bool changed = pressed ? _activeButtons.Add(source) : _activeButtons.Remove(source);
            if (changed)
            {
                ApplyState();
            }
        }

        public void ResetDoor()
        {
            _activeButtons.Clear();
            ApplyState();
        }

        private void CacheClosedColors()
        {
            if (_visuals == null)
            {
                _closedColors = null;
                return;
            }

            _closedColors = new Color[_visuals.Length];
            for (int i = 0; i < _visuals.Length; i++)
            {
                _closedColors[i] = _visuals[i] != null ? _visuals[i].color : Color.white;
            }
        }

        private void ApplyState()
        {
            bool open = IsOpen;
            if (_solidCollider != null)
            {
                _solidCollider.enabled = !open;
            }

            if (_visuals == null)
            {
                return;
            }

            if (_closedColors == null || _closedColors.Length != _visuals.Length)
            {
                CacheClosedColors();
            }

            for (int i = 0; i < _visuals.Length; i++)
            {
                if (_visuals[i] == null)
                {
                    continue;
                }

                Color color = _closedColors[i];
                color.a = open ? color.a * _openAlpha : color.a;
                _visuals[i].color = color;
            }
        }
    }
}
