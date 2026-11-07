using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class SightAbility : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _detectionRange = 7f;
        [SerializeField, Min(0.1f)] private float _detectionHeight = 1.5f;
        [SerializeField] private LayerMask _targetMask;
        [SerializeField] private LineRenderer _scanLine;
        [SerializeField, Min(0f)] private float _feedbackDuration = 0.2f;

        private PlayerController _player;
        private float _feedbackTimeRemaining;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            if (_scanLine != null)
            {
                _scanLine.enabled = false;
            }
        }

        private void Update()
        {
            if (_scanLine == null || !_scanLine.enabled)
            {
                return;
            }

            _feedbackTimeRemaining -= Time.deltaTime;
            if (_feedbackTimeRemaining <= 0f)
            {
                _scanLine.enabled = false;
            }
        }

        public void Configure(float detectionRange, LayerMask targetMask, LineRenderer scanLine)
        {
            _detectionRange = detectionRange;
            _targetMask = targetMask;
            _scanLine = scanLine;
        }

        public bool Activate()
        {
            Vector2 origin = transform.position;
            Vector2 direction = Vector2.right * _player.FacingDirection;
            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _targetMask,
                useTriggers = true
            };
            RaycastHit2D[] hits = new RaycastHit2D[16];
            int hitCount = Physics2D.BoxCast(
                origin,
                new Vector2(0.15f, _detectionHeight),
                0f,
                direction,
                filter,
                hits,
                _detectionRange);
            HiddenPlatform target = null;
            Vector2 end = origin + direction * _detectionRange;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hits[i];
                HiddenPlatform candidate = hit.collider.GetComponentInParent<HiddenPlatform>();
                if (candidate == null || candidate.IsRevealed || hit.distance >= nearestDistance)
                {
                    continue;
                }

                target = candidate;
                end = hit.point;
                nearestDistance = hit.distance;
            }

            ShowScan(origin, end, target != null);
            if (target == null)
            {
                return false;
            }

            target.Reveal();
            return true;
        }

        private void ShowScan(Vector2 origin, Vector2 end, bool success)
        {
            if (_scanLine == null)
            {
                return;
            }

            _scanLine.startColor = success ? new Color(0.55f, 1f, 0.78f) : new Color(0.75f, 0.9f, 1f, 0.7f);
            _scanLine.endColor = _scanLine.startColor;
            _scanLine.positionCount = 2;
            _scanLine.SetPosition(0, origin);
            _scanLine.SetPosition(1, end);
            _scanLine.enabled = true;
            _feedbackTimeRemaining = _feedbackDuration;
        }

        private void OnDrawGizmosSelected()
        {
            int direction = _player != null ? _player.FacingDirection : 1;
            Gizmos.color = new Color(0.55f, 1f, 0.78f);
            Vector3 center = transform.position + Vector3.right * direction * (_detectionRange * 0.5f);
            Gizmos.DrawWireCube(center, new Vector3(_detectionRange, _detectionHeight, 0f));
        }
    }
}
