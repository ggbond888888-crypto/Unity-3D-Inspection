using UnityEngine;

/// <summary>
/// 区域归属：场景沿 x 轴以 x=0 为界分为左右两区。
/// - x &lt; 0 → A区（左侧）
/// - x &gt;= 0 → B区（右侧）
/// 不同表面类型带不同后缀：地面 → A区-地面 / B区-地面，墙面 → A区-墙面 / B区-墙面。
/// 设备优先使用 DeviceInfo 中绑定的区域（如 A区-机柜区、B区-管道区）。
/// </summary>
public static class AreaResolver
{
    public const string AreaA = "A区";
    public const string AreaB = "B区";

    public const string WallSuffix = "-墙面";
    public const string GroundSuffix = "-地面";

    /// <summary>按命中点坐标判定墙面区域（A区-墙面 / B区-墙面）。</summary>
    public static string FromHit(Vector3 point, Vector3 normal)
    {
        return ZoneOf(point) + WallSuffix;
    }

    /// <summary>按坐标分区（地面等水平面），返回 A区-地面 / B区-地面。</summary>
    public static string FromPoint(Vector3 point)
    {
        return ZoneOf(point) + GroundSuffix;
    }

    /// <summary>x=0 为界，左侧 A区，右侧 B区。</summary>
    static string ZoneOf(Vector3 point)
    {
        return point.x < 0f ? AreaA : AreaB;
    }
}
