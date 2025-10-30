using UnityEngine;

/// <summary>
/// 플레이어가 지면에 있는지 확인하는 컴포넌트
/// 레이캐스트를 사용하여 바닥 감지 및 충돌 정보 제공
/// Launch Pad 감지도 이 컴포넌트에서 처리
/// </summary>
public class KirbyGroundCheck : MonoBehaviour
{
    private bool onGround;
    private RaycastHit2D rightRayHit;
    private RaycastHit2D leftRayHit;

    [Header("Collider Settings")]
    [SerializeField]
    [Tooltip("Raycast 길이")]
    private float groundLength = 0.95f;

    [SerializeField]
    [Tooltip("Raycast 오프셋")]
    private Vector3 colliderOffset;

    [Header("Layer Masks")]
    [SerializeField]
    [Tooltip("바닥 Layer")]
    private LayerMask groundLayer;

    private void Update()
    {
        // 양쪽에서 레이캐스트를 수행하고 결과 저장
        rightRayHit = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, groundLayer);
        leftRayHit = Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, groundLayer);

        onGround = rightRayHit.collider != null || leftRayHit.collider != null;
    }

    private void OnDrawGizmos()
    {
        // 디버그용 지면 감지 시각화
        if (onGround) { Gizmos.color = Color.green; }
        else { Gizmos.color = Color.red; }

        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);
    }

    /// <summary>
    /// 플레이어가 지면에 있는지 반환
    /// </summary>
    public bool GetOnGround() => onGround;

    /// <summary>
    /// 현재 밟고 있는 지면의 LaunchPadData를 반환
    /// Launch Pad 태그가 있고 LaunchPadData 컴포넌트가 있는 경우만 반환
    /// 없으면 null 반환
    /// </summary>
    public LaunchPadData GetLaunchPadData()
    {
        // 오른쪽 레이가 LaunchPad 태그를 감지했는지 확인
        if (rightRayHit.collider != null && rightRayHit.collider.CompareTag("LaunchPad"))
        {
            LaunchPadData _launchPadData = rightRayHit.collider.GetComponent<LaunchPadData>();
            if (_launchPadData != null)
                return _launchPadData;
        }

        // 왼쪽 레이가 LaunchPad 태그를 감지했는지 확인
        if (leftRayHit.collider != null && leftRayHit.collider.CompareTag("LaunchPad"))
        {
            LaunchPadData _launchPadData = leftRayHit.collider.GetComponent<LaunchPadData>();
            if (_launchPadData != null)
                return _launchPadData;
        }

        return null;
    }
}