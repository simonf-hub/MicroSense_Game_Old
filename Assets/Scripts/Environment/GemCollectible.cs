using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class GemCollectible : MonoBehaviour
    {
        private Collider2D _trigger;
        private SpriteRenderer[] _visuals;

        public bool IsCollected { get; private set; }

        private void Awake()
        {
            _trigger = GetComponent<Collider2D>();
            _visuals = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCollected || other.GetComponentInParent<PlayerController>() == null)
            {
                return;
            }

            SetCollected(true);
        }

        public void ResetGem()
        {
            SetCollected(false);
        }

        private void SetCollected(bool collected)
        {
            IsCollected = collected;
            if (_trigger != null)
            {
                _trigger.enabled = !collected;
            }

            if (_visuals == null)
            {
                return;
            }

            foreach (SpriteRenderer visual in _visuals)
            {
                visual.enabled = !collected;
            }
        }
    }
}
