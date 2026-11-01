using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 巡检场景相机控制器（编辑器 Play 模式可用）：
/// - WASD 水平移动（跟随相机朝向），Q/E 升降，Shift 加速
/// - 按住鼠标右键拖拽旋转视角
/// 使用新 Input System 包，兼容 activeInputHandler = 新输入系统。
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("移动")]
    [Tooltip("基础移动速度（米/秒）")]
    public float moveSpeed = 5f;
    [Tooltip("按住 Shift 时的加速倍率")]
    public float fastMultiplier = 2f;

    [Header("视角")]
    [Tooltip("右键拖拽灵敏度（度/像素）")]
    public float lookSensitivity = 0.15f;
    [Tooltip("俯仰角限制（度）")]
    public float pitchLimit = 89f;

    float yaw;
    float pitch;

    void Start()
    {
        // 以场景中摆放的初始朝向为起点
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = euler.x > 180f ? euler.x - 360f : euler.x;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.isPressed)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, -pitchLimit, pitchLimit);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMove()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // 输入框获得焦点时，WASD/QE 只用于文本输入，不驱动相机
        if (IsTypingInInputField())
        {
            return;
        }

        Vector3 input = Vector3.zero;
        if (keyboard.wKey.isPressed) input.z += 1f;
        if (keyboard.sKey.isPressed) input.z -= 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.eKey.isPressed) input.y += 1f;
        if (keyboard.qKey.isPressed) input.y -= 1f;
        if (input == Vector3.zero)
        {
            return;
        }

        float speed = moveSpeed;
        if (keyboard.shiftKey.isPressed)
        {
            speed *= fastMultiplier;
        }

        // 水平移动跟随相机朝向（不含俯仰），升降沿世界 Y 轴
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
        Vector3 horizontal = yawRotation * new Vector3(input.x, 0f, input.z);
        Vector3 move = (horizontal + Vector3.up * input.y).normalized;
        transform.position += move * speed * Time.deltaTime;
    }

    /// <summary>当前是否有 InputField 正在编辑（有焦点）。</summary>
    bool IsTypingInInputField()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            return false;
        }

        InputField field = selected.GetComponent<InputField>();
        return field != null && field.isFocused;
    }
}
