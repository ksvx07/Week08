using UnityEngine;

public class KirbyGroundCheck : MonoBehaviour
{
    public bool onGround;

    [Header("Collider Settings")]
    [SerializeField][Tooltip("Raycast 길이")] private float groundLength = 0.95f;
    [SerializeField][Tooltip("Raycast 오프셋")] private Vector3 colliderOffset;
    [Header("Layer Masks")]
    [SerializeField][Tooltip("바닥 Layer")] private LayerMask groundLayer;

    private void OnDisable()
    {
        onGround = false;
    }
    private void Update()
    {
        onGround = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, groundLayer) || Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, groundLayer);
    }
    private void OnDrawGizmos()
    {
        //Draw the ground colliders on screen for debug purposes
        if (onGround) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);
    }

    // 바닥 여부, 외부에서 접근 가능한 함수
    public bool GetOnGround() { return onGround; }
}
