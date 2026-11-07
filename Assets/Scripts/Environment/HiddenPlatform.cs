using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class HiddenPlatform : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _visual;
        [SerializeField] private Collider2D _platformCollider;
        [SerializeField, Range(0f, 1f)] private float _hiddenAlpha;

        private Color _revealedColor;

        public bool IsRevealed { get; private set; }
        public bool IsSolid => _platformCollider != null && !_platformCollider.isTrigger;

        private void Awake()
        {
            if (_visual == null)
            {
                _visual = GetComponent<SpriteRenderer>();
            }

            if (_platformCollider == null)
            {
                _platformCollider = GetComponent<Collider2D>();
            }

            _revealedColor = _visual.color;
            ApplyState();
        }

        public void Configure(SpriteRenderer visual, float hiddenAlpha = 0f)
        {
            _visual = visual;
            _platformCollider = GetComponent<Collider2D>();
            _hiddenAlpha = hiddenAlpha;
            _revealedColor = _visual.color;
        }

        public void Reveal()
        {
            IsRevealed = true;
            ApplyState();
        }

        public void ResetPlatform()
        {
            IsRevealed = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (_visual != null)
            {
                Color color = _revealedColor;
                color.a = IsRevealed ? _revealedColor.a : _hiddenAlpha;
                _visual.color = color;
            }

            if (_platformCollider != null)
            {
                // The trigger remains raycast-detectable while players pass through it.
                _platformCollider.enabled = true;
                _platformCollider.isTrigger = !IsRevealed;
            }
        }
    }
}
