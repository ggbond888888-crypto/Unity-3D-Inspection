using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 历史问题标记管理：
/// - 启动时从后端 GET /issues，按 position 在场景恢复标记
/// - 标记颜色按优先级区分：high 红 / medium 黄 / low 蓝
/// - 点击标记时由 IssueMarkerPlacer 转发到 ShowDetail，弹出详情面板
/// </summary>
public class IssueMarkerManager : MonoBehaviour
{
    [Header("后端地址（与 IssueReporter 保持一致）")]
    public string backendUrl = "http://localhost:8080";

    [Header("标记表现")]
    public float markerScale = 0.28f;

    [Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class IssueItem
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public PositionData position;
        public string createdAt;
        public string updatedAt;
    }

    [Serializable]
    class IssueListWrapper
    {
        public IssueItem[] items;
    }

    public static IssueMarkerManager Instance { get; private set; }

    InspectionUI ui;
    Material highMaterial;
    Material mediumMaterial;
    Material lowMaterial;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ui = FindFirstObjectByType<InspectionUI>();

        highMaterial = CreateMaterial(new Color(0.9f, 0.15f, 0.12f, 1f));
        mediumMaterial = CreateMaterial(new Color(0.95f, 0.75f, 0.1f, 1f));
        lowMaterial = CreateMaterial(new Color(0.2f, 0.45f, 0.95f, 1f));

        StartCoroutine(LoadIssues());
    }

    /// <summary>重新从后端加载全部历史问题（上报成功或需要刷新时调用）。
    /// 先清除场景内已有历史标记，再重新 GET 恢复，保证新增问题立即可见。</summary>
    public void Refresh()
    {
        StopAllCoroutines();
        ClearAllMarkers();
        StartCoroutine(LoadIssues());
    }

    void ClearAllMarkers()
    {
        foreach (var view in FindObjectsByType<IssueMarkerView>(FindObjectsSortMode.None))
        {
            Destroy(view.gameObject);
        }
    }

    IEnumerator LoadIssues()
    {
        string url = backendUrl.TrimEnd('/') + "/issues";
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[IssueMarkerManager] 历史问题加载失败：" + request.error);
                yield break;
            }

            string body = request.downloadHandler.text;
            IssueItem[] issues = ParseIssueList(body);
            foreach (IssueItem issue in issues)
            {
                SpawnMarker(issue);
            }
            Debug.Log($"[IssueMarkerManager] 已恢复 {issues.Length} 个历史问题标记");
        }
    }

    /// <summary>JsonUtility 不能直接解析顶层数组，包一层 wrapper。</summary>
    IssueItem[] ParseIssueList(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return Array.Empty<IssueItem>();
        }

        try
        {
            var wrapper = JsonUtility.FromJson<IssueListWrapper>("{\"items\":" + json + "}");
            return wrapper != null && wrapper.items != null ? wrapper.items : Array.Empty<IssueItem>();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[IssueMarkerManager] 问题列表解析失败：" + ex.Message);
            return Array.Empty<IssueItem>();
        }
    }

    /// <summary>上报成功后，用后端返回的问题 JSON 即时生成标记，无需重载场景。</summary>
    public void SpawnMarkerFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        try
        {
            var issue = JsonUtility.FromJson<IssueItem>(json);
            if (issue != null)
            {
                SpawnMarker(issue);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[IssueMarkerManager] 新问题标记解析失败：" + ex.Message);
        }
    }

    void SpawnMarker(IssueItem issue)
    {
        if (issue == null || issue.position == null)
        {
            return;
        }

        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "IssueMarker_" + issue.id;
        marker.transform.position = new Vector3(issue.position.x, issue.position.y, issue.position.z);
        marker.transform.localScale = Vector3.one * markerScale;

        var renderer = marker.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(issue.priority);

        // 保留 Collider 供射线点击；挂上视图组件便于点击时取回数据
        var view = marker.AddComponent<IssueMarkerView>();
        view.issue = issue;
        view.manager = this;
    }

    Material MaterialFor(string priority)
    {
        switch (priority)
        {
            case "high":
                return highMaterial;
            case "medium":
                return mediumMaterial;
            case "low":
            default:
                return lowMaterial;
        }
    }

    Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        var material = new Material(shader);
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        return material;
    }

    /// <summary>点击历史标记时展示详情（由 IssueMarkerPlacer 调用）。</summary>
    public void ShowDetail(IssueItem issue)
    {
        if (ui == null || issue == null)
        {
            return;
        }

        Vector3 position = issue.position != null
            ? new Vector3(issue.position.x, issue.position.y, issue.position.z)
            : Vector3.zero;

        ui.ShowIssueDetail(issue.title, issue.description, issue.priority, issue.status, position);
    }
}

/// <summary>挂在历史标记球上，携带问题数据，供点击命中时回查。</summary>
public class IssueMarkerView : MonoBehaviour
{
    public IssueMarkerManager.IssueItem issue;
    public IssueMarkerManager manager;
}
