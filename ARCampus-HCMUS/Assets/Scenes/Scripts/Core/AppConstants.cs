// Core/AppConstants.cs
/// <summary>
/// Toàn bộ magic numbers và hardcoded values của project.
/// Khi cần thay đổi một con số, chỉ cần sửa ở đây.
/// </summary>
public static class GeoConstants
{
    public const float EarthRadiusMeters = 6371000f;
    public const float MetersPerDegreeLat = 110540f;
    public const float MetersPerDegreeLng = 111320f;
}

public static class NavigationConstants
{
    public const float ArrivalThresholdMeters = 10f;
    public const float ArrowDistance = 2f;
    public const float ArrowHeightOffset = -0.3f;
    public const float LineYOffset = -0.5f;
    public const float LabelFloatHeight = 1.5f;
}

public static class UIConstants
{
    public const float SwipeThresholdPixels = 100f;
    public const float DefaultContainerWidth = 1080f;
    public const float IndoorScanInterval = 0.5f;
    public const float IndoorShowDistanceThreshold = 15f;
}

public static class CampusMapConstants
{
    // Tọa độ góc bản đồ campus — chỉnh ở đây khi cần update
    public const double TopLeftLat = 10.879541374505507;
    public const double TopLeftLng = 106.79134049825655;
    public const double BotRightLat = 10.873023893401822;
    public const double BotRightLng = 106.80315686070759;
}