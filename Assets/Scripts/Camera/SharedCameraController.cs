using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Camera))]
    public sealed class SharedCameraController : MonoBehaviour
    {
        [SerializeField] private Transform[] _targets;
        [SerializeField, Min(0f)] private float _horizontalPadding = 4f;
        [SerializeField, Min(0f)] private float _verticalPadding = 3f;
        [SerializeField, Min(0.1f)] private float _minimumSize = 6f;
        [SerializeField, Min(0.1f)] private float _maximumSize = 11f;
        [SerializeField, Min(0f)] private float _positionSmoothTime = 0.22f;
        [SerializeField, Min(0f)] private float _zoomSmoothTime = 0.3f;
        [SerializeField] private Vector2 _minimumBounds = new Vector2(-40f, -15f);
        [SerializeField] private Vector2 _maximumBounds = new Vector2(40f, 20f);

        private Camera _camera;
        private Vector3 _positionVelocity;
        private float _zoomVelocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (_targets == null || _targets.Length == 0)
            {
                return;
            }

            Bounds bounds = new Bounds(_targets[0].position, Vector3.zero);
            for (int i = 1; i < _targets.Length; i++)
            {
                bounds.Encapsulate(_targets[i].position);
            }

            Vector3 desiredPosition = bounds.center;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minimumBounds.x, _maximumBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y + 0.8f, _minimumBounds.y, _maximumBounds.y);
            desiredPosition.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _positionVelocity,
                _positionSmoothTime);

            float aspect = Mathf.Max(0.1f, _camera.aspect);
            float horizontalSize = bounds.size.x * 0.5f / aspect + _horizontalPadding;
            float verticalSize = bounds.size.y * 0.5f + _verticalPadding;
            float desiredSize = Mathf.Clamp(Mathf.Max(horizontalSize, verticalSize), _minimumSize, _maximumSize);
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                desiredSize,
                ref _zoomVelocity,
                _zoomSmoothTime);
        }

        public void Configure(Transform[] targets, Vector2 minimumBounds, Vector2 maximumBounds)
        {
            _targets = targets;
            _minimumBounds = minimumBounds;
            _maximumBounds = maximumBounds;
        }
    }
}
