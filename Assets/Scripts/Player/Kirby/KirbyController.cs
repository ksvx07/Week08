using System.Collections;
using UnityEngine;

public class KirbyController : MonoBehaviour, IPlayerController
{
    // 능력 사용 데이터 로그
    [Header("Game Log 용")]
    [SerializeField] PlayerDataLog playerDataLog;

    #region References
    Rigidbody2D _rb;
    KirbyGroundCheck _groundCheck;
    Collider2D _playerCollider;
    #endregion

    [Header("Movement Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("최대 속도")] private float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)][Tooltip("가속도")] private float maxAcceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("감속도")] private float maxDecceleration = 52f;
    [SerializeField, Range(1f, 100f)][Tooltip("방향 전환 속도")] private float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)][Tooltip("공중 가속도")] private float maxAirAcceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("공중 감속도")] private float maxAirDeceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("공중 방향 전환 속도")] private float maxAirTurnSpeed = 80f;

    [Header("Turbo Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("터보 속도")] private float turboSpeed = 20f;
    [SerializeField, Range(0f, 100f)][Tooltip("터보 감속도")] private float turboDecceleration = 52f;

    [Header("Bounce Settings")]
    [Tooltip("X방향 튕겨오르는 힘")]
    [SerializeField] private float bounceStrength = 5f;
    [Tooltip("Y방향 튕겨오르는 힘")]
    [SerializeField] private float bounceHeight = 10f;
    [Tooltip("튕겨오르는 효과가 지속되는 최소 시간")]
    [SerializeField] private float bounceDuration = 0.3f;
    private bool isBouncing = false;
    private bool isFixedBouncing = false;

    // Launch Pad 관련 변수
    [Header("Launch Pad Settings")]
    [SerializeField, Range(0.1f, 5f)]
    [Tooltip("발사 종료 후 터보 기본속도로 감속하는 시간 (초)")]
    private float launchDecelerationDuration = 2f;

    private bool isLaunched = false;
    private float launchTimer = 0f;
    private float timeOnLaunchPad = 0f;
    private LaunchPadData currentLaunchPad = null;
    private bool wasOnLaunchPadLastFrame = false;
    private bool isReadyToLaunch = false;

    private bool isLaunchDecelerating = false;
    private float launchDecelerationTimer = 0f;
    private float launchEndSpeed = 0f;

    [Header("Current State")]
    [SerializeField]
    private LayerMask groundLayer;
    public bool onGround;
    public bool pressingKey;
    private bool turboMode;

    #region Private - Speed Calculation Variables
    private Vector2 desiredVelocity;
    private float directionX;
    private float turboDirectionX;
    private float maxSpeedChangeAmount;
    private float acceleration;
    private float deceleration;
    private float turnSpeed;
    #endregion

    #region Public - Return Speed Variables
    public float DirectionX
    {
        get { return directionX; }
        set { directionX = value; }
    }
    public float MaxSpeed => maxSpeed;
    public bool TurboMode => turboMode;
    #endregion

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _groundCheck = GetComponent<KirbyGroundCheck>();
        _playerCollider = GetComponent<Collider2D>();
    }

    private void OnDisable()
    {
        InitializedCircle();
    }

    private void Update()
    {
        // 지상에 있으면 대시 횟수 초기화
        if (_groundCheck.GetOnGround())
            dashCount = maxDashCount;

        // 튕겨나가는 동안 입력 무시
        if (isFixedBouncing)
        {
            return;
        }

        // 입력값에 따라 캐릭터 방향 전환
        if (directionX != 0)
        {
            transform.localScale = new Vector3(directionX > 0 ? 1 : -1, 1, 1);
            pressingKey = true;
        }
        else
        {
            pressingKey = false;
        }

        // 튕겨나가는 상태에서 입력이 있으면 바운스 상태 해제
        if (isBouncing)
        {
            if (pressingKey) { isBouncing = false; }
            else return;
        }
    }

    private void FixedUpdate()
    {
        // 발사 중이면 기존 물리 로직 모두 스킵
        if (isLaunched)
        {
            HandleLaunchState();
            return;
        }

        // 발사 감속 중이면 감속 처리만 수행
        if (isLaunchDecelerating)
        {
            HandleLaunchDeceleration();
            return;
        }

        // 튕겨나가는 동안 물리 처리 무시
        if (isFixedBouncing) 
            return;

        onGround = _groundCheck.GetOnGround();

        // Launch Pad 감지 및 타이머 관리
        UpdateLaunchPadState();

        // 튕겨나가는 상태에서 지상에 착지하면 바운스 상태 해제
        if (isBouncing)
        {
            if (onGround) { isBouncing = false; }
            return;
        }

        // 터보 모드에 따라 움직임 처리
        if (turboMode)
        {
            RunWithoutAcceleration();
        }
        else
        {
            terboLayRotation = Vector2.down;
            RunWithAcceleration();
        }
    }

    /// <summary>
    /// Launch Pad 상태를 감지하고 발사 조건을 확인합니다.
    /// 발판 위에서의 체류 시간을 추적하고, 발판을 떠날 때 발사 여부를 결정합니다.
    /// </summary>
    private void UpdateLaunchPadState()
    {
        LaunchPadData _launchPadOnGround = _groundCheck.GetLaunchPadData();
        bool _isOnLaunchPadNow = _launchPadOnGround != null && turboMode;

        // 발사대에 새로 올라탔을 때: 타이머 초기화
        if (_isOnLaunchPadNow && !wasOnLaunchPadLastFrame)
        {
            currentLaunchPad = _launchPadOnGround;
            timeOnLaunchPad = 0f;
            isReadyToLaunch = false;
        }

        // 발사대 위에 있을 때: 체류 시간 증가 및 발사 준비 상태 확인
        if (_isOnLaunchPadNow)
        {
            timeOnLaunchPad += Time.fixedDeltaTime;

            // 최소 체류 시간을 충족하고 범위 내에 있으면 발사 준비 완료
            if (timeOnLaunchPad >= currentLaunchPad.GetMinimumTimeOnPad() && 
                currentLaunchPad.IsPlayerInLaunchPadBounds(_playerCollider))
            {
                isReadyToLaunch = true;
            }
        }

        // 발사대에서 떠났을 때 (이전 프레임에는 있었는데 지금 없음)
        if (!_isOnLaunchPadNow && wasOnLaunchPadLastFrame)
        {
            // 발사 준비가 완료된 상태면 발사
            if (currentLaunchPad != null && isReadyToLaunch)
            {
                Debug.Log($"[Launch Debug] 발사 조건 충족! 발사 시작");
                LaunchPlayer(currentLaunchPad);
            }
            else
            {
                // 발사하지 않는 경우만 상태 초기화
                currentLaunchPad = null;
                timeOnLaunchPad = 0f;
                isReadyToLaunch = false;
            }
        }

        // 터보모드 해제되면 발사 예약 취소
        if (wasOnLaunchPadLastFrame && !turboMode && _isOnLaunchPadNow)
        {
            currentLaunchPad = null;
            timeOnLaunchPad = 0f;
            isReadyToLaunch = false;
        }

        wasOnLaunchPadLastFrame = _isOnLaunchPadNow;
    }

    /// <summary>
    /// 플레이어를 발사대의 방향과 힘으로 발사시킵니다.
    /// 발사 중에는 중력, 속도, 입력을 모두 제어합니다.
    /// </summary>
    private void LaunchPlayer(LaunchPadData _launchPad)
    {
        isLaunched = true;
        launchTimer = 0f;
        currentLaunchPad = _launchPad;

        // 발사 중력 설정
        _rb.gravityScale = _launchPad.GetLaunchGravityScale();

        // LaunchForce를 속도의 크기값으로 사용하여 초기 발사 속도 설정
        Vector2 _launchVelocity = _launchPad.GetLaunchDirection() * _launchPad.GetLaunchForce();
        _rb.linearVelocity = _launchVelocity;
    }

    /// <summary>
    /// 발사 상태를 유지하고 타이머를 관리합니다.
    /// LaunchDuration 동안 발사 속도를 유지하다가, 시간이 끝나면 감속 상태로 전환합니다.
    /// </summary>
    private void HandleLaunchState()
    {
        launchTimer += Time.fixedDeltaTime;
        float _launchDuration = GetCurrentLaunchPadDuration();

        // 발사 중 속도 유지: 발사 방향과 힘으로 매 프레임 속도 유지
        if (currentLaunchPad != null)
        {
            Vector2 _maintainVelocity = currentLaunchPad.GetLaunchDirection() * currentLaunchPad.GetLaunchForce();
            _rb.linearVelocity = _maintainVelocity;
            
            // 디버그: 속도가 제대로 설정되는지 확인
            Debug.Log($"[Launch State] 타이머: {launchTimer:F2}, 지속시간: {_launchDuration:F2}, 현재속도: {_rb.linearVelocity.magnitude:F2}, 목표속도: {_maintainVelocity.magnitude:F2}");
        }

        // 발사 지속시간 초과 시 감속 상태로 전환
        if (launchTimer >= _launchDuration)
        {
            // 현재 속도를 기록하고 감속 상태 시작
            launchEndSpeed = _rb.linearVelocity.magnitude;
            isLaunched = false;
            isLaunchDecelerating = true;
            launchDecelerationTimer = 0f;
            
            Debug.Log($"[Launch End] 발사 종료, 종료 속도: {launchEndSpeed:F2}");
        }
    }

    /// <summary>
    /// 발사 종료 후 목표 속도로 천천히 감속합니다.
    /// 터보모드 상태에 따라 다른 목표 속도로 감속합니다.
    /// </summary>
    private void HandleLaunchDeceleration()
    {
        launchDecelerationTimer += Time.fixedDeltaTime;

        // 감속 중에 정상 중력 적용
        _rb.gravityScale = fixeNormaldGravity;

        // 감속 진행 비율 계산 (0 ~ 1)
        float _decelerationProgress = Mathf.Clamp01(launchDecelerationTimer / launchDecelerationDuration);

        // 터보모드 여부에 따라 목표 속도 결정
        float _targetSpeedValue = turboMode ? turboSpeed : maxSpeed;
        
        // 발사 종료 속도에서 목표 속도로 선형 보간
        float _targetSpeed = Mathf.Lerp(launchEndSpeed, _targetSpeedValue, _decelerationProgress);

        // 현재 속도의 방향은 유지하면서 크기만 변경
        if (_rb.linearVelocity.magnitude > 0)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * _targetSpeed;
        }

        // 감속 완료 시 감속 상태 해제
        if (launchDecelerationTimer >= launchDecelerationDuration)
        {
            isLaunchDecelerating = false;
            currentLaunchPad = null;
            Debug.Log($"[Deceleration Complete] 감속 완료, 현재 속도: {_rb.linearVelocity.magnitude:F2}");
        }
    }

    /// <summary>
    /// 현재 설정된 Launch Pad의 발사 지속시간을 반환합니다.
    /// Launch Pad가 없으면 기본값 1초를 반환합니다.
    /// </summary>
    private float GetCurrentLaunchPadDuration()
    {
        return currentLaunchPad != null ? currentLaunchPad.GetLaunchDuration() : 1f;
    }

    private Vector2 previousVelocity;

    /// <summary>
    /// 벽과 충돌 시 바운스 처리를 수행합니다.
    /// 터보모드에서만 작동하며, 충돌 각도에 따라 발사 여부를 결정합니다.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 바운스 중이거나 터보 모드가 아니면 무시
        if (isBouncing || !turboMode) return;

        if (groundLayer != 0)
        {
            // 모든 충돌 지점을 확인하여 벽인지 판단
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 _normal = contact.normal;
                float _angle = Vector2.Angle(terboLayRotation, _normal) - 90f;

                if (_angle < maxCollisionAngle)
                {
                    turboMode = false;
                    StartCoroutine(Bounce(collision));
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 컨트롤러의 모든 상태를 초기화합니다.
    /// 비활성화 될 때 또는 장면 전환 시 호출됩니다.
    /// </summary>
    private void InitializedCircle()
    {
        turboMode = false;
        isFixedBouncing = false;
        isBouncing = false;
        isLaunched = false;
        isLaunchDecelerating = false;
        launchTimer = 0f;
        launchDecelerationTimer = 0f;
        timeOnLaunchPad = 0f;
        currentLaunchPad = null;
        isReadyToLaunch = false;
    }

    /// <summary>
    /// 가속도를 적용한 이동 처리를 수행합니다 (일반 모드).
    /// 지상/공중에 따라 다른 가속도를 적용합니다.
    /// </summary>
    private void RunWithAcceleration()
    {
        _rb.gravityScale = fixeNormaldGravity;

        // 지상/공중 여부에 따라 가속도 설정
        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        // 이동 키를 누르고 있을 때
        if (pressingKey)
        {
            // 같은 방향으로 이동 중
            if (Mathf.Sign(directionX) == Mathf.Sign(_rb.linearVelocity.x))
            {
                // 최대 속도 미만이면 가속
                if (Mathf.Abs(_rb.linearVelocity.x) < maxSpeed)
                {
                    if (Mathf.Abs(_rb.linearVelocity.x + acceleration * directionX * Time.fixedDeltaTime) >= maxSpeed)
                    {
                        _rb.linearVelocityX = maxSpeed * Mathf.Sign(directionX);
                    }
                    else
                        _rb.linearVelocityX += acceleration * directionX * Time.deltaTime;
                }
                else
                {
                    // 최대 속도 초과 시 감속
                    _rb.linearVelocityX -= deceleration * Mathf.Sign(_rb.linearVelocity.x) * Time.deltaTime;
                }
            }
            else
            {
                // 반대 방향으로 전환 시 턴 속도 적용
                _rb.linearVelocityX += turnSpeed * directionX * Time.deltaTime;
            }
        }
        else
        {
            // 키를 떼면 감속
            _rb.linearVelocityX -= deceleration * Mathf.Sign(_rb.linearVelocity.x) * Time.deltaTime;

            // 속도가 0을 넘어가면 정지
            if (Mathf.Sign(_rb.linearVelocity.x) != Mathf.Sign(_rb.linearVelocity.x - deceleration * Mathf.Sign(_rb.linearVelocity.x) * Time.fixedDeltaTime))
            {
                _rb.linearVelocityX = 0;
            }
        }
    }

    private Vector2 terboLayRotation = Vector2.down;
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float rotationReturnSpeed = 5f;
    [SerializeField] private float terboForce = 60f;
    [SerializeField] private float maxCollisionAngle = 30.0f;
    [SerializeField] private float fixeNormaldGravity = 4.0f;
    private float fixeterbodGravity = 0f;

    /// <summary>
    /// 즉시 속도 변경 처리를 수행합니다 (터보 모드).
    /// 지면과 접촉 여부에 따라 중력과 이동 방향을 동적으로 조정합니다.
    /// </summary>
    private void RunWithoutAcceleration()
    {
        RaycastHit2D _hit = Physics2D.Raycast(transform.position, terboLayRotation, groundCheckDistance, groundLayer);
        Debug.DrawRay(transform.position, terboLayRotation * groundCheckDistance, Color.red);
        Vector2 _moveDirection;

        if (_hit.collider != null)
        {
            // 지면 접촉 시: 중력 제거, 표면 법선에 따라 방향 조정
            _rb.gravityScale = fixeterbodGravity;
            _rb.AddForce(9.81f * fixeNormaldGravity * terboLayRotation * _rb.mass, ForceMode2D.Force);
            terboLayRotation = -_hit.normal;
            _moveDirection = new Vector2(-terboLayRotation.y, terboLayRotation.x) * Mathf.Sign(transform.localScale.x);
        }
        else
        {
            // 지면 미접촉 시: 중력 복구, 기본 이동 방향 적용
            _rb.gravityScale = fixeNormaldGravity;
            terboLayRotation = Vector2.Lerp(terboLayRotation, Vector2.down, rotationReturnSpeed * Time.fixedDeltaTime);
            _moveDirection = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        }

        _rb.AddForce(terboForce * _moveDirection * _rb.mass, ForceMode2D.Force);

        // 속도 제한
        if (_rb.linearVelocity.magnitude > turboSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * turboSpeed;
        }
    }

    /// <summary>
    /// 벽 충돌 시 바운스 효과를 적용하는 코루틴입니다.
    /// 설정된 시간 동안 입력을 무시하고 정해진 방향으로 튕겨냅니다.
    /// </summary>
    private IEnumerator Bounce(Collision2D collision)
    {
        isFixedBouncing = true;
        isBouncing = true;

        Vector2 _fixedBounceVelocity = new Vector2(
            -transform.localScale.x * bounceStrength,
            bounceHeight
        );

        _rb.linearVelocity = _fixedBounceVelocity;

        yield return new WaitForSeconds(bounceDuration);
        isFixedBouncing = false;
    }

    #region Public - PlayerInput
    /// <summary>
    /// 이동 입력을 처리합니다.
    /// Input System에서 호출됩니다.
    /// </summary>
    public void OnMoveInput(Vector2 movementInput)
    {
        directionX = movementInput.x;
    }

    /// <summary>
    /// 터보 모드 토글을 처리합니다.
    /// 바운스나 발사 중에는 변경이 불가능합니다.
    /// 터보모드 해제 시 발사 예약도 함께 취소됩니다.
    /// </summary>
    public void OnTurboModePressed()
    {
        // 바운스 중에는 터보 모드 변경 불가
        if (isBouncing) return;

        // 발사 중에는 터보 모드 변경 불가
        if (isLaunched) return;

        if (turboMode == false)
        {
            // 터보 활성화 시 능력 사용 로그
            playerDataLog.OnPlayerUseAbility();
        }

        turboMode = !turboMode;

        // 터보모드 해제 시 발사 예약 취소
        if (!turboMode)
        {
            currentLaunchPad = null;
            timeOnLaunchPad = 0f;
        }
    }

    private int maxDashCount = 1;
    public int dashCount { get; set; }

    /// <summary>
    /// 플레이어 상태 전환 시 속도와 대시 횟수를 설정합니다.
    /// 상태 전환 스크립트에서 호출됩니다.
    /// </summary>
    public void OnEnableSetVelocity(float newVelX, float newVelY, int currentDashCount)
    {
        _rb = GetComponent<Rigidbody2D>();
        _groundCheck = GetComponent<KirbyGroundCheck>();
        _playerCollider = GetComponent<Collider2D>();
        _rb.linearVelocity = new Vector2(newVelX, newVelY);
        dashCount = currentDashCount;
    }
    #endregion
}