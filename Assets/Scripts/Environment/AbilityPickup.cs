using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public sealed class AbilityPickup : MonoBehaviour
    {
        [SerializeField] private AbilityType _ability = AbilityType.Sight;

        private Collider2D _collider;
        private SpriteRenderer[] _renderers;
        private bool _collected;

        public AbilityType Ability => _ability;
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
            if (player != null && player.TryAcquire(_ability))
            {
                SetCollected(true);
            }
        }

        public void Configure(AbilityType ability)
        {
            _ability = ability;
        }

        public void ResetPickup()
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
