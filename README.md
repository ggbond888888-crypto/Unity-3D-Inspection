# 虚拟现场巡检问题标注系统（无 AR 设备备选版）

Unity 虚拟现场 + Go 后端 + React 管理端的三端闭环 Demo：
Unity 进入虚拟巡检现场 → 点击设备 / 场景表面放置问题标记 → 填写并上报 → Go 后端保存 → React 页面查看问题并修改处理状态。

本方案对应题目《Unity 研发实操题（无 AR 设备备选版）：虚拟现场巡检问题标注》。**不要求真实 AR，也无需真实设备**，Unity 编辑器即可运行完整闭环。

## 使用版本
- Go：1.27.3（Gin v1.12.0）
- Node.js：22.23.2 / npm：10.9.8
- React 19.3.1 + TypeScript（Vite 8.3.5 构建）
- Unity：6 LTS（6000.3.27f1）
- 数据存储：本地 JSON（`Go/issues.json`），无需额外数据库服务

## 接口与数据格式
> 接口路径与响应格式为自行设计，三端（Unity / React / Go）共用同一套契约。

**问题数据格式（Issue）**
```json
{
  "id": "后端生成 32 位十六进制 ID",
  "title": "1 号机柜温度异常",
  "description": "机柜右侧温度明显偏高",
  "priority": "high",
  "status": "open",
  "position": { "x": 1.25, "y": 0.8, "z": 2.4 },
  "createdAt": "后端生成（RFC3339）",
  "updatedAt": "后端生成（RFC3339）"
}
```
- `priority`：`low` / `medium` / `high`；`status`：`open` / `in_progress` / `resolved`
- `POST /issues` 请求体只需 `title` / `description` / `priority` / `position`；`id`、`status`（固定 `open`）、`createdAt`、`updatedAt` 由后端生成

**接口一览**

| 方法 | 路径 | 请求体 | 成功响应 | 错误响应 |
|-|-|-|-|-|
| GET | `/health` | 无 | `200` `{"status":"ok","time":...}` | — |
| GET | `/issues` | 无 | `200` Issue 数组 | — |
| POST | `/issues` | `{title,description,priority,position}` | `201` 完整 Issue | `400` `{"error":"原因"}` |
| PATCH | `/issues/:id/status` | `{"status":"in_progress"}` | `200` 更新后 Issue | `400`/`404` `{"error":"原因"}` |

- 参数错误（`title` 为空、`priority`/`status` 非法）返回 `400`；按 ID 找不到记录返回 `404`。

## 启动方式
> 备选版三个部分都在同一台电脑上运行，无需局域网与额外设备。

### 1. Go 后端（默认端口 8080）
```bash
cd "E:\Game Coding\Unity\Go"
go mod download
go run .
```
> 必须用 `go run .`（编译整个 main 包），因为 `main.go` 引用了同目录 `store.go` / `api.go` 的函数；用 `go run main.go` 只编译单文件会报 `undefined` 错误。
数据文件为 `Go/issues.json`（可用环境变量 `ISSUES_FILE` 指定其他路径）。

### 2. React 管理端（默认端口 5173）
```bash
cd "E:\Game Coding\Unity\React"
npm install
npm run dev
```
浏览器打开 `http://localhost:5173`（API 指向 `http://localhost:8080`，见 `src` 下的 api 模块）。

### 3. Unity 客户端（虚拟现场）
用 Unity 6 LTS 打开工程 `E:\Game Coding\Unity-AR`，打开虚拟现场场景（改造后场景，位于 `Assets/Scenes/`），点击 **Play** 在编辑器内运行：
- 移动 / 转动视角浏览虚拟现场；
- 点击设备或场景表面放置问题标记；
- 填写表单上报，数据写入同一 Go 后端。

> 无需额外设备、无需局域网 IP；Unity 端上报地址用 `http://localhost:8080`（编辑器与后端同一台电脑）。

## 后端地址与接口设计
见上文「接口与数据格式」。Unity 的 `IssueApiClient` 在 Inspector 的 **Base Url** 字段配置后端地址，编辑器测试填 `http://localhost:8080` 即可。

## 完成情况

### 已完成
- **Go 后端（全部）**：`/health` 健康检查；`POST /issues` 接收并持久化；`GET /issues` 返回列表；`PATCH /issues/:id/status` 修改状态；自动生成唯一 ID、新问题默认 `open`；空标题 / 非法 priority、status 返回 400；JSON 持久化（重启数据不丢）；CORS 跨域。
- **React 管理端（全部）**：列表展示标题 / 优先级 / 状态；状态修改 `open` / `in_progress` / `resolved`；loading / 空列表处理；后端不可用时提示错误、不白屏，可重试。
- **三端数据契约一致**：`id / title / description / priority / status / position / createdAt / updatedAt`。

### 未完成 / 进行中（Unity 改造）
- **【改造中】Unity 由原版本改造为备选版虚拟现场**：当前 `E:\Game Coding\Unity-AR` 工程仍为原版本，需按下述备选版要求改造：
  - 用 Unity 基础对象搭建虚拟现场（地面、墙面、至少两个可点击设备）；
  - 移除原版本中原有组件与脚本，改用普通 `Camera` + 相机移动脚本（WASD + 鼠标视角）实现场景漫游；
  - 用普通物理射线 `Physics.Raycast` 点击设备 / 场景表面，在命中位置放置标记；
  - 一次有效点击最多创建一个待提交标记；取消填写表单时移除未保存标记（必做 3.1 第 8 条）；
  - 保留：表单 UI、`ARReportForm.cs`、`IssueApiClient.cs` 网络上报逻辑（无需改动）。
- 改造完成并人工核验通过后，此处更新为「已完成」。

## 已知问题 / 注意事项
**代码层面**
- Unity 上报脚本 `IssueApiClient` 的 Base Url 默认值是**示例 IP** `http://192.168.1.100:8080`，未修改时会连不到本机后端；编辑器测试前必须在 Inspector 改成 `http://localhost:8080`。
- Go 后端数据文件 `issues.json` 用的是**相对路径**，实际落盘位置取决于启动时的工作目录；请始终在 `Go` 目录下启动，或显式设置环境变量 `ISSUES_FILE` 为绝对路径。
- Windows PowerShell 默认执行策略限制会导致 `npm.ps1` 无法运行，React 需用 cmd 或管理员模式执行 `npm run dev`。
- 项目路径含空格（`Game Coding`），`cd` 时需用引号包住完整路径。

**备选版改造**
- Unity 工程当前仍为原版本，虚拟现场场景、射线放置、相机控制等尚未改造完成；提交前需按上文改造清单完成并核验。
- 原版本相关已知问题（设备 / 平台限制等）在本备选版中不再适用。

## AR 基础理解（备选版必答，不要求实现）
> 由于备选版不进行真实设备测试，此处简要回答三道基础问题。

**1. 如果把虚拟场景标注改成真实 AR，平面检测和点击放置通常需要哪些 AR Foundation 能力？**
- `ARSession` / `ARSessionOrigin`：管理 AR 会话与坐标系，是所有追踪能力的基础。
- `ARPlaneManager`：开启平面检测，识别真实环境中的水平 / 竖直平面并可视化（可搭配 `AR Feathered Plane` 等平面预制体）。
- `ARRaycastManager`：把屏幕点击发射射线命中真实平面，返回命中点的 `Pose`（位置 + 朝向）。
- 点击放置：用 `ARRaycastManager.Raycast` 命中平面后，在该 `Pose` 处实例化标记物体即可；标记锚定在真实空间，随设备移动保持相对世界坐标不变。

**2. 为什么只保存 Unity 中的 x/y/z 坐标，通常不能保证应用下次启动后仍对应同一个真实位置？**
- 坐标是相对于 AR 会话原点的，而每次启动时 AR 会话都会重新建立原点（取决于设备启动时的位置与初始化结果），原点不固定，坐标也就失去可比性。
- 真实平面检测是相对的，重启后即使识别到同一张桌子，其相对于新原点的坐标也可能不同。
- 要实现跨启动的稳定对应，需要持久化的空间锚点或世界地图（如 ARKit 的 `ARWorldMap` / ARCore 的 Cloud Anchors、保存平面数据库），把标记绑定到真实空间的锚点，而不是裸坐标。

**3. 当 AR 初始化失败、设备不支持或 Tracking 状态异常时，应用应该如何提示用户？**
- 设备不支持：通过 `ARSession` 能力检测或平台 API 判断是否支持 AR（如是否支持 ARKit / ARCore），不支持则显示「当前设备不支持 AR，无法使用该功能」并禁用相关入口。
- 初始化失败：监听 `ARSession.state`（如 `SessionState.Unsupported` / `Failed`），显示「AR 初始化失败，请检查相机权限或重启应用」并提供重试按钮。
- Tracking 状态异常 / 丢失：监听追踪状态（如 `TrackingState.Limited` / `None`），提示「追踪丢失，请缓慢移动设备、改善光线或对准有明显纹理的平面」；恢复后自动清除提示，避免在追踪异常时放置或上报错位标记。
