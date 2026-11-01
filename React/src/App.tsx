import { useEffect, useState } from 'react'
import { getIssues, updateStatus, type Issue, type Priority, type Status } from './api'
import './App.css'

const priorityText: Record<Priority, string> = {
  low: '低',
  medium: '中',
  high: '高',
}

const statusText: Record<Status, string> = {
  open: '待处理',
  in_progress: '处理中',
  resolved: '已解决',
}

type LoadState = 'loading' | 'error' | 'success'

function formatTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('zh-CN', { hour12: false })
}

function App() {
  const [issues, setIssues] = useState<Issue[]>([])
  const [state, setState] = useState<LoadState>('loading')
  const [errorMessage, setErrorMessage] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [actionError, setActionError] = useState('')

  useEffect(() => {
    let cancelled = false
    setState('loading')

    getIssues()
      .then((data) => {
        if (cancelled) return
        setIssues(data)
        setState('success')
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setErrorMessage(error instanceof Error ? error.message : String(error))
        setState('error')
      })

    return () => {
      cancelled = true
    }
  }, [reloadKey])

  function handleStatusChange(id: string, nextStatus: Status) {
    setUpdatingId(id)
    setActionError('')

    updateStatus(id, nextStatus)
      .then(() => getIssues())
      .then((data) => setIssues(data))
      .catch((error: unknown) => {
        setActionError(error instanceof Error ? error.message : String(error))
      })
      .finally(() => setUpdatingId(null))
  }

  return (
    <main className="page">
      <header className="page-header">
        <h1>巡检问题列表</h1>
        {'success' === state && (
          <span className="count">共 {issues.length} 条</span>
        )}
      </header>

      {state === 'loading' && <p className="tip">正在加载问题列表…</p>}

      {state === 'error' && (
        <div className="error-box" role="alert">
          <strong>后端不可用，请检查 Go 服务</strong>
          <p className="error-detail">{errorMessage}</p>
          <button type="button" onClick={() => setReloadKey((key) => key + 1)}>
            重试
          </button>
        </div>
      )}

      {state === 'success' && issues.length === 0 && (
        <p className="tip">暂无巡检问题</p>
      )}

      {state === 'success' && actionError && (
        <div className="action-error" role="alert">
          状态修改失败：{actionError}
        </div>
      )}

      {state === 'success' && issues.length > 0 && (
        <table className="issue-table">
          <thead>
            <tr>
              <th>标题</th>
              <th>描述</th>
              <th>优先级</th>
              <th>状态</th>
              <th>上报时间</th>
            </tr>
          </thead>
          <tbody>
            {issues.map((issue) => (
              <tr key={issue.id}>
                <td>{issue.title}</td>
                <td className="desc">{issue.description ? issue.description : '—'}</td>
                <td>
                  <span className={`badge priority-${issue.priority}`}>
                    {priorityText[issue.priority] ?? issue.priority}
                  </span>
                </td>
                <td>
                  <select
                    className={`status-select status-${issue.status}`}
                    value={issue.status}
                    disabled={updatingId === issue.id}
                    aria-label={`修改「${issue.title}」的处理状态`}
                    onChange={(event) =>
                      handleStatusChange(issue.id, event.target.value as Status)
                    }
                  >
                    {(Object.keys(statusText) as Status[]).map((value) => (
                      <option key={value} value={value}>
                        {statusText[value]}
                      </option>
                    ))}
                  </select>
                  {updatingId === issue.id && <span className="saving">保存中…</span>}
                </td>
                <td className="time">{formatTime(issue.createdAt)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}

export default App
