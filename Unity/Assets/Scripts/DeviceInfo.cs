using UnityEngine;

/// <summary>
/// 设备信息：挂在机柜等可交互设备上。
/// 射线命中带此组件的物体上报问题时，自动携带设备编号与区域。
/// </summary>
public class DeviceInfo : MonoBehaviour
{
    [Header("设备编号，例如 CAB-01")]
    public string deviceId = "";

    [Header("所属区域，例如 A区 / B区")]
    public string area = "";
}
