using System;
using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class AbilitySlotUpgrade : MonoBehaviour
    {
        private Collider2D _collider;
        private SpriteRenderer[] _renderers;
        private bool _collected;

        public event Action<PlayerAbilityController> Collected;

        public bool IsCollected => _collected;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected)
            {
                return;
            }

            PlayerAbilityController player = other.GetComponentInParent<PlayerAbilityController>();
            GameManager manager = GameManager.Instance;
            if (player != null && manager != null && manager.TryUnlockAbilitySlot(player))
            {
                SetCollected(true);
                Collected?.Invoke(player);
            }
        }

        public void ResetUpgrade()
        {
            SetCollected(false);
        }

        private void SetCollected(bool collected)
        {
            _collected = collected;
            if (_collider != null)
            {
                _collider.enabled = !collected;
            }

            if (_renderers == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in _renderers)
            {
                renderer.enabled = !collected;
            }
        }
    }
}
