using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(PlayerController), typeof(Rigidbody2D))]
    public sealed class TouchAbility : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _surfaceCheckRadius = 0.85f;
        [SerializeField, Min(0f)] private float _climbSpeed = 4.5f;
        [SerializeField] private LayerMask _surfaceMask;

        private PlayerController _player;
        private Rigidbody2D _body;
        private ClimbableSurface _activeSurface;
        private float _normalGravityScale;

        public bool IsClimbing => _activeSurface != null;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _body = GetComponent<Rigidbody2D>();
            _normalGravityScale = _body.gravityScale;
        }

        private void FixedUpdate()
        {
            if (_activeSurface == null)
            {
                return;
            }

            if (!IsStillNear(_activeSurface))
            {
                ExitClimb();
                return;
            }

            _body.linearVelocity = Vector2.up * (_player.VerticalInput * _climbSpeed);
        }

        public void Configure(float checkRadius, float climbSpeed, LayerMask surfaceMask)
        {
            _surfaceCheckRadius = checkRadius;
            _climbSpeed = climbSpeed;
            _surfaceMask = surfaceMask;
        }

        public void ToggleClimb()
        {
            if (_activeSurface != null)
            {
                ExitClimb();
                return;
            }

            ClimbableSurface surface = FindNearbySurface();
            if (surface != null)
            {
                EnterClimb(surface);
            }
        }

        public void ResetState()
        {
            ExitClimb();
        }

        private void EnterClimb(ClimbableSurface surface)
        {
            _normalGravityScale = _body.gravityScale;
            _activeSurface = surface;
            _body.gravityScale = 0f;
            _body.linearVelocity = Vector2.zero;
            _player.SetClimbing(true);
        }

        private void ExitClimb()
        {
            if (_body != null)
            {
                _body.gravityScale = _normalGravityScale <= 0f ? 4f : _normalGravityScale;
                _body.linearVelocity = new Vector2(_body.linearVelocity.x, Mathf.Min(0f, _body.linearVelocity.y));
            }

            _activeSurface = null;
            if (_player != null)
            {
                _player.SetClimbing(false);
            }
        }

        private ClimbableSurface FindNearbySurface()
        {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, _surfaceCheckRadius, _surfaceMask);
            foreach (Collider2D overlap in overlaps)
            {
                ClimbableSurface surface = overlap.GetComponentInParent<ClimbableSurface>();
                if (surface != null)
                {
                    return surface;
                }
            }

            return null;
        }

        private bool IsStillNear(ClimbableSurface surface)
        {
            Collider2D surfaceCollider = surface.SurfaceCollider;
            return surfaceCollider != null
                && surfaceCollider.Distance(_player.PlayerCollider).distance <= _surfaceCheckRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.75f, 0.55f, 1f);
            Gizmos.DrawWireSphere(transform.position, _surfaceCheckRadius);
        }
    }
}
