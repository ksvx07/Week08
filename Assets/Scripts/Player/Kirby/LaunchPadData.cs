using UnityEngine;

/// <summary>
/// 곡선 발사대 발판의 발사 정보를 관리하는 컴포넌트
/// LaunchPad 태그가 붙은 발판에 이 컴포넌트를 추가하면
/// KirbyController가 자동으로 감지하고 발사 처리를 수행합니다.
/// </summary>
public class LaunchPadData : MonoBehaviour
{
    [Header("Launch Direction")]
    [SerializeField]
    [Tooltip("발사 방향 (단위벡터, 자동 정규화됨)")]
    private Vector2 launchDirection = Vector2.up;

    [Header("Launch Force")]
    [SerializeField]
    [Range(0f, 100f)]
    [Tooltip("발사 중 적용되는 힘")]
    private float launchForce = 50f;

    [Header("Launch Duration")]
    [SerializeField]
    [Range(0.1f, 5f)]
    [Tooltip("발사 상태가 유지되는 시간 (초)")]
    private float launchDuration = 1f;

    [Header("Gravity During Launch")]
    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("발사 중 적용되는 중력값 (0 = 중력 없음)")]
    private float launchGravityScale = 0f;

    [Header("Minimum Time on Pad")]
    [SerializeField]
    [Range(0.1f, 5f)]
    [Tooltip("발사 준비를 위해 발판을 밟아야 하는 최소 시간 (초)")]
    private float minimumTimeOnPad = 1f;

    private BoxCollider2D _launchPadCollider;

    private void Awake()
    {
        // 발사대 범위를 정의하는 BoxCollider2D 캐싱
        _launchPadCollider = GetComponent<BoxCollider2D>();

        if (_launchPadCollider == null)
        {
            Debug.LogWarning($"{gameObject.name}에 BoxCollider2D가 없습니다. LaunchPad 범위 체크가 작동하지 않습니다.");
        }
    }

    /// <summary>
    /// 정규화된 발사 방향 반환
    /// </summary>
    public Vector2 GetLaunchDirection() => launchDirection.normalized;

    /// <summary>
    /// 발사 중 적용되는 힘 반환
    /// </summary>
    public float GetLaunchForce() => launchForce;

    /// <summary>
    /// 발사 상태가 유지되는 시간 반환
    /// </summary>
    public float GetLaunchDuration() => launchDuration;

    /// <summary>
    /// 발사 중 적용되는 중력값 반환
    /// </summary>
    public float GetLaunchGravityScale() => launchGravityScale;

    /// <summary>
    /// 발사 준비를 위한 최소 체류 시간 반환
    /// </summary>
    public float GetMinimumTimeOnPad() => minimumTimeOnPad;

    /// <summary>
    /// 플레이어의 collider가 발사대의 BoxCollider 범위와 겹쳐있는지 확인
    /// </summary>
    public bool IsPlayerInLaunchPadBounds(Collider2D _playerCollider)
    {
        if (_launchPadCollider == null || _playerCollider == null)
            return true;

        // BoxCollider2D의 범위와 플레이어의 collider 범위가 교차하는지 확인
        return _launchPadCollider.bounds.Intersects(_playerCollider.bounds);
    }

    private void OnDrawGizmosSelected()
    {
        // 씬에서 발사 방향을 시각화 (노란색 화살표)
        Gizmos.color = Color.yellow;
        Vector3 _startPos = transform.position;
        Vector3 _endPos = _startPos + (Vector3)launchDirection.normalized * 2f;
        Gizmos.DrawLine(_startPos, _endPos);
        Gizmos.DrawWireSphere(_endPos, 0.3f);
    }
}