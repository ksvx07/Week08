using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Hack: enum으로 전면 수술하기
public enum PlayerShape
{
    Circle,
    Star,
    Square,
    Triangle
}
[RequireComponent(typeof(PlayerDataLog))] // Hack : 데이터 로그 참고용 제거예정
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    public PlayerShape CurrentShape { get; private set; }
    private PlayerShape selectShape;
    private bool isSelectUIActive = false;
    [SerializeField] private PlayerShape startShape = PlayerShape.Circle;
    [SerializeField] private List<GameObject> shapes;
    [SerializeField] private List<Image> pannels;
    [SerializeField] private Color originColor;
    [SerializeField] private Color highLightColor;

    [SerializeField] private CameraController camControlelr;
    [SerializeField] private GameObject selectPlayerPanel;

    public GameObject _currentPlayerPrefab { get; private set; }
    private PlayerInput inputActions;

    public bool IsHold { get; private set; }
    public bool IsSelectMode { get; private set; }
    public bool IsTimeSlow { get; private set; }
    private Vector3 _MaxScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private float _selectPanelSpeed = 60f;
    [SerializeField] private float transformTimeScale = 0.02f;
    private Coroutine pannelActive;

    #region Mouse 
    private Vector2 mouseStartPosition; // 우클릭을 시작한 마우스 위치
    [SerializeField] private float mouseDeadZone = 50f;       // 마우스가 이 거리 이상 움직여야 인식
    #endregion

    #region 게임 로그용
    PlayerDataLog playerDataLog;
    #endregion

    private void Awake()
    {

        if (null == Instance)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        playerDataLog = GetComponent<PlayerDataLog>();

        inputActions = new PlayerInput();

        selectShape = startShape;
        CurrentShape = selectShape;
        _currentPlayerPrefab = shapes[(int)CurrentShape];

        playerDataLog.PlayerLogStart(startShape); // Log 데이터 수집 시작
        ActiveStartPlayer(startShape);
        InitChangingShape();

        // 시스템 마우스 커서 숨기기
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnEnable()
    {
        inputActions.UI.Enable();
        inputActions.UI.SwitchModeActive.performed += OnSwitchModeClicked;
        inputActions.UI.SwitchModeEnd.performed += OnSwitchModeEnd;
    }

    private void OnDisable()
    {
        inputActions.UI.SwitchModeActive.performed -= OnSwitchModeClicked;
        inputActions.UI.SwitchModeEnd.performed -= OnSwitchModeEnd;
        inputActions.UI.Disable();
    }
    private void Update()
    {
        ToOriginalTimeScale();
        UpdateMouseSelection();
    }

    #region InputAction 콜백 함수

    private bool changingShape = false;


    private void OffNewSwitch(InputAction.CallbackContext context)
    {
        foreach (var control in context.action.controls)
        {
            if (control.name == "w" && control.IsPressed() && !wPressed)
            {
                selectShape = PlayerShape.Circle;
                wPressed = true;
            }
            else if (control.name == "s" && control.IsPressed() && !sPressed)
            {
                selectShape = PlayerShape.Square;
                sPressed = true;
            }
            else if (control.name == "a" && control.IsPressed() && !aPressed)
            {
                selectShape = PlayerShape.Triangle;
                aPressed = true;
            }
            else if (control.name == "d" && control.IsPressed() && !dPressed)
            {
                selectShape = PlayerShape.Star;
                dPressed = true;
            }

            if (control.name == "w" && !control.IsPressed())
            {
                wPressed = false;
            }
            if (control.name == "s" && !control.IsPressed())
            {
                sPressed = false;
            }
            if (control.name == "a" && !control.IsPressed())
            {
                aPressed = false;
            }
            if (control.name == "d" && !control.IsPressed())
            {
                dPressed = false;
            }

            if (control.IsPressed())
            {
                return;
            }
        }

        if (!canChangeTimeScale) return;

        if (changingShape)
        {
            changingShape = false;
            ActiveSelectShape(CurrentShape, selectShape);
            // 잠금된 도형이 아니면 로그 기록
            if (ShapeUnlockSystem.IsUnlocked(selectShape) == true)
            {
                playerDataLog.OnPlayerQuickSwitch(selectShape); // Hack : 게임 Log 용
            }
            DeActiveSelectUI();
        }
    }

    private bool wPressed = false;
    private bool sPressed = false;
    private bool aPressed = false;
    private bool dPressed = false;

    private void OnNewSwitch(InputAction.CallbackContext context)
    {
        changingShape = true;
        OnSwithModeStart();

        foreach (var control in context.action.controls)
        {
            if (control.name == "w" && control.IsPressed() && !wPressed)
            {
                selectShape = PlayerShape.Circle;
                wPressed = true;
            }
            else if (control.name == "s" && control.IsPressed() && !sPressed)
            {
                selectShape = PlayerShape.Square;
                sPressed = true;
            }
            else if (control.name == "a" && control.IsPressed() && !aPressed)
            {
                selectShape = PlayerShape.Triangle;
                aPressed = true;
            }
            else if (control.name == "d" && control.IsPressed() && !dPressed)
            {
                selectShape = PlayerShape.Star;
                dPressed = true;
            }

            if (control.name == "w" && !control.IsPressed())
            {
                wPressed = false;
            }
            if (control.name == "s" && !control.IsPressed())
            {
                sPressed = false;
            }
            if (control.name == "a" && !control.IsPressed())
            {
                aPressed = false;
            }
            if (control.name == "d" && !control.IsPressed())
            {
                dPressed = false;
            }
        }

        HighLightSelectShape(selectShape);
    }




    private void InitChangingShape()
    {
        changingShape = false;
    }

    #endregion

    #region Mouse
    private void OnSwitchModeClicked(InputAction.CallbackContext context)
    {
        OnSwithModeStart();
    }

    private void OnSwitchModeEnd(InputAction.CallbackContext context)
    {
        if (isSelectUIActive == false) return;
        
        DeActiveSelectUI();
        ActiveSelectShape(CurrentShape, selectShape);
    }

    private void UpdateMouseSelection()
    {
        if (isSelectUIActive == false) return;

        Vector2 mouseOffset = (Vector2)Input.mousePosition - mouseStartPosition;

        // 마우스가 deadZone보다 적게 움직였으면 아무것도 선택하지 않음
        if (mouseOffset.magnitude < mouseDeadZone)
        {
            return;
        }

        PlayerShape mouseSelecteShape = CurrentShape;
        float angle = Vector2.SignedAngle(Vector2.up, mouseOffset);

        // 각도를 기반으로 상하좌우 결정
        if (angle > -45 && angle <= 45) mouseSelecteShape = PlayerShape.Circle; // 상
        else if (angle > 135 || angle <= -135) mouseSelecteShape = PlayerShape.Square; // 하
        else if (angle > 45 && angle <= 135) mouseSelecteShape = PlayerShape.Triangle; // 좌
        else if (angle > -135 && angle <= -45) mouseSelecteShape = PlayerShape.Star; ; // 우

        Debug.Log($"선택된 모양: {mouseSelecteShape.ToString()}, 각도: {angle}");

        selectShape = mouseSelecteShape;
        HighLightSelectShape(selectShape);
    }

    #endregion

    public void OnSwithModeStart()
    {
        SlowTimeScale();

        if (!isSelectUIActive)
        {
            // IsHold = true;
            AcitveSelectUI();
        }
    }

    // public void OnSwitchModeEnd()
    // {
    //     if (isSelectUIActive)
    //     {
    //         DeActiveSelectUI();
    //         ActiveSelectShape(CurrentShape, selectShape);

    //         // 잠금된 도형이 아니면 로그 기록
    //         if (ShapeUnlockSystem.IsUnlocked(selectShape) == true)
    //         {
    //             playerDataLog.OnPlayerModeSwitch(selectShape); // Hack : 게임 Log 용
    //         }
    //     }
    // }

    public void OnPlayerDead()
    {


        playerDataLog.PlayerDeadLog();
        if (isSelectUIActive)
        {
            DeActiveSelectUI();
            ActiveSelectShape(CurrentShape, selectShape);
        }
    }

    private void HighLightSelectShape(PlayerShape newShape)
    {
        foreach (var pannel in pannels)
        {
            pannel.color = originColor;
        }
        pannels[(int)newShape].color = highLightColor;
    }
    private void ActiveStartPlayer(PlayerShape starstPlayer)
    {
        _currentPlayerPrefab = shapes[(int)starstPlayer];
        _currentPlayerPrefab.SetActive(true);
        CurrentShape = selectShape;
    }

    public void PlayerSetActive(bool isAcitve)
    {
        _currentPlayerPrefab.SetActive(isAcitve);
    }
    // 새 모양으로 변신하기
    private void ActiveSelectShape(PlayerShape oldShape, PlayerShape newShape)
    {
        OriginalTimeScale();

        // 잠금된 도형으로 변경 불가능
        if (ShapeUnlockSystem.IsUnlocked(newShape) == false)
        {
            Debug.Log($"{newShape}은 잠금 상태입니다");
            return;
        }

        HighLightSelectShape(newShape);
        if (oldShape == PlayerShape.Square && newShape == PlayerShape.Square) return;

        GameObject oldPlayerPrefab = shapes[(int)oldShape];
        Transform lastPos = oldPlayerPrefab.transform;
        Vector2 lastVelocity = oldPlayerPrefab.GetComponent<Rigidbody2D>().linearVelocity;
        int lastDashCount = oldPlayerPrefab.GetComponent<IPlayerController>().dashCount;
        oldPlayerPrefab.SetActive(false);

        _currentPlayerPrefab = shapes[(int)newShape];
        _currentPlayerPrefab.transform.position = lastPos.position;
        _currentPlayerPrefab.SetActive(true);
        _currentPlayerPrefab.GetComponent<IPlayerController>().OnEnableSetVelocity(lastVelocity.x, lastVelocity.y, lastDashCount);

        CurrentShape = selectShape;
        InitChangingShape();
    }

    /// <summary>
    /// 플레이어랑 상관없이 강제로 도형 변경하기
    /// </summary>
    public void ForceToChangeShape(PlayerShape newShape)
    {
        OriginalTimeScale();
        playerDataLog.OnPlayerModeSwitch(newShape); // Hack : 게임 Log 용

        // 잠금된 도형으로 변경 불가능
        if (ShapeUnlockSystem.IsUnlocked(newShape) == false)
        {
            Debug.Log($"{newShape}은 잠금 상태입니다");
            return;
        }

        HighLightSelectShape(newShape);

        GameObject oldPlayerPrefab = shapes[(int)CurrentShape];

        Transform lastPos = oldPlayerPrefab.transform;
        Vector2 lastVelocity = oldPlayerPrefab.GetComponent<Rigidbody2D>().linearVelocity;
        int lastDashCount = oldPlayerPrefab.GetComponent<IPlayerController>().dashCount;
        oldPlayerPrefab.SetActive(false);

        _currentPlayerPrefab = shapes[(int)newShape];
        _currentPlayerPrefab.transform.position = lastPos.position;
        _currentPlayerPrefab.SetActive(true);
        _currentPlayerPrefab.GetComponent<IPlayerController>().OnEnableSetVelocity(lastVelocity.x, lastVelocity.y, lastDashCount);

        CurrentShape = newShape;
    }

    #region Switch Mode UI 함수
    private void AcitveSelectUI()
    {
        if (!StageManager.Instance.unlockAll)
            return;

        // 시스템 마우스 커서 숨기기
        Cursor.visible = false;

        // 현재 마우스 위치를 시작점으로 저장
        mouseStartPosition = Input.mousePosition;

        HighLightSelectShape(selectShape);
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(_currentPlayerPrefab.transform.position);
        selectPlayerPanel.GetComponent<RectTransform>().position = screenPosition;

        if (pannelActive != null)
        {
            ScaleDownOverTime();
        }
        pannelActive = StartCoroutine(ScaleOverTime());
        selectPlayerPanel.SetActive(true);
        isSelectUIActive = true;
    }

    private void DeActiveSelectUI()
    {
        if (pannelActive != null)
        {
            ScaleDownOverTime();
        }
        // IsHold = false;
        selectPlayerPanel.SetActive(false);
        isSelectUIActive = false;
    }
    // 게임 시간 느리게 하기
    private void SlowTimeScale()
    {
        if (!StageManager.Instance.unlockAll)
            return;
        if (!canChangeTimeScale) return;
        if (IsTimeSlow) return;
        IsTimeSlow = true;
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private bool canChangeTimeScale = true;

    public void SetCanChangeTimeScale(bool canChange)
    {
        canChangeTimeScale = canChange;
    }

    // 게임 시간 원래대로 되돌리기
    private void OriginalTimeScale()
    {
        if (!canChangeTimeScale) return;
        IsTimeSlow = false;
    }
    [SerializeField] float slowTimeScale = 0.05f;
    [SerializeField] float timeScaleSpeed = 2f;

    public void OriginalTimeScaleImmediate()
    {
        IsTimeSlow = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void ToOriginalTimeScale()
    {
        if (!IsTimeSlow)
        {
            if (Time.timeScale < 1f)
            {
                Time.timeScale += timeScaleSpeed * Time.unscaledDeltaTime;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
            else
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }
    }

    private IEnumerator ScaleOverTime()
    {
        selectPlayerPanel.SetActive(true);
        selectPlayerPanel.transform.localScale = Vector3.zero;

        Vector3 initialScale = selectPlayerPanel.transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < _selectPanelSpeed)
        {
            selectPlayerPanel.transform.position = _currentPlayerPrefab.transform.position;

            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / _selectPanelSpeed);

            selectPlayerPanel.transform.localScale = Vector3.Lerp(initialScale, _MaxScale, t);

            yield return null;
        }
        while (true)
        {
            selectPlayerPanel.transform.position = _currentPlayerPrefab.transform.position;
            scaleDownDelayTimerCounter = scaleDownDelay;
            yield return null;
        }
    }

    [SerializeField] private float scaleDownDelay = 0.1f;
    private float scaleDownDelayTimerCounter = 0f;

    private void ScaleDownOverTime()
    {
        if (scaleDownDelayTimerCounter > 0f)
        {
            scaleDownDelayTimerCounter -= Time.deltaTime;
            return;
        }
        if (pannelActive != null)
        {
            StopCoroutine(pannelActive);
        }
        selectPlayerPanel.transform.localScale = Vector3.zero;

    }

    #endregion
}
