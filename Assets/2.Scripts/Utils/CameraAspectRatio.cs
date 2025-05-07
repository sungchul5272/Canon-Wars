using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    int _lastWidth;
    int _lastHeight;
    float _targetAspect = 16f / 9f;

    void Start()
    {
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
    }

    void Update()
    {
        int width = Screen.width;
        int height = Screen.height;

        if (width != _lastWidth || height != _lastHeight)
        {
            _lastWidth = width;
            _lastHeight = height;
            Debug.Log("해상도 고정");

            float currentAspect = (float)width / height;

            if (Mathf.Abs(currentAspect - _targetAspect) > 0.01f)
            {
                int correctedHeight = Mathf.RoundToInt(width / _targetAspect);
                Screen.SetResolution(width, correctedHeight, false);
                Debug.Log("해상도 수정됨");
            }
        }
    }
}
