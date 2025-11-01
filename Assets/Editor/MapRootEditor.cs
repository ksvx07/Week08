using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MapRootEditor
{
    // Ŭ������ �ε�� �� EditorApplication.update �̺�Ʈ ���
    static MapRootEditor()
    {
        EditorApplication.update += UpdateMapRoots;
    }

    // ���� ��ġ�� ������ Dictionary
    private static readonly System.Collections.Generic.Dictionary<MapRoot, Vector3> lastPositions
        = new System.Collections.Generic.Dictionary<MapRoot, Vector3>();

    private static void UpdateMapRoots()
    {
        if (Application.isPlaying) return; // �÷��� ��忡���� �������� �ʰ�

        // �� �ȿ� �ִ� ��� MapRoot ã��
        foreach (var mapRoot in GameObject.FindObjectsOfType<MapRoot>())
        {
            Vector3 lastPos;
            if (!lastPositions.TryGetValue(mapRoot, out lastPos))
            {
                // ó�� �߰ߵ� ��� ���� ��ġ�� �ʱ�ȭ
                lastPositions[mapRoot] = mapRoot.transform.position;
                continue;
            }

            Vector3 currentPos = mapRoot.transform.position;
            if (currentPos != lastPos)
            {
                Vector3 delta = currentPos - lastPos;

                // Undo �ý��� ��� (Ctrl+Z/Redo ����)
                if (mapRoot.stageDataArray != null && mapRoot.stageDataArray.Length > 0)
                    Undo.RecordObjects(mapRoot.stageDataArray, "Move MapRoot");

                // ScriptableObject ��ǥ ����
                mapRoot.ApplyStageDataOffset(delta);

                // ����� ScriptableObject�� ���� ǥ��
                if (mapRoot.stageDataArray != null)
                {
                    foreach (var stageData in mapRoot.stageDataArray)
                    {
                        if (stageData != null)
                            EditorUtility.SetDirty(stageData);
                    }
                }

                // ������ ��ġ ����
                lastPositions[mapRoot] = currentPos;
            }
        }
    }
}
