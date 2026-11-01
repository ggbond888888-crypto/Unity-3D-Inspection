# AI 使用记录（AI_LOG）
## P-001
- 目标：对照原题文档梳理整体开发步骤，确定业务流程，只处理必做功能
### Prompt 原文
> 根据现场巡检问题文档梳理开发步骤 https://xx2pxezy6js.feishu.cn/wiki/MYH7wYyRzidsHSkm3K4cfwmQnJh 
 业务流程：Unity 进入虚拟现场 
   → 点击设备或场景表面放置问题标记 
   → 填写并上报问题 
   → Go 后端保存 
   → React 页面查看问题并修改状态 
 仅实现必做功能，排除所有加分项。 
### 结果
- 采用
- 验证：AI 读取了文档全文，输出了按 Go 后端 → React 管理端 → Unity 客户端 顺序组织的开发步骤；输出内容与原题必做项一一对应，按照顺序进行开发。

## P-002
- 目标：初始化 Go 项目，定义 Issue 数据模型与 JSON 文件持久化层。
### Prompt 原文
> 用 Go 初始化巡检问题后端项目（go mod init，用 Gin 框架）。要求：
> 1. 定义 Issue 结构体，字段与三端约定一致：id、title、description、priority（low/medium/high）、status（open/in_progress/resolved）、position{x,y,z}、createdAt、updatedAt；
> 2. 用本地 JSON 文件做持久化，提供 Load/Save 函数，服务启动时加载、每次变更后落盘，保证重启后数据不丢。
### 结果
- 采用
- 验证方式：核对 model.go 文件对比文档，数据结构字段名称对比一致。  
在 go 目录执行：`go build ./...`、`go vet ./...`，无报错无警告。  
- `go run .` 启动后 8080 端口正常监听  
- Load/Save 持久化逻辑的实际读写、重启数据，之后通过 Postman 进行测试

## P-003
- 目标：实现四个 API 接口及参数校验
### Prompt 原文
> 在已有 Go 项目上实现以下接口：
> 1. GET /health — 返回服务运行状态；
> 2. POST /issues — 接收 Unity 上报的问题：后端生成唯一 ID、默认 status=open、写入 createdAt/updatedAt；title 为空或 priority 不是 low/medium/high 时返回 400 和明确错误信息；
> 3. GET /issues — 返回全部问题列表；
> 4. PATCH /issues/:id/status — 修改状态，只允许 open/in_progress/resolved，非法值返回 400，更新 updatedAt；
> 5. 配置 CORS 允许浏览器跨域访问。
### 结果
- 采用
- 验证方式：首先用 go run . 命令看是否能跑通 Go 后端，成功跑通。  
- 使用 Postman 调用全部接口；  
1. GET /health：无请求体，发送，校验服务连通。
2. GET /issues：无请求体，发送，查看工单列表。
3. POST /issues：填入工单 json，发送，拿到返回的工单 ID。
4. PATCH /issues/{id}/status：把 url 里`{id}`替换成上一步得到的 id，传入 status 参数，发送修改状态。
5. 再次 GET /issues，核对状态变更。
6. 持久化验证：终端停止 go 程序，重新启动，再次 GET /issues，工单数据不丢失，证明持久化存储正常。  
- 非法参数请求返回 400 错误；正常请求返回符合约定的 JSON 结构；  
- 浏览器直接访问接口无跨域报错（http://127.0.0.1:8080/health）。

## P-004
- 目标：验证后端 CORS 跨域配置，确认跨域中间件代码行为符合预期。
### Prompt 原文
> 修改前方案：「临时静态页 + 真实浏览器」页面测试。
> 修改后方案：现在设计的 CORS 验证方案只使用浏览器页面测试。核查后发现该方式无法读取 CORS 响应头，不能充分验证后端 CORS 中间件代码。因此放弃单一浏览器测试方案，增加 OPTIONS 直连请求校验接口返回头。
### 结果
- 修改前验证方式：临时静态 HTML 页面，浏览器手动测试，人工观察请求。
- 修改后验证方式：`scripts/cors-check.ps1` 自动化脚本，覆盖 OPTIONS 预检、普通 GET、非法 POST/PATCH，断言状态码、Access-Control 系列响应头，脚本退出码标识结果，不写入数据。
- 结论：全部 16 项断言通过，后端 CORS 配置校验合格；浏览器页面仅用于演示。

## 第二阶段：React 管理端
## P-005
- 目标：初始化 React + TypeScript 项目并封装后端 API。
### Prompt 原文
> 用 Vite 创建 React + TypeScript 项目。封装一个 api 模块：baseURL 指向 Go 后端（http://localhost:8080），提供 getIssues() 和 updateStatus(id, status) 两个函数，Issue 类型定义与后端 JSON 字段一致（id/title/description/priority/status/position/createdAt/updatedAt）。不要加路由、状态库等多余依赖。
### 结果
- 采用
- 验证方式：npm run dev 报错，原因是 Windows PowerShell 默认执行策略限制不让运行 `npm.ps1` 脚本，改用 cmd 或 Windows PowerShell 管理员模式运行后启动正常；  
- npm install：依赖安装成功
- npm run dev：开发服务 http://localhost:5173 可正常访问。
- npm run build：打包成功，生成 dist 目录。
- npm run preview：静态产物预览服务 http://localhost:4173 启动正常，页面渲染正常，控制台无报错。
- ts 类型检查无报错；可以正常导入 api 模块。  
- 复制链接浏览器打开，页面正常渲染。

## P-006
- 目标：实现问题列表页，含后端不可用时的错误提示
### Prompt 原文
> 实现问题列表页面：加载时调用 GET /issues，每条记录至少显示标题、优先级、状态（时间可选）；请求失败（后端不可用）时页面显示"后端不可用，请检查 Go 服务"的错误提示并提供重试按钮，不能白屏；加载中有 loading 状态。
### 结果
- 采用
- 验证方式：分别启动、关闭 Go 服务进行测试；  
Go 正常、改 issues.json，刷新前端能拿到新数据，展示标题 / 优先级 / 状态
Go 服务正常、返回空数组：展示「暂无巡检问题」，不报错、不触发错误页
Go 关闭，页面不白屏，展示指定错误文案 + 重试按钮
点击重试按钮：点击出现 loading；启动 Go 服务后重试，成功拉取数据；Go 仍关闭时重试，回到错误提示
loading 状态：请求期间展示 loading，请求结束自动消失，不会卡死

## P-007
- 目标：实现状态修改功能并联调
### Prompt 原文
> 在列表每行加状态修改控件（下拉框或三个按钮：open / in_progress / resolved），点击后调用 PATCH /issues/:id/status，成功后刷新列表，失败时提示错误。帮我联调验证：Go 启动时改状态生效；Go 停掉时页面报错不白屏。
### 结果
- 采用
- 验证方式：Go 服务正常，由未处理改成处理中：PATCH 请求发出，后端 issues.json 状态持久更新，列表自动刷新展示新状态。  
Go 服务关闭时修改状态：列表数据保留，顶部弹出状态修改失败提示，页面不跳转全局错误页、不白屏，同时页面刷新时展示指定错误文案
原有列表页面功能（loading、重试、空列表、后端离线提示）不受新增功能影响

## 第三阶段：Unity 客户端
## P‑008
- 目标：搭建虚拟巡检场景，实现场景视角漫游控制
### Prompt 原文
> 搭建Unity虚拟巡检现场，使用Plane、Cube基础几何体构建地面、墙面，至少制作2个可交互点击的机柜设备对象；实现编辑器Play模式可用的视角控制：WASD按键移动相机，鼠标右键拖拽旋转视角 目录为E:\Game Coding\Unity\Unity。
### 结果
- 采用
- 验证方式：
1. 进入Play模式，可以使用WASD在场景内自由移动；按住鼠标右键拖动可以转动视角，能够完整浏览地面、墙面、两台机柜设备。
2. Hierarchy内不存在AR Session、XR Origin、ARPlaneManager、ARRaycastManager等AR相关组件与物体。

## P‑009
- 目标：实现鼠标射线检测、标记放置，完成防误触、单待提交标记、取消销毁未保存标记逻辑
### Prompt 原文
> 实现鼠标左键射线放置标记逻辑：使用Camera.ScreenPointToRay配合Physics.Raycast，在物理碰撞命中点hit.point生成球体标记；增加UI防误触判断，使用EventSystem.current.IsPointerOverGameObject()，点击UI界面时不生成标记；增加单标记约束：全局只允许存在一个待提交标记，再次点击场景时先销毁旧待提交标记；表单点击取消按钮时，移除当前未上报保存的标记。
### 结果
- 待实施
- 验证方式：
1. 点击场景地面、机柜表面，在点击命中位置生成Sphere标记，标记坐标与碰撞点位置一致。
2. 点击UI表单输入框、按钮区域，不会生成任何场景标记。
3. 不提交表单直接再次点击场景，旧的待提交标记自动销毁，只保留新生成标记。
4. 生成标记后打开表单，点击取消按钮，待提交标记直接从场景移除，不向后端发送请求。

## P‑010
- 目标：集成上报表单，完成网络上报逻辑，实现完整客户端上报闭环
### Prompt 原文
> 复用现有UI表单，表单字段：标题必填、描述选填，优先级下拉选项low/medium/high；自动读取待提交标记的x/y/z空间坐标作为上报position数据；使用UnityWebRequest向Go后端POST JSON工单数据；后端地址做成Inspector可配置字段；上报成功、上报失败分别弹出明确提示弹窗；上报成功后清空当前待提交标记。
### 结果
- 待实施
- 验证方式：
1. 不填写标题直接提交，请求被后端拦截，客户端展示参数错误提示。
2. 完整填写表单提交，发送符合三端约定JSON结构，Go后端接收存储；刷新React页面可以看到新增巡检工单。
3. 关闭Go后端服务，执行上报操作，客户端展示网络请求失败提示，程序不崩溃。
4. Inspector修改后端地址，可切换目标服务地址；上报成功后场景内待提交标记自动销毁。

## P‑011
- 目标：Unity端整体联调，打通三端完整业务闭环
### Prompt 原文
> 完成Unity客户端全部必做功能后，与Go后端、React管理端做完整联调。完整流程：Unity场景移动浏览 → 点击放置标记 → 填写表单提交工单 → React页面查看新增工单 → React修改工单状态 → 重启Go后端，验证数据持久化。同时核对全部交付物要求：完善README.md（版本信息、启动步骤、已知问题、接口说明、三道AR基础问答题）、整理AI_LOG记录、准备演示视频素材，确认项目导出规则，Unity不包含Library、Temp、Logs目录。
### 结果
- 待实施
- 验证方式：
1. Unity编辑器Play模式走完完整业务流程，React可以正常获取、修改工单状态。
2. Go服务重启，工单数据不丢失。
3. README.md包含版本、三端启动说明、接口路径与响应格式、三道AR基础理解简答题。
4. AI_LOG.md格式完整，保留Prompt原文，具备检查修改放弃的记录，无敏感密钥信息。
5. 确认项目资源：仅保留Assets、Packages、ProjectSettings，剔除自动生成缓存目录，满足打包交付要求。