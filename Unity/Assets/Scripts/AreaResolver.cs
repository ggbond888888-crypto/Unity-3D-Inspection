using UnityEngine;

/// <summary>
/// 非设备表面的区域归属：
/// - 地面 / 设备：按坐标分区（场景 20m × 20m，沿 x 轴等分 A区 / B区 / C区）
/// - 墙面：按命中面法线归属东 / 西 / 南 / 北墙区
/// </summary>
public static class AreaResolver
{
    public const string AreaA = "A区";
    public const string AreaB = "B区";
    public const string AreaC = "C区";

    public const string WallEast = "东墙区";
    public const string WallWest = "西墙区";
    public const string WallSouth = "南墙区";
    public const string WallNorth = "北墙区";

    const float Min = -10f;
    const float Max = 10f;

    /// <summary>按射线命中点与命中面法线归属区域。</summary>
    public static string FromHit(Vector3 point, Vector3 normal)
    {
        // 命中面接近竖直 → 视为墙面；法线指向墙内侧，反推墙所在方位
        if (Mathf.Abs(normal.y) < 0.5f)
        {
            if (Mathf.Abs(normal.x) >= Mathf.Abs(normal.z))
            {
                return normal.x < 0f ? WallEast : WallWest;
            }
            return normal.z < 0f ? WallNorth : WallSouth;
        }

        return FromPoint(point);
    }

    /// <summary>按坐标分区（地面等水平面）。</summary>
    public static string FromPoint(Vector3 point)
    {
        float band = (Max - Min) / 3f;
        if (point.x < Min + band)
        {
            return AreaA;
        }
        if (point.x < Min + band * 2f)
        {
            return AreaB;
        }
        return AreaC;
    }
}
