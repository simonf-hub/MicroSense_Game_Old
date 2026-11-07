using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ClimbableSurface : MonoBehaviour
    {
        [SerializeField] private Collider2D _surfaceCollider;

        public Collider2D SurfaceCollider => _surfaceCollider;

        private void Awake()
        {
            if (_surfaceCollider == null)
            {
                _surfaceCollider = GetComponent<Collider2D>();
            }
        }

        public void Configure(Collider2D surfaceCollider)
        {
            _surfaceCollider = surfaceCollider;
        }
    }
}
