using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 上报问题到 Go 后端。
/// 后端地址为 Inspector 可配置字段，默认本机 8080。
/// </summary>
public class IssueReporter : MonoBehaviour
{
    [Header("后端地址（真机/局域网调试时改为电脑 IP）")]
    public string backendUrl = "http://localhost:8080";

    [Serializable]
    public class PositionPayload
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class IssuePayload
    {
        public string title;
        public string description;
        public string priority;
        public PositionPayload position;
    }

    [Serializable]
    class ErrorPayload
    {
        public string error;
    }

    /// <summary>提交问题；done(是否成功, 给用户看的提示, 后端返回的问题 JSON)。</summary>
    public void Submit(string title, string description, string priority, Vector3 position, Action<bool, string, string> done)
    {
        StartCoroutine(PostIssue(title, description, priority, position, done));
    }

    IEnumerator PostIssue(string title, string description, string priority, Vector3 position, Action<bool, string, string> done)
    {
        var payload = new IssuePayload
        {
            title = title,
            description = description,
            priority = priority,
            position = new PositionPayload { x = position.x, y = position.y, z = position.z }
        };
        string json = JsonUtility.ToJson(payload);
        string url = backendUrl.TrimEnd('/') + "/issues";

        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 后端 201 返回完整问题对象，交给调用方即时生成场景标记
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : null;
                done?.Invoke(true, "上报成功，问题已保存到后端", responseBody);
                yield break;
            }

            done?.Invoke(false, BuildErrorMessage(request, url), null);
        }
    }

    string BuildErrorMessage(UnityWebRequest request, string url)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            return $"无法连接后端服务（{url}），请确认 Go 服务已启动";
        }

        // 后端参数错误会返回 {"error": "..."}，优先展示这句话
        string bodyText = request.downloadHandler != null ? request.downloadHandler.text : null;
        if (!string.IsNullOrEmpty(bodyText))
        {
            try
            {
                var err = JsonUtility.FromJson<ErrorPayload>(bodyText);
                if (err != null && !string.IsNullOrEmpty(err.error))
                {
                    return "上报失败：" + err.error;
                }
            }
            catch
            {
                // 非标准错误 JSON，走下面的通用提示
            }
        }

        return $"上报失败（HTTP {request.responseCode}）：{request.error}";
    }
}
