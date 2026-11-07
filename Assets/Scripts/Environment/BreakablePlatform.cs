using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class BreakablePlatform : MonoBehaviour
    {
        [SerializeField] private Collider2D _solidCollider;
        [SerializeField] private SpriteRenderer[] _visuals;

        public bool IsBroken { get; private set; }

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
        }

        public void Configure(Collider2D solidCollider, SpriteRenderer[] visuals)
        {
            _solidCollider = solidCollider;
            _visuals = visuals;
        }

        public void Break()
        {
            SetBroken(true);
        }

        public void ResetPlatform()
        {
            SetBroken(false);
        }

        private void SetBroken(bool broken)
        {
            IsBroken = broken;
            if (_solidCollider != null)
            {
                _solidCollider.enabled = !broken;
            }

            if (_visuals == null)
            {
                return;
            }

            foreach (SpriteRenderer visual in _visuals)
            {
                visual.enabled = !broken;
            }
        }
    }
}
