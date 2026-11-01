// 与 Go 后端约定的接口封装，字段名与后端 JSON 保持一致。

export const BASE_URL = 'http://localhost:8080'

export type Priority = 'low' | 'medium' | 'high'
export type Status = 'open' | 'in_progress' | 'resolved'

export interface Position {
  x: number
  y: number
  z: number
}

export interface Issue {
  id: string
  title: string
  description: string
  priority: Priority
  status: Status
  position: Position
  deviceId: string
  area: string
  createdAt: string
  updatedAt: string
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response
  try {
    res = await fetch(`${BASE_URL}${path}`, init)
  } catch {
    throw new Error('无法连接后端服务，请检查 Go 服务是否已启动')
  }

  if (!res.ok) {
    let message = `请求失败（${res.status}）`
    try {
      const data = (await res.json()) as { error?: string }
      if (data.error) message = data.error
    } catch {
      // 响应体不是 JSON 时保留默认提示
    }
    throw new Error(message)
  }

  // 204 / 空响应体（如 DELETE）不解析 JSON
  const text = await res.text()
  return (text ? JSON.parse(text) : undefined) as T
}

export function getIssues(): Promise<Issue[]> {
  return request<Issue[]>('/issues')
}

export function updateStatus(id: string, status: Status): Promise<Issue> {
  return request<Issue>(`/issues/${encodeURIComponent(id)}/status`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status }),
  })
}

export async function deleteIssue(id: string): Promise<void> {
  await request<unknown>(`/issues/${encodeURIComponent(id)}`, { method: 'DELETE' })
}
