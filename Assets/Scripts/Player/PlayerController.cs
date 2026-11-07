using UnityEngine;

namespace MicroSense
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(PlayerAbilityController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const float LaunchGroundGraceDuration = 0.1f;
        private static readonly Color TasteModeColor = new Color(1f, 0.43f, 0.58f);
        private static readonly Color TouchModeColor = new Color(0.72f, 0.55f, 1f);

        [Header("Identity")]
        [SerializeField] private string _playerName = "Player";
        [SerializeField] private Color _playerColor = Color.white;
        [SerializeField] private PlayerInputBindings _inputBindings;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 6.5f;
        [SerializeField, Min(0f)] private float _jumpVelocity = 11f;
        [SerializeField] private bool _allowDoubleJump;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private Vector2 _groundCheckSize = new Vector2(0.55f, 0.12f);
        [SerializeField] private LayerMask _groundMask;

        [Header("Mode Feedback")]
        [SerializeField] private SpriteRenderer _bodyVisual;
        [SerializeField] private SpriteRenderer _modeAura;
        [SerializeField, Min(0f)] private float _modePulseSpeed = 5f;
        [SerializeField, Range(0f, 1f)] private float _modeAuraMinimumAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _modeAuraMaximumAlpha = 0.85f;
        [SerializeField, Min(1f)] private float _modeAuraMinimumScale = 1.35f;
        [SerializeField, Min(1f)] private float _modeAuraMaximumScale = 1.62f;

        private Rigidbody2D _body;
        private Collider2D _collider;
        private PlayerAbilityController _abilityController;
        private Color _normalBodyColor;
        private Color _normalAuraColor;
        private Vector3 _normalAuraScale;
        private Vector3 _normalScale;
        private bool _isHeld;
        private bool _isClimbing;
        private bool _isAiming;
        private bool _isLaunched;
        private bool _isGrounded;
        private float _launchGroundGraceRemaining;
        private int _jumpsUsed;
        private int _facingDirection = 1;

        public string PlayerName => _playerName;
        public Color PlayerColor => _playerColor;
        public PlayerInputBindings InputBindings => _inputBindings;
        public Rigidbody2D Body => _body;
        public Collider2D PlayerCollider => _collider;
        public PlayerAbilityController AbilityController => _abilityController;
        public bool IsGrounded => _isGrounded;
        public bool IsHeld => _isHeld;
        public bool IsClimbing => _isClimbing;
        public bool IsAiming => _isAiming;
        public bool AllowDoubleJump => _allowDoubleJump;
        public bool CanReceiveInput => !_isHeld && !_isLaunched && isActiveAndEnabled;
        public int FacingDirection => _facingDirection;
        public float HorizontalInput => CanReceiveInput ? _inputBindings.ReadHorizontal() : 0f;
        public float VerticalInput => CanReceiveInput ? _inputBindings.ReadVertical() : 0f;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _abilityController = GetComponent<PlayerAbilityController>();
            _normalScale = transform.localScale;
            CacheModeVisuals();
        }

        private void Update()
        {
            UpdateGroundedState();
            UpdateModeVisuals();

            if (!CanReceiveInput)
            {
                return;
            }

            if (_isAiming)
            {
                if (_inputBindings.UpPressedThisFrame)
                {
                    _abilityController.TryHandleJumpAction();
                }

                return;
            }

            if (_isClimbing)
            {
                return;
            }

            float horizontal = _inputBindings.ReadHorizontal();
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                _facingDirection = horizontal > 0f ? 1 : -1;
            }

            if (_inputBindings.UpPressedThisFrame)
            {
                bool abilityConsumedInput = _abilityController.TryHandleJumpAction();
                int maximumJumps = _allowDoubleJump ? 2 : 1;
                if (!abilityConsumedInput && _jumpsUsed < maximumJumps)
                {
                    Vector2 velocity = _body.linearVelocity;
                    velocity.y = _jumpVelocity;
                    _body.linearVelocity = velocity;
                    _jumpsUsed++;
                }
            }
        }

        private void FixedUpdate()
        {
            if (_isHeld || _isClimbing || _isLaunched)
            {
                return;
            }

            float horizontal = _isAiming ? 0f : _inputBindings.ReadHorizontal();
            Vector2 velocity = _body.linearVelocity;
            velocity.x = horizontal * _moveSpeed;
            _body.linearVelocity = velocity;
        }

        public void Configure(
            string playerName,
            Color playerColor,
            PlayerInputBindings inputBindings,
            Transform groundCheck,
            LayerMask groundMask,
            bool allowDoubleJump = false)
        {
            _playerName = playerName;
            _playerColor = playerColor;
            _inputBindings = inputBindings;
            _groundCheck = groundCheck;
            _groundMask = groundMask;
            _allowDoubleJump = allowDoubleJump;
        }

        public void SetHeld(bool held, Transform holdAnchor = null)
        {
            _isHeld = held;
            _body.simulated = !held;
            _collider.enabled = !held;

            if (held)
            {
                _isLaunched = false;
                _launchGroundGraceRemaining = 0f;
                _body.linearVelocity = Vector2.zero;
                transform.SetParent(holdAnchor, false);
                transform.localPosition = Vector3.zero;
                transform.localScale = _normalScale * 0.62f;
            }
            else
            {
                transform.SetParent(null, true);
                transform.localScale = _normalScale;
                _body.simulated = true;
                _collider.enabled = true;
            }
        }

        public void Launch(Vector2 velocity)
        {
            if (_isHeld)
            {
                SetHeld(false);
            }

            _jumpsUsed = 1;
            _isGrounded = false;
            _isLaunched = true;
            _launchGroundGraceRemaining = LaunchGroundGraceDuration;
            _body.linearVelocity = velocity;
        }

        public void SetClimbing(bool climbing)
        {
            _isClimbing = climbing;
            UpdateModeVisuals();
        }

        public void SetAiming(bool aiming)
        {
            _isAiming = aiming;
            UpdateModeVisuals();
            if (aiming)
            {
                Vector2 velocity = _body.linearVelocity;
                velocity.x = 0f;
                _body.linearVelocity = velocity;
            }
        }

        public void ResetPlayer(Vector3 position)
        {
            if (_isHeld)
            {
                SetHeld(false);
            }

            _isClimbing = false;
            _isAiming = false;
            _isLaunched = false;
            _isGrounded = false;
            _launchGroundGraceRemaining = 0f;
            _jumpsUsed = 0;
            _facingDirection = 1;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            _body.simulated = true;
            _collider.enabled = true;
            _body.gravityScale = 4f;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            RestoreModeVisuals();
        }

        private void CacheModeVisuals()
        {
            if (_bodyVisual == null)
            {
                _bodyVisual = GetComponent<SpriteRenderer>();
            }

            if (_modeAura == null)
            {
                Transform aura = transform.Find("Aura");
                if (aura != null)
                {
                    _modeAura = aura.GetComponent<SpriteRenderer>();
                }
            }

            if (_bodyVisual != null)
            {
                _normalBodyColor = _bodyVisual.color;
            }

            if (_modeAura != null)
            {
                _normalAuraColor = _modeAura.color;
                _normalAuraScale = _modeAura.transform.localScale;
            }
        }

        private void UpdateModeVisuals()
        {
            if (!_isAiming && !_isClimbing)
            {
                RestoreModeVisuals();
                return;
            }

            float pulse = (Mathf.Sin(Time.time * _modePulseSpeed) + 1f) * 0.5f;
            Color modeColor = _isAiming ? TasteModeColor : TouchModeColor;

            if (_bodyVisual != null)
            {
                _bodyVisual.color = Color.Lerp(_normalBodyColor, Color.white, Mathf.Lerp(0.12f, 0.32f, pulse));
            }

            if (_modeAura != null)
            {
                modeColor.a = Mathf.Lerp(_modeAuraMinimumAlpha, _modeAuraMaximumAlpha, pulse);
                _modeAura.color = modeColor;
                float scale = Mathf.Lerp(_modeAuraMinimumScale, _modeAuraMaximumScale, pulse);
                _modeAura.transform.localScale = Vector3.one * scale;
            }
        }

        private void RestoreModeVisuals()
        {
            if (_bodyVisual != null)
            {
                _bodyVisual.color = _normalBodyColor;
            }

            if (_modeAura != null)
            {
                _modeAura.color = _normalAuraColor;
                _modeAura.transform.localScale = _normalAuraScale;
            }
        }

        private void OnDisable()
        {
            RestoreModeVisuals();
        }

        private void UpdateGroundedState()
        {
            bool wasGrounded = _isGrounded;
            Vector2 position = _groundCheck != null
                ? (Vector2)_groundCheck.position
                : (Vector2)transform.position + Vector2.down * 0.55f;
            _isGrounded = Physics2D.OverlapBox(position, _groundCheckSize, 0f, _groundMask) != null;
            if (_isLaunched)
            {
                _launchGroundGraceRemaining = Mathf.Max(
                    0f,
                    _launchGroundGraceRemaining - Time.deltaTime);
                if (_isGrounded && _launchGroundGraceRemaining <= 0f)
                {
                    _isLaunched = false;
                }
            }

            if (_isGrounded && !wasGrounded)
            {
                _jumpsUsed = 0;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.yellow;
            Vector3 position = _groundCheck != null
                ? _groundCheck.position
                : transform.position + Vector3.down * 0.55f;
            Gizmos.DrawWireCube(position, _groundCheckSize);
        }
    }
}
