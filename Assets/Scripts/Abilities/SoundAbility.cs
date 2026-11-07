using System.Collections.Generic;
using UnityEngine;

namespace MicroSense
{
    public sealed class SoundAbility : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _interactionRadius = 3.2f;
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private bool _affectAllTargets = true;
        [SerializeField] private Transform _pulseVisual;

        private float _pulseTime;

        private void Update()
        {
            if (_pulseVisual == null || !_pulseVisual.gameObject.activeSelf)
            {
                return;
            }

            _pulseTime += Time.deltaTime * 3.5f;
            float scale = Mathf.Lerp(0.25f, _interactionRadius * 2f, Mathf.Clamp01(_pulseTime));
            _pulseVisual.localScale = Vector3.one * scale;
            SpriteRenderer renderer = _pulseVisual.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f - Mathf.Clamp01(_pulseTime);
                renderer.color = color;
            }

            if (_pulseTime >= 1f)
            {
                _pulseVisual.gameObject.SetActive(false);
            }
        }

        public void Configure(
            float interactionRadius,
            LayerMask targetMask,
            bool affectAllTargets,
            Transform pulseVisual)
        {
            _interactionRadius = interactionRadius;
            _targetMask = targetMask;
            _affectAllTargets = affectAllTargets;
            _pulseVisual = pulseVisual;
            if (_pulseVisual != null)
            {
                _pulseVisual.gameObject.SetActive(false);
            }
        }

        public int Activate()
        {
            ShowPulse();
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, _interactionRadius, _targetMask);
            List<BreakablePlatform> targets = new List<BreakablePlatform>();

            foreach (Collider2D overlap in overlaps)
            {
                BreakablePlatform target = overlap.GetComponentInParent<BreakablePlatform>();
                if (target != null && !target.IsBroken && !targets.Contains(target))
                {
                    targets.Add(target);
                }
            }

            if (targets.Count == 0)
            {
                return 0;
            }

            targets.Sort((a, b) =>
            {
                float aDistance = ((Vector2)a.transform.position - (Vector2)transform.position).sqrMagnitude;
                float bDistance = ((Vector2)b.transform.position - (Vector2)transform.position).sqrMagnitude;
                return aDistance.CompareTo(bDistance);
            });

            int count = _affectAllTargets ? targets.Count : 1;
            for (int i = 0; i < count; i++)
            {
                targets[i].Break();
            }

            return count;
        }

        private void ShowPulse()
        {
            if (_pulseVisual == null)
            {
                return;
            }

            _pulseTime = 0f;
            _pulseVisual.localScale = Vector3.one * 0.25f;
            SpriteRenderer renderer = _pulseVisual.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 0.75f;
                renderer.color = color;
            }

            _pulseVisual.gameObject.SetActive(true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.82f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }
    }
}
