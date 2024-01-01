using UnityEngine;

public static class ResolutionUtility
{
    /// <summary>
    /// Width가 1024 이상이면 True를 반환합니다.
    /// </summary>
    /// <param name="width">검사할 값</param>
    /// <returns>1024이상이면 True, 아닐시 False</returns>
    public static bool CheckMinimumResolution(int width)
    {
        if (width >= 1920)
            return true;

        return false;
    }

    /// <summary>
    /// 프레임이 60 이상이면 True를 반환합니다.
    /// </summary>
    /// <param name="resolution">검사할 해상도 값</param>
    public static bool CheckMinimumRefreshRateRatio(Resolution resolution)
    {
#if UNITY_EDITOR
        return true;
#else
        float refresh = Mathf.Floor((float)resolution.refreshRateRatio.value);

        if (
            (int)refresh == 60 ||
            (int)refresh == 90 ||
            (int)refresh == 144)
            return true;
#endif


        return false;
    }

    /// <summary>
    /// 16대 9비율이면 True를 반환합니다.
    /// </summary>
    /// <param name="width">검사할 Width 값</param>
    /// <param name="height">검사할 Height 값</param>
    /// <returns>16대 9비율이라면 True를 반환합니다.</returns>
    public static bool Check16To9Ratio(int width, int height)
    {
        float aspectRatio = (float)width/height;
        return Mathf.Approximately(aspectRatio, 16.0f/9.0f);
    }

    /// <summary>
    /// 16대 9비율이면 True를 반환합니다.
    /// </summary>
    /// <param name="resolution">검사할 Resolution</param>
    /// <returns>16대 9비율이라면 True를 반환합니다.</returns>
    public static bool Check16To9Ratio(Resolution resolution)
    {
        float aspectRatio = (float)resolution.width/resolution.height;
        return Mathf.Approximately(aspectRatio, 16.0f/9.0f);
    }

    /// <summary>
    /// 범용적인 해상도만 지원합니다.
    /// </summary>
    /// <param name="resolution">검사할 Resolution</param>
    /// <returns>범용적인 해상도라면 True를 반환합니다.</returns>
    public static bool CheckUniversalResolution(Resolution resolution)
    {
        if (resolution is { width: 7680, height: 4320 }) // 8K
            return true;

        if (resolution is { width: 3840, height: 2160 }) // 4K
            return true;

        if (resolution is { width: 1920, height: 1080 }) // 2K
            return true;

        return false;
    }
}
