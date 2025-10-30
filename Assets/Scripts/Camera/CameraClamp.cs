using System.Collections.Generic;
using UnityEngine;

public class CameraClamp : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float SwitchSpeed;

    [SerializeField] public float _minX;
    [SerializeField] public float _maxX;
    [SerializeField] public float _minY;
    [SerializeField] public float _maxY;

    private float _targetMinX, _targetMinY, _targetMaxX, _targetMaxY;
    private float _targetZoom = 6f;
    private float _initialZoom = 9f;

    private void Update()
    {
        _minX = Mathf.Lerp(_minX, _targetMinX, Time.deltaTime * SwitchSpeed);
        _maxX = Mathf.Lerp(_maxX, _targetMaxX, Time.deltaTime * SwitchSpeed);
        _minY = Mathf.Lerp(_minY, _targetMinY, Time.deltaTime * SwitchSpeed);
        _maxY = Mathf.Lerp(_maxY, _targetMaxY, Time.deltaTime * SwitchSpeed);
    }

    public Vector3 HandleClamp(Vector3 desiredPos)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float mapWidth = _maxX - _minX;
        float mapHeight = _maxY - _minY;

        // 맵이 카메라보다 작은 경우, 중앙 고정
        if (mapWidth <= camWidth * 2f && mapHeight <= camHeight * 2f)
        {
            float centerX = (_minX + _maxX) / 2f;
            float centerY = (_minY + _maxY) / 2f;
            return new Vector3(centerX, centerY, desiredPos.z);
        }

        // 가로만 작거나 세로만 작은 경우
        if (mapWidth <= camWidth * 2f)
            desiredPos.x = (_minX + _maxX) / 2f;
        if (mapHeight <= camHeight * 2f)
            desiredPos.y = (_minY + _maxY) / 2f;

        // 일반적인 Clamp 동작
        float clampX = Mathf.Clamp(desiredPos.x, _minX + camWidth, _maxX - camWidth);
        float clampY = Mathf.Clamp(desiredPos.y, _minY + camHeight, _maxY - camHeight);

        return new Vector3(clampX, clampY, desiredPos.z);
    }


    public void SetMapBounds(StageScriptableObject stageData)
    {
        _targetMinX = stageData.minX;
        _targetMaxX = stageData.maxX;
        _targetMinY = stageData.minY;
        _targetMaxY = stageData.maxY;

        float mapWidth = _targetMaxX - _targetMinX;
        float mapHeight = _targetMaxY - _targetMinY;

        float aspect = cam.aspect;

        // 현재 카메라 폭과 높이 계산
        float currentCamHeight = cam.orthographicSize * 2f;
        float currentCamWidth = currentCamHeight * aspect;

        // 맵보다 카메라가 크면, 줄이기 (즉, 줌 인)
        float sizeByWidth = (mapWidth / 2f) / aspect;
        float sizeByHeight = mapHeight / 2f;
        float targetSize = Mathf.Min(sizeByWidth, sizeByHeight);

        // 너무 작게 줄어드는 걸 방지 (최소 줌 제한)
        float minZoomLimit = 3f;
        targetSize = Mathf.Max(targetSize, minZoomLimit);

        cam.GetComponent<CameraController>().TriggerZoom(targetSize);
    }



    public List<float> GetMapBounds()
    {
        var bounds = new List<float>
        {
            _minX,
            _maxX,
            _minY,
            _maxY
        };

        return bounds;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 center = new Vector3((_minX + _maxX) / 2f, (_minY + _maxY) / 2f, 0);
        Vector3 size = new Vector3(_maxX - _minX, _maxY - _minY, 0);

        Gizmos.DrawWireCube(center, size);
    }
}
