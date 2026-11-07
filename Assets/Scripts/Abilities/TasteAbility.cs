using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class TasteAbility : MonoBehaviour
    {
        private const float ReleaseOffset = 0.75f;

        [SerializeField] private PlayerController _teammate;
        [SerializeField] private Transform _holdAnchor;
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField, Min(0f)] private float _eatRange = 1.65f;
        [SerializeField, Min(0f)] private float _launchSpeed = 25f;
        [SerializeField, Range(0f, 180f)] private float _minimumAngle = 0f;
        [SerializeField, Range(0f, 180f)] private float _maximumAngle = 180f;
        [SerializeField, Min(0f)] private float _angleSpeed = 75f;
        [SerializeField, Min(0f)] private float _angleInputResponsiveness = 10f;
        [SerializeField, Min(2)] private int _trajectoryPoints = 18;
        [SerializeField, Min(0.01f)] private float _trajectoryStep = 0.08f;

        private PlayerController _player;
        private PlayerController _heldPlayer;
        private bool _isAiming;
        private float _launchAngle;
        private float _smoothedAngleInput;

        public bool IsHoldingPlayer => _heldPlayer != null;
        public bool IsAiming => _isAiming;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
        }

        private void Update()
        {
            if (!_isAiming)
            {
                return;
            }

            _smoothedAngleInput = Mathf.MoveTowards(
                _smoothedAngleInput,
                _player.HorizontalInput,
                _angleInputResponsiveness * Time.deltaTime);
            _launchAngle = Mathf.Clamp(
                _launchAngle - _smoothedAngleInput * _angleSpeed * Time.deltaTime,
                _minimumAngle,
                _maximumAngle);
            if (_heldPlayer != null)
            {
                DrawTrajectory();
            }
        }

        public void Configure(
            PlayerController teammate,
            Transform holdAnchor,
            LineRenderer trajectoryLine,
            float eatRange,
            float launchSpeed)
        {
            _teammate = teammate;
            _holdAnchor = holdAnchor;
            _trajectoryLine = trajectoryLine;
            _eatRange = eatRange;
            _launchSpeed = launchSpeed;
        }

        public bool TryHandleJumpAction()
        {
            if (!_isAiming)
            {
                return false;
            }

            if (_heldPlayer != null)
            {
                LaunchHeldPlayer();
                return true;
            }

            if (!CanEatTeammate())
            {
                return false;
            }

            EatTeammate();
            return true;
        }

        public void ToggleAiming()
        {
            _isAiming = !_isAiming;
            _player.SetAiming(_isAiming);
            if (_isAiming)
            {
                _launchAngle = _player.FacingDirection > 0 ? 30f : 150f;
                _smoothedAngleInput = 0f;
                if (_heldPlayer != null)
                {
                    DrawTrajectory();
                }
            }
            else
            {
                _smoothedAngleInput = 0f;
                if (_trajectoryLine != null)
                {
                    _trajectoryLine.enabled = false;
                }
            }
        }

        public void ResetState()
        {
            if (_heldPlayer != null)
            {
                PlayerController releasedPlayer = _heldPlayer;
                _heldPlayer = null;
                releasedPlayer.SetHeld(false);
                releasedPlayer.transform.position = transform.position + Vector3.up * 1.2f;
            }

            _isAiming = false;
            _smoothedAngleInput = 0f;
            if (_player != null)
            {
                _player.SetAiming(false);
            }

            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
        }

        private bool CanEatTeammate()
        {
            return _teammate != null
                && !_teammate.IsHeld
                && _teammate.gameObject.activeInHierarchy
                && Vector2.Distance(transform.position, _teammate.transform.position) <= _eatRange;
        }

        private void EatTeammate()
        {
            _heldPlayer = _teammate;
            _heldPlayer.AbilityController.ResetTemporaryActionsOnly();
            _heldPlayer.SetHeld(true, _holdAnchor);
            DrawTrajectory();
        }

        private void LaunchHeldPlayer()
        {
            PlayerController launchedPlayer = _heldPlayer;
            _heldPlayer = null;
            _isAiming = false;
            _smoothedAngleInput = 0f;
            _player.SetAiming(false);

            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }

            Vector2 direction = GetLaunchDirection();
            launchedPlayer.transform.position = GetReleasePosition(direction);
            launchedPlayer.Launch(direction * _launchSpeed);
        }

        private void DrawTrajectory()
        {
            if (_trajectoryLine == null)
            {
                return;
            }

            Vector2 direction = GetLaunchDirection();
            Vector2 velocity = direction * _launchSpeed;
            Vector2 start = GetReleasePosition(direction);
            Vector2 gravity = Physics2D.gravity * _heldPlayer.Body.gravityScale;

            _trajectoryLine.positionCount = _trajectoryPoints;
            for (int i = 0; i < _trajectoryPoints; i++)
            {
                float time = i * _trajectoryStep;
                Vector2 position = start + velocity * time + 0.5f * gravity * time * time;
                _trajectoryLine.SetPosition(i, position);
            }

            _trajectoryLine.enabled = true;
        }

        private Vector2 GetLaunchDirection()
        {
            float radians = _launchAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private Vector2 GetReleasePosition(Vector2 direction)
        {
            return (Vector2)_holdAnchor.position + direction * ReleaseOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.45f, 0.62f);
            Gizmos.DrawWireSphere(transform.position, _eatRange);
        }
    }
}
