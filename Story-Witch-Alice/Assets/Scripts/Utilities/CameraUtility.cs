using UnityEngine;

public static class CameraUtility
{
    private static Camera _mainCamera;
    
    /// <summary>
    /// 获取主相机（带缓存）
    /// </summary>
    public static Camera MainCamera
    {
        get
        {
            if (_mainCamera == null || !_mainCamera.isActiveAndEnabled)
                _mainCamera = Camera.main;
            return _mainCamera;
        }
    }

    /// <summary>
    /// 将鼠标位置转换为世界坐标，Z 轴固定为 0
    /// </summary>
    public static Vector3 MouseToWorld(float z = 0f)
    {
        if (MainCamera == null)
        {
            Debug.LogError("[CameraUtility] 未找到主相机！");
            return Vector3.zero;
        }
        
        Vector3 screen = Input.mousePosition;
        screen.z = z;
        Vector3 world = MainCamera.ScreenToWorldPoint(screen);
        world.z = z;
        return world;
    }

    /// <summary>
    /// 将屏幕坐标转换为世界坐标，Z 轴固定为 0
    /// </summary>
    public static Vector3 ScreenToWorld(Vector3 screenPos, float z = 0f)
    {
        if (MainCamera == null)
        {
            Debug.LogError("[CameraUtility] 未找到主相机！");
            return Vector3.zero;
        }
        
        Vector3 screen = screenPos;
        screen.z = z;
        Vector3 world = MainCamera.ScreenToWorldPoint(screen);
        world.z = z;
        return world;
    }
}