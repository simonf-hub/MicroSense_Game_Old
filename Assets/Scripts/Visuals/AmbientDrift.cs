using UnityEngine;

namespace MicroSense
{
    public sealed class AmbientDrift : MonoBehaviour
    {
        [SerializeField] private Vector2 _amplitude = new Vector2(0f, 0.12f);
        [SerializeField, Min(0f)] private float _frequency = 1f;
        [SerializeField] private float _phase;
        [SerializeField] private float _rotationAmplitude = 2f;

        private Vector3 _origin;

        private void Awake()
        {
            _origin = transform.localPosition;
        }

        private void Update()
        {
            float wave = Mathf.Sin(Time.time * _frequency + _phase);
            transform.localPosition = _origin + (Vector3)(_amplitude * wave);
            transform.localRotation = Quaternion.Euler(0f, 0f, wave * _rotationAmplitude);
        }

        public void Configure(Vector2 amplitude, float frequency, float phase, float rotationAmplitude = 2f)
        {
            _amplitude = amplitude;
            _frequency = frequency;
            _phase = phase;
            _rotationAmplitude = rotationAmplitude;
        }
    }
}
