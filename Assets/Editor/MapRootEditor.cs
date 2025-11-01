using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapRootEditor
{
    // 클래스가 로드될 때 EditorApplication.update 이벤트 등록
    static MapRootEditor()
    {
        EditorApplication.update += UpdateMapRoots;
    }

    // 이전 위치를 저장할 Dictionary
    private static readonly System.Collections.Generic.Dictionary<MapRoot, Vector3> lastPositions
        = new System.Collections.Generic.Dictionary<MapRoot, Vector3>();

    private static void UpdateMapRoots()
    {
        if (Application.isPlaying) return; // 플레이 모드에서는 동작하지 않게

        // 씬 안에 있는 모든 MapRoot 찾기
        foreach (var mapRoot in GameObject.FindObjectsOfType<MapRoot>())
        {
            Vector3 lastPos;
            if (!lastPositions.TryGetValue(mapRoot, out lastPos))
            {
                // 처음 발견된 경우 현재 위치로 초기화
                lastPositions[mapRoot] = mapRoot.transform.position;
                continue;
            }

            Vector3 currentPos = mapRoot.transform.position;
            if (currentPos != lastPos)
            {
                Vector3 delta = currentPos - lastPos;

                // Undo 시스템 등록 (Ctrl+Z/Redo 지원)
                if (mapRoot.stageDataArray != null && mapRoot.stageDataArray.Length > 0)
                    Undo.RecordObjects(mapRoot.stageDataArray, "Move MapRoot");

                // ScriptableObject 좌표 보정
                mapRoot.ApplyStageDataOffset(delta);

                // 변경된 ScriptableObject를 저장 표시
                if (mapRoot.stageDataArray != null)
                {
                    foreach (var stageData in mapRoot.stageDataArray)
                    {
                        if (stageData != null)
                            EditorUtility.SetDirty(stageData);
                    }
                }

                // 마지막 위치 갱신
                lastPositions[mapRoot] = currentPos;
            }
        }
    }
}
