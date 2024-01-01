using UnityEngine;

public class ResolutionData
{
    public readonly int Width;
    public readonly int Height;
    public RefreshRate RefreshRateRatio;

    public ResolutionData(int width, int height, RefreshRate refreshRateRatio)
    {
        Width = width;
        Height = height;
        RefreshRateRatio = refreshRateRatio;
    }

    public override string ToString()
    {
#if UNITY_EDITOR
        // RefreshRateRatio을 반올림
        float refreshRate = 60f;
#else
        // RefreshRateRatio을 내림
        float refreshRate = Mathf.Floor((float)RefreshRateRatio.value);
#endif

        return $"{Width} x {Height} ({refreshRate})";
    }
}