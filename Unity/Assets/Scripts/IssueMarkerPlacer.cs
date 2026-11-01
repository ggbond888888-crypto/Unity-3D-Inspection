using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 鼠标左键射线放置问题标记：
/// - 左键点击时用 Camera.ScreenPointToRay + Physics.Raycast 命中物理碰撞体
/// - 在命中点 hit.point 生成球体标记
/// - 指针落在 UI 上时不生成（EventSystem.IsPointerOverGameObject）
/// - 全局只允许一个待提交标记，再次点击先销毁旧的
/// </summary>
public class IssueMarkerPlacer : MonoBehaviour
{
    public static IssueMarkerPlacer Instance { get; private set; }

    [Header("标记表现")]
    [Tooltip("标记球体直径（米）")]
    public float markerScale = 0.2f;
    public Color markerColor = new Color(1f, 0.23f, 0.18f, 1f);
    [Tooltip("射线最大检测距离（米）")]
    public float maxRayDistance = 100f;

    public bool HasPendingMarker => pendingMarker != null;
    public Vector3 PendingPosition => pendingMarker != null ? pendingMarker.transform.position : Vector3.zero;

    GameObject pendingMarker;
    InspectionUI ui;
    Material markerMaterial;

    void Awake()
    {
        Instance = this;

        // 标记材质在运行时创建，避免依赖资源导入
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        markerMaterial = new Material(shader);
        markerMaterial.SetColor("_BaseColor", markerColor);
        markerMaterial.SetColor("_Color", markerColor);
    }

    void Start()
    {
        ui = FindFirstObjectByType<InspectionUI>();
        ui?.SetPending(false, Vector3.zero);
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // UI 防误触：点击落在 UI 上时，既不放置标记也不影响已有标记
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 表单打开期间禁用场景打点，避免填写时误点替换标记
        if (ui != null && ui.IsFormOpen)
        {
            return;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            return;
        }

        // 命中历史问题标记：弹出详情面板，不生成新的待提交标记
        var view = hit.collider.GetComponent<IssueMarkerView>();
        if (view != null && view.manager != null && view.issue != null)
        {
            view.manager.ShowDetail(view.issue);
            return;
        }

        // 命中带 DeviceInfo 的设备时，上报自动携带设备编号与设备绑定区域；
        // 否则按命中点 / 命中面法线归属空间区域（地面分区 / 墙面区）
        var device = hit.collider.GetComponentInParent<DeviceInfo>();
        string area = ResolveArea(device, hit);
        PlaceMarker(hit.point, device, area);
    }

    static string ResolveArea(DeviceInfo device, RaycastHit hit)
    {
        if (device != null && !string.IsNullOrEmpty(device.area))
        {
            return device.area;
        }
        return AreaResolver.FromHit(hit.point, hit.normal);
    }

    void PlaceMarker(Vector3 position, DeviceInfo device, string area)
    {
        // 单标记约束：先清掉上一个待提交标记
        ClearPendingMarker();

        pendingMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pendingMarker.name = "PendingIssueMarker";
        pendingMarker.transform.position = position;
        pendingMarker.transform.localScale = Vector3.one * markerScale;

        // 标记自身不参与射线检测，避免遮挡后续点击
        Destroy(pendingMarker.GetComponent<Collider>());
        pendingMarker.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;

        pendingDevice = device;
        pendingArea = area;
        ui?.SetPending(true, position, device);
    }

    DeviceInfo pendingDevice;
    string pendingArea = string.Empty;

    /// <summary>当前待提交标记命中的设备信息；命中普通场景表面时为 null。</summary>
    public DeviceInfo PendingDevice => pendingDevice;

    /// <summary>当前待提交标记归属的区域（设备绑定区域或空间区域）。</summary>
    public string PendingArea => pendingArea;

    /// <summary>移除当前未提交的标记（取消按钮、切换点击都会调用）。</summary>
    public void ClearPendingMarker()
    {
        if (pendingMarker != null)
        {
            Destroy(pendingMarker);
            pendingMarker = null;
        }
        pendingDevice = null;
        pendingArea = string.Empty;
        ui?.SetPending(false, Vector3.zero);
    }
}
