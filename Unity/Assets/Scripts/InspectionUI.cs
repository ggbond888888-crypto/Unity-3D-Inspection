using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 巡检界面（运行时构建，避免依赖预制体 / TMP 字体资源）：
/// - 左上角操作提示
/// - 放置标记后弹出上报表单：标题（必填）、描述（选填）、优先级下拉（low/medium/high）
/// - 上报 / 取消按钮；上报结果以弹窗提示
/// </summary>
public class InspectionUI : MonoBehaviour
{
    const string LOW = "low";
    const string MEDIUM = "medium";
    const string HIGH = "high";

    GameObject formPanel;
    GameObject popupPanel;
    GameObject priorityList;

    Text pendingInfoText;
    Text priorityLabel;
    Text popupText;
    InputField titleInput;
    InputField descInput;

    GameObject detailPanel;
    Text detailText;

    string priority = LOW;
    IssueReporter reporter;

    void Awake()
    {
        EnsureEventSystem();
        BuildCanvas();
    }

    void Start()
    {
        reporter = FindFirstObjectByType<IssueReporter>();
        formPanel.SetActive(false);
        popupPanel.SetActive(false);
        priorityList.SetActive(false);
        detailPanel.SetActive(false);
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var go = new GameObject("EventSystem");
        go.transform.SetParent(transform, false);
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void BuildCanvas()
    {
        var canvasGO = new GameObject("InspectionCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        BuildHint(canvasGO.transform);
        BuildForm(canvasGO.transform);
        BuildDetailPanel(canvasGO.transform);
        BuildPopup(canvasGO.transform);   // 最后创建，保证弹窗显示在最上层
    }

    // ---------- 提示 ----------

    void BuildHint(Transform parent)
    {
        Text hint = CreateText(parent, "Hint", 24, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.9f));
        hint.text = "WASD 移动 ｜ 右键拖拽转视角 ｜ 左键点击设备或场景表面放置标记";
        hint.raycastTarget = false;

        RectTransform rt = hint.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f, -20f);
        rt.sizeDelta = new Vector2(900f, 40f);

        var shadow = hint.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);
    }

    // ---------- 上报表单 ----------

    void BuildForm(Transform parent)
    {
        formPanel = new GameObject("FormPanel");
        formPanel.transform.SetParent(parent, false);

        var bg = formPanel.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.13f, 0.15f, 0.96f);

        RectTransform panelRt = formPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = new Vector2(720f, 440f);

        Text title = CreateText(formPanel.transform, "Title", 30, TextAnchor.MiddleLeft, Color.white);
        Place(title.rectTransform, 30f, -24f, 400f, 40f);
        title.text = "上报巡检问题";

        pendingInfoText = CreateText(formPanel.transform, "Position", 22, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.9f, 1f));
        Place(pendingInfoText.rectTransform, 30f, -64f, 620f, 30f);

        CreateFieldLabel("标题（必填）", 30f, -106f);
        titleInput = CreateInputField(formPanel.transform, "例如：机柜散热口温度过高", 190f, -106f, 470f, 46f);

        CreateFieldLabel("描述（选填）", 30f, -164f);
        descInput = CreateInputField(formPanel.transform, "补充说明（可留空）", 190f, -164f, 470f, 46f);

        CreateFieldLabel("优先级", 30f, -222f);
        BuildPriorityDropdown(190f, -222f);

        CreateButton(formPanel.transform, "取消", new Color(0.35f, 0.35f, 0.38f, 1f), -28f, 26f, 130f, 52f, Cancel, true);
        CreateButton(formPanel.transform, "上报", new Color(0.16f, 0.6f, 0.32f, 1f), -170f, 26f, 150f, 52f, Submit, true);
    }

    void CreateFieldLabel(string text, float x, float y)
    {
        Text label = CreateText(formPanel.transform, "Label_" + text, 24, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.88f, 1f));
        Place(label.rectTransform, x, y, 150f, 40f);
        label.text = text;
    }

    void BuildPriorityDropdown(float x, float y)
    {
        var buttonGO = new GameObject("PriorityButton");
        buttonGO.transform.SetParent(formPanel.transform, false);

        var img = buttonGO.AddComponent<Image>();
        img.color = new Color(0.25f, 0.27f, 0.3f, 1f);

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(TogglePriorityList);

        RectTransform rt = buttonGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(150f, 38f);

        priorityLabel = CreateText(buttonGO.transform, "Label", 22, TextAnchor.MiddleLeft, Color.white);
        Stretch(priorityLabel.rectTransform, 14f, 0f, 14f, 0f);
        priorityLabel.text = "low  ▾";
        priorityLabel.raycastTarget = false;

        // 下拉列表：三条选项按钮，覆盖在表单上方
        priorityList = new GameObject("PriorityList");
        priorityList.transform.SetParent(formPanel.transform, false);

        var listBg = priorityList.AddComponent<Image>();
        listBg.color = new Color(0.2f, 0.22f, 0.25f, 1f);

        RectTransform listRt = priorityList.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 1f);
        listRt.anchorMax = new Vector2(0f, 1f);
        listRt.pivot = new Vector2(0f, 1f);
        listRt.anchoredPosition = new Vector2(x, y - 42f);
        listRt.sizeDelta = new Vector2(150f, 108f);

        BuildPriorityOption(priorityList.transform, LOW, 0f);
        BuildPriorityOption(priorityList.transform, MEDIUM, 36f);
        BuildPriorityOption(priorityList.transform, HIGH, 72f);
    }

    void BuildPriorityOption(Transform parent, string value, float yOffset)
    {
        var go = new GameObject("Option_" + value);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.26f, 0.28f, 0.32f, 1f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(() => SelectPriority(value));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yOffset);
        rt.sizeDelta = new Vector2(150f, 36f);

        Text label = CreateText(go.transform, "Label", 22, TextAnchor.MiddleLeft, Color.white);
        Stretch(label.rectTransform, 14f, 0f, 14f, 0f);
        label.text = value;
        label.raycastTarget = false;
    }

    // ---------- 历史问题详情面板 ----------

    void BuildDetailPanel(Transform parent)
    {
        detailPanel = new GameObject("DetailPanel");
        detailPanel.transform.SetParent(parent, false);

        var bg = detailPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.11f, 0.13f, 0.96f);

        RectTransform rt = detailPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-40f, 0f);
        rt.sizeDelta = new Vector2(460f, 420f);

        Text title = CreateText(detailPanel.transform, "Title", 28, TextAnchor.MiddleLeft, Color.white);
        Place(title.rectTransform, 24f, -18f, 400f, 40f);
        title.text = "问题详情";

        detailText = CreateText(detailPanel.transform, "Content", 20, TextAnchor.UpperLeft, new Color(0.88f, 0.9f, 0.92f, 1f));
        Place(detailText.rectTransform, 24f, -64f, 410f, 280f);
        detailText.verticalOverflow = VerticalWrapMode.Overflow;

        CreateButton(detailPanel.transform, "删除该问题", new Color(0.75f, 0.2f, 0.18f, 1f), -140f, 18f, 150f, 46f, DeleteCurrentDetail, true);
        CreateButton(detailPanel.transform, "关闭", new Color(0.35f, 0.35f, 0.38f, 1f), -24f, 18f, 110f, 46f, CloseDetail, true);
    }

    string detailIssueId;

    void CloseDetail()
    {
        detailPanel.SetActive(false);
        detailIssueId = null;
    }

    void DeleteCurrentDetail()
    {
        if (string.IsNullOrEmpty(detailIssueId))
        {
            return;
        }
        if (IssueMarkerManager.Instance == null)
        {
            ShowPopup("场景缺少 IssueMarkerManager 组件，无法删除");
            return;
        }
        SetDetailButtonsInteractable(false);
        IssueMarkerManager.Instance.DeleteIssue(detailIssueId, OnIssueDeleted);
    }

    void OnIssueDeleted(bool success, string id, string message)
    {
        SetDetailButtonsInteractable(true);
        if (success)
        {
            CloseDetail();
        }
        ShowPopup(message);
    }

    void SetDetailButtonsInteractable(bool value)
    {
        foreach (var button in detailPanel.GetComponentsInChildren<Button>())
        {
            button.interactable = value;
        }
    }

    /// <summary>点击历史标记时由 IssueMarkerManager 调用。</summary>
    public void ShowIssueDetail(string title, string description, string priority, string status, Vector3 position, string deviceId = "", string area = "", string id = "")
    {
        string priorityText = PriorityDisplayName(priority);
        string statusText = StatusDisplayName(status);

        string body = $"标题：{title}\n\n";
        body += $"描述：{(string.IsNullOrEmpty(description) ? "（无）" : description)}\n\n";
        body += $"优先级：{priorityText}（{priority}）\n";
        body += $"状态：{statusText}（{status}）\n";
        if (!string.IsNullOrEmpty(deviceId) || !string.IsNullOrEmpty(area))
        {
            body += $"设备：{(string.IsNullOrEmpty(deviceId) ? "（无）" : deviceId)}\n";
            body += $"区域：{(string.IsNullOrEmpty(area) ? "（无）" : area)}\n";
        }
        body += $"坐标：({position.x:F2}, {position.y:F2}, {position.z:F2})";

        detailText.text = body;
        detailIssueId = id;
        detailPanel.SetActive(true);
        detailPanel.transform.SetAsLastSibling();
    }

    static string PriorityDisplayName(string value)
    {
        switch (value)
        {
            case "high": return "高";
            case "medium": return "中";
            case "low": return "低";
            default: return value;
        }
    }

    static string StatusDisplayName(string value)
    {
        switch (value)
        {
            case "open": return "待处理";
            case "in_progress": return "处理中";
            case "resolved": return "已解决";
            default: return value;
        }
    }

    // ---------- 结果弹窗 ----------

    void BuildPopup(Transform parent)
    {
        popupPanel = new GameObject("PopupPanel");
        popupPanel.transform.SetParent(parent, false);

        var bg = popupPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.09f, 0.97f);

        RectTransform rt = popupPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 140f);
        rt.sizeDelta = new Vector2(560f, 200f);

        popupText = CreateText(popupPanel.transform, "Message", 28, TextAnchor.MiddleCenter, Color.white);
        Stretch(popupText.rectTransform, 28f, 76f, 28f, 20f);

        CreateButton(popupPanel.transform, "确定", new Color(0.2f, 0.45f, 0.8f, 1f), -190f, 24f, 120f, 44f, ClosePopup, false);
    }

    // ---------- 交互 ----------

    void TogglePriorityList()
    {
        priorityList.SetActive(!priorityList.activeSelf);
        if (priorityList.activeSelf)
        {
            priorityList.transform.SetAsLastSibling();
        }
    }

    void SelectPriority(string value)
    {
        priority = value;
        priorityLabel.text = value + "  ▾";
        priorityList.SetActive(false);
    }

    void Cancel()
    {
        priorityList.SetActive(false);
        popupPanel.SetActive(false);
        IssueMarkerPlacer.Instance?.ClearPendingMarker();
    }

    void Submit()
    {
        string title = titleInput.text.Trim();
        if (title.Length == 0)
        {
            ShowPopup("标题不能为空，请填写后再上报");
            return;
        }

        if (reporter == null)
        {
            ShowPopup("场景缺少 IssueReporter 组件，无法上报");
            return;
        }

        Vector3 position = IssueMarkerPlacer.Instance != null
            ? IssueMarkerPlacer.Instance.PendingPosition
            : Vector3.zero;

        // 区域在打点时已定（设备绑定区域优先，否则按命中点 / 墙面法线归属），提交时直接沿用
        DeviceInfo device = IssueMarkerPlacer.Instance?.PendingDevice;
        string deviceId = device != null ? device.deviceId : string.Empty;
        string area = IssueMarkerPlacer.Instance?.PendingArea ?? string.Empty;

        SubmitButtonInteractable(false);
        reporter.Submit(title, descInput.text.Trim(), priority, position, deviceId, area, OnSubmitted);
    }

    void SubmitButtonInteractable(bool value)
    {
        // 防止重复点击：提交期间禁用上报按钮
        foreach (var button in formPanel.GetComponentsInChildren<Button>())
        {
            if (button.name == "Button_上报")
            {
                button.interactable = value;
            }
        }
    }

    void OnSubmitted(bool success, string message, string responseJson)
    {
        SubmitButtonInteractable(true);
        ShowPopup(message);
        if (success)
        {
            // 用后端返回的问题数据立即生成历史标记，并清空待提交标记（同时关闭表单）
            IssueMarkerManager.Instance?.SpawnMarkerFromJson(responseJson);
            IssueMarkerPlacer.Instance?.ClearPendingMarker();
        }
    }

    void ShowPopup(string message)
    {
        popupText.text = message;
        popupPanel.SetActive(true);
        popupPanel.transform.SetAsLastSibling();
    }

    void ClosePopup()
    {
        popupPanel.SetActive(false);
    }

    // ---------- 由 IssueMarkerPlacer 调用 ----------

    /// <summary>表单是否处于打开状态（打开时禁用场景打点）。</summary>
    public bool IsFormOpen => formPanel != null && formPanel.activeSelf;

    /// <summary>有/无待提交标记时切换表单；新标记会重置表单内容。命中设备时附带设备信息。</summary>
    public void SetPending(bool hasPending, Vector3 position, DeviceInfo device = null)
    {
        if (formPanel == null)
        {
            return;
        }

        formPanel.SetActive(hasPending);
        priorityList.SetActive(false);

        if (hasPending)
        {
            string area = IssueMarkerPlacer.Instance != null ? IssueMarkerPlacer.Instance.PendingArea : AreaResolver.FromPoint(position);
            string info = $"标记坐标：({position.x:F2}, {position.y:F2}, {position.z:F2}) ｜ 区域：{area}";
            if (device != null && !string.IsNullOrEmpty(device.deviceId))
            {
                info += $" ｜ 设备：{device.deviceId}";
            }
            pendingInfoText.text = info;
            titleInput.text = string.Empty;
            descInput.text = string.Empty;
            SelectPriority(LOW);
        }
        else
        {
            popupPanel.SetActive(false);
        }
    }

    // ---------- 构建工具 ----------

    Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = GetUIFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    Button CreateButton(Transform parent, string label, Color color, float x, float y, float width, float height, UnityEngine.Events.UnityAction onClick, bool alignBottomRight)
    {
        var go = new GameObject("Button_" + label);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.onClick.AddListener(onClick);

        RectTransform rt = go.GetComponent<RectTransform>();
        if (alignBottomRight)
        {
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
        }
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);

        Text text = CreateText(go.transform, "Label", 26, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.text = label;
        return button;
    }

    InputField CreateInputField(Transform parent, string placeholder, float x, float y, float width, float height)
    {
        var go = new GameObject("InputField");
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.95f);

        var field = go.AddComponent<InputField>();
        field.targetGraphic = bg;
        field.lineType = InputField.LineType.SingleLine;

        Text text = CreateText(go.transform, "Text", 24, TextAnchor.MiddleLeft, new Color(0.08f, 0.08f, 0.1f, 1f));
        Stretch(text.rectTransform, 12f, 4f, 12f, 4f);
        text.supportRichText = false;

        Text placeholderText = CreateText(go.transform, "Placeholder", 24, TextAnchor.MiddleLeft, new Color(0.45f, 0.45f, 0.48f, 1f));
        Stretch(placeholderText.rectTransform, 12f, 4f, 12f, 4f);
        placeholderText.text = placeholder;
        placeholderText.fontStyle = FontStyle.Italic;

        field.textComponent = text;
        field.placeholder = placeholderText;

        Place(go.GetComponent<RectTransform>(), x, y, width, height);
        return field;
    }

    /// <summary>按左上角为锚点定位（x 向右，y 向下为负）。</summary>
    void Place(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    /// <summary>优先使用系统中文字体，避免内置 Arial 缺字显示方块。
    /// 动态字体用较大栅格尺寸创建，再缩放显示，避免小字号文字模糊。</summary>
    Font GetUIFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 128);
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return font;
    }
}
