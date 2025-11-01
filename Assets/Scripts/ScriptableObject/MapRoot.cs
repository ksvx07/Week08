using UnityEngine;

public class MapRoot : MonoBehaviour
{
    [Header("연결된 Stage 데이터들 (여러 개 가능)")]
    public StageScriptableObject[] stageDataArray;

    private Vector3 _lastPosition;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (stageDataArray == null) return;

        Gizmos.color = Color.yellow;

        foreach (var stageData in stageDataArray)
        {
            if (stageData == null) continue;

            Vector3 center = new Vector3(
                (stageData.minX + stageData.maxX) / 2f,
                (stageData.minY + stageData.maxY) / 2f,
                0f
            );

            Vector3 size = new Vector3(
                stageData.maxX - stageData.minX,
                stageData.maxY - stageData.minY,
                0f
            );

            Gizmos.DrawWireCube(center, size);
        }
    }
#endif

    public void ApplyStageDataOffset(Vector3 delta)
    {
        if (stageDataArray == null) return;

        foreach (var stageData in stageDataArray)
        {
            if (stageData == null) continue;

            stageData.minX += delta.x;
            stageData.maxX += delta.x;
            stageData.minY += delta.y;
            stageData.maxY += delta.y;
        }
    }

    private void OnValidate()
    {
        _lastPosition = transform.position;
    }

    public Vector3 GetLastPosition() => _lastPosition;
    public void SetLastPosition(Vector3 pos) => _lastPosition = pos;
}
