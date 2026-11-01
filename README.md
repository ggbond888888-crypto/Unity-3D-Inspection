# 虚拟现场巡检问题标注系统（无 AR 设备备选版）

Unity 虚拟现场 + Go 后端 + React 管理端的三端闭环 Demo：
在 Unity 里进入一个虚拟巡检现场 → 用相机漫游走动 → 点击设备或地面墙面放置问题标记 → 填写表单上报 → Go 后端保存 → React 页面查看工单、修改处理状态、删除工单。

本方案对应题目《Unity 研发实操题（无 AR 设备备选版）：虚拟现场巡检问题标注》。**不要求真实 AR，也不需要真机**，Unity 编辑器里直接跑完整闭环。

## 使用版本
- Go：1.27.3（Gin v1.12.0）
- Node.js：22.23.2 / npm：10.9.8
- React 19.3.1 + TypeScript（Vite 8.3.5 构建）
- Unity：6 LTS（6000.3.27f1）
- 数据存储：本地 JSON（`Go/issues.json`），无需额外数据库服务

## 接口与数据格式

**问题数据格式（Issue）**
```json
{
  "id": "后端生成 32 位十六进制 ID",
  "title": "1 号机柜温度异常",
  "description": "机柜右侧温度明显偏高",
  "priority": "high",
  "status": "open",
  "position": { "x": 1.25, "y": 0.8, "z": 2.4 },
  "deviceId": "CAB-01",
  "area": "A区-机柜区",
  "createdAt": "后端生成（RFC3339）",
  "updatedAt": "后端生成（RFC3339）"
}
```
- `priority`：`low` / `medium` / `high`；`status`：`open` / `in_progress` / `resolved`
- `deviceId`：命中设备时才有值（如 `CAB-01`），点地面/墙面为空
- `area`：细分区域，设备带设备区（如 `A区-机柜区`），墙面按朝向（如 `北墙区`），地面按坐标分区
- `POST /issues` 请求体传 `title` / `description` / `priority` / `position` / `deviceId` / `area`；`id`、`status`（固定 `open`）、`createdAt`、`updatedAt` 由后端生成

**接口一览**

| 方法 | 路径 | 请求体 | 成功响应 | 错误响应 |
|-|-|-|-|-|
| GET | `/health` | 无 | `200` `{"status":"ok","time":...}` | — |
| GET | `/issues` | 无 | `200` Issue 数组 | — |
| POST | `/issues` | `{title,description,priority,position,deviceId,area}` | `201` 完整 Issue | `400` `{"error":"原因"}` |
| PATCH | `/issues/:id/status` | `{"status":"in_progress"}` | `200` 更新后 Issue | `400`/`404` `{"error":"原因"}` |
| DELETE | `/issues/:id` | 无 | `204` No Content | `404` `{"error":"原因"}` |

- 参数错误（`title` 为空、`priority`/`status` 非法）返回 `400`；按 ID 找不到记录返回 `404`。

## 启动方式
> 三个部分都在同一台电脑上运行，无需局域网与额外设备。

### 1. Go 后端（默认端口 8080）
```bash
cd "E:\Game Coding\Unity\Go"
go mod download
go run .
```
> 必须用 `go run .`（编译整个 main 包），因为 `main.go` 引用了同目录 `store.go` / `api.go` / `model.go` 的函数；用 `go run main.go` 只编译单文件会报 `undefined` 错误。
> 数据文件为 `Go/issues.json`（可用环境变量 `ISSUES_FILE` 指定其他路径）。

### 2. React 管理端（默认端口 5173）
```bash
cd "E:\Game Coding\Unity\React"
npm install
npm run dev
```
浏览器打开 `http://localhost:5173`（API 指向 `http://localhost:8080`，见 `src/api.ts`）。

### 3. Unity 客户端（虚拟现场）
用 Unity 6 LTS 打开工程 `E:\Game Coding\Unity\Unity`，打开场景 `Assets/Scenes/Main.unity`，点击 **Play** 在编辑器内运行：
- WASD 移动相机、右键拖拽转视角，浏览虚拟现场；
- 点击设备 / 地面 / 墙面放置问题标记（白色球）；
- 启动时自动从后端加载历史工单，按优先级以红 / 黄 / 蓝恢复标记，点击可查看详情、删除；
- 填写表单上报，数据写入同一 Go 后端。

> 无需真机、无需局域网 IP；Unity 端上报地址在 `IssueReporter.cs` 的 Inspector `backendUrl` 字段配置，编辑器测试填 `http://localhost:8080` 即可。

## 完成情况

### 已完成
- **Go 后端**：`/health` 健康检查；`POST /issues` 接收并持久化；`GET /issues` 返回列表；`PATCH /issues/:id/status` 修改状态；`DELETE /issues/:id` 删除工单；自动生成唯一 ID、新问题默认 `open`；空标题 / 非法 priority、status 返回 400；JSON 持久化（重启数据不丢）；CORS 跨域。
- **React 管理端**：列表展示标题 / 描述 / 优先级 / 状态 / 设备编号 / 区域 / 时间；状态修改 `open` / `in_progress` / `resolved`；按优先级、区域、状态筛选；行点击查看完整详情弹窗；分页（每页 30 条）；工单删除（二次确认弹窗）；loading / 空列表 / 后端不可用错误提示与重试，不白屏。
- **Unity 客户端**：
  - 虚拟现场：Plane 地面、Cube 墙面、机柜、管道、设备箱等基础几何体构建，工业风格材质与装饰；
  - 相机漫游：WASD 移动、右键拖拽转视角、Shift 加速、输入框聚焦时不触发移动；
  - 射线打点：`Camera.ScreenPointToRay` + `Physics.Raycast` 在命中点放置标记；UI 防误触；单待提交标记约束；
  - 区域判定：`AreaResolver` 按命中面法线区分墙面 / 地面 / 设备，自动计算细分区域，随上报携带 `deviceId`、`area`；
  - 上报表单：标题必填、描述选填、优先级下拉，自动读取标记坐标 / 区域 / 设备，上报成功清空标记、失败弹窗提示；
  - 历史标记：启动时加载后端工单并按优先级恢复标记，点击查看详情、删除工单；
  - 刷新按钮：左上角手动刷新，同步后端最新工单到场景。
- **三端数据契约一致**：`id / title / description / priority / status / position / deviceId / area / createdAt / updatedAt`。

### 未实现 / 已知局限
- **真实 AR 未接入**：本方案为无 AR 备选版，用虚拟 3D 场景模拟 AR 巡检，未使用 AR Foundation 平面识别（详见下文「AR 基础理解」）。
- Unity 不会自动轮询后端变更：在 React 删除工单后，需点击 Unity 左上角【刷新标记】按钮同步场景标记。

## 已知问题 / 注意事项
- Unity 上报脚本 `IssueReporter` 的 `backendUrl` 默认 `http://localhost:8080`；真机或局域网调试需改为电脑 IP。
- Go 数据文件 `issues.json` 用相对路径，实际落盘位置取决于启动时的工作目录；请在 `Go` 目录下启动，或用环境变量 `ISSUES_FILE` 指定绝对路径。
- Windows PowerShell 默认执行策略会限制运行 `npm.ps1`，React 需用 cmd 或管理员模式执行 `npm run dev`。
- 项目路径含空格（`Game Coding`），命令行 `cd` 时需用引号包住完整路径。

## AR 基础理解
> 备选版不做真机测试，这里把三道基础题用大白话说一下，方便理解方案背后的原理。

**1. 如果把虚拟场景标注改成真实 AR，平面检测和点击放置通常需要哪些 AR Foundation 能力？**
真实 AR 里，手机要先"看懂"周围环境，才知道哪里有桌子、地板可以放东西。需要用到 AR Foundation 这几个模块：
- `ARSession` 和 `ARSessionOrigin`：管着整个 AR 会话和坐标系，是所有追踪的地基，没有它后面都跑不起来；
- `ARPlaneManager`：打开平面检测，让设备识别真实环境里的水平面（地板、桌面）和竖直面（墙面），识别到的平面能在画面上显示出来；
- `ARRaycastManager`：负责把手指点在屏幕上的位置，发射一条射线，命中刚才识别到的真实平面，返回命中点的位置和朝向。
点击放置的思路其实和现在很像：手指点屏幕 → 用 `ARRaycastManager` 对平面做射线检测 → 命中后在那个位置生成标记物体。只是现在的"地面/墙面"是虚拟的，真机上变成了真实识别到的平面。

**2. 为什么只保存 Unity 里的 x/y/z 坐标，通常不能保证应用下次启动后仍对应同一个真实位置？**
因为坐标是相对于 AR 会话原点的，而这个原点每次启动都是临时建立的——设备开机的位置、角度、初始化结果都不一样，原点也就跟着变。今天记录的是"以这次的手机为原点往左 1 米"，明天重启原点变了，同一张桌子在坐标里就变成别的位置了。真实平面识别本来就是相对的，所以不能拿裸坐标去"复现同一个真实位置"。要让标记跨启动稳定待在一个地方，得用持久化的空间锚点或世界地图这类机制（比如 ARKit 的 `ARWorldMap`、ARCore 的 Cloud Anchors），把标记真正钉在真实世界的某个锚点上，而不是记一个会漂的坐标。

**3. 当 AR 初始化失败、设备不支持或 Tracking 状态异常时，应用应该如何提示用户？**
这块对用户体验影响很大，处理不好用户会觉得"这 App 坏了"。大致分三种情况：
- 设备不支持：启动前先判断设备支不支持 AR（查 ARKit / ARCore 支持情况），不支持就直接提示"当前设备不支持 AR"，并把相关入口禁掉，别让用户点进来才发现用不了；
- 初始化失败：监听 `ARSession.state`，如果进了失败状态，弹提示"AR 初始化失败，请检查相机权限或重启应用"，再给个重试按钮；
- 追踪异常或丢失：监听追踪状态，一旦变成 `Limited` 或 `None`，提示用户"追踪丢失，请放慢移动、改善光线、对准有明显纹路的平面"。等恢复后要自动把提示清掉。关键是追踪不稳的时候不要让人继续放置或上报标记，免得存了错位的数据。
