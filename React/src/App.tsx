import { useEffect, useMemo, useState } from 'react'
import { deleteIssue, getIssues, updateStatus, type Issue, type Priority, type Status } from './api'
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

const PAGE_SIZE = 30

type LoadState = 'loading' | 'error' | 'success'

function formatTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return date.toLocaleString('zh-CN', { hour12: false })
}

function formatPosition(issue: Issue): string {
  const { x, y, z } = issue.position
  return `(${x.toFixed(2)}, ${y.toFixed(2)}, ${z.toFixed(2)})`
}

function App() {
  const [issues, setIssues] = useState<Issue[]>([])
  const [state, setState] = useState<LoadState>('loading')
  const [errorMessage, setErrorMessage] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [actionError, setActionError] = useState('')

  const [priorityFilter, setPriorityFilter] = useState('')
  const [areaFilter, setAreaFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)
  const [detail, setDetail] = useState<Issue | null>(null)

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

  // 区域筛选项取数据中出现的全部细分区域（设备区 / 墙面区 / 空间区），保持与上报值同步
  const areaOptions = useMemo(() => {
    const set = new Set<string>()
    issues.forEach((issue) => {
      if (issue.area) set.add(issue.area)
    })
    return Array.from(set).sort((a, b) => a.localeCompare(b, 'zh-CN'))
  }, [issues])

  const filtered = useMemo(
    () =>
      issues.filter(
        (issue) =>
          (priorityFilter === '' || issue.priority === priorityFilter) &&
          (areaFilter === '' || issue.area === areaFilter) &&
          (statusFilter === '' || issue.status === statusFilter),
      ),
    [issues, priorityFilter, areaFilter, statusFilter],
  )

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE))
  const currentPage = Math.min(page, totalPages)
  const paged = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE)

  function resetPage() {
    setPage(1)
  }

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

  function handlePageJump(raw: string) {
    const value = Number.parseInt(raw, 10)
    if (Number.isNaN(value)) return
    setPage(Math.min(Math.max(1, value), totalPages))
  }

  function handleDelete(issue: Issue) {
    if (!window.confirm(`确认删除问题「${issue.title}」？删除后不可恢复。`)) return
    setUpdatingId(issue.id)
    setActionError('')

    deleteIssue(issue.id)
      .then(() => getIssues())
      .then((data) => {
        setIssues(data)
        setDetail((current) => (current && current.id === issue.id ? null : current))
      })
      .catch((error: unknown) => {
        setActionError(error instanceof Error ? error.message : String(error))
      })
      .finally(() => setUpdatingId(null))
  }

  return (
    <main className="page">
      <header className="page-header">
        <h1>巡检问题列表</h1>
        {state === 'success' && (
          <span className="count">
            共 {filtered.length} 条{filtered.length !== issues.length && `（筛选自 ${issues.length} 条）`}
          </span>
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

      {state === 'success' && (
        <div className="filters">
          <label>
            优先级
            <select
              value={priorityFilter}
              onChange={(e) => {
                setPriorityFilter(e.target.value)
                resetPage()
              }}
            >
              <option value="">全部</option>
              {(Object.keys(priorityText) as Priority[]).map((value) => (
                <option key={value} value={value}>
                  {priorityText[value]}（{value}）
                </option>
              ))}
            </select>
          </label>
          <label>
            区域
            <select
              value={areaFilter}
              onChange={(e) => {
                setAreaFilter(e.target.value)
                resetPage()
              }}
            >
              <option value="">全部区域</option>
              {areaOptions.map((value) => (
                <option key={value} value={value}>
                  {value}
                </option>
              ))}
            </select>
          </label>
          <label>
            状态
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value)
                resetPage()
              }}
            >
              <option value="">全部</option>
              {(Object.keys(statusText) as Status[]).map((value) => (
                <option key={value} value={value}>
                  {statusText[value]}（{value}）
                </option>
              ))}
            </select>
          </label>
        </div>
      )}

      {state === 'success' && filtered.length === 0 && (
        <p className="tip">{issues.length === 0 ? '暂无巡检问题' : '没有符合筛选条件的工单'}</p>
      )}

      {state === 'success' && actionError && (
        <div className="action-error" role="alert">
          状态修改失败：{actionError}
        </div>
      )}

      {state === 'success' && paged.length > 0 && (
        <>
          <table className="issue-table">
            <thead>
              <tr>
                <th>标题</th>
                <th>描述</th>
                <th>设备</th>
                <th>区域</th>
                <th>优先级</th>
                <th>状态</th>
                <th>上报时间</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {paged.map((issue) => (
                <tr
                  key={issue.id}
                  className="issue-row"
                  onClick={() => setDetail(issue)}
                  title="点击查看完整详情"
                >
                  <td>{issue.title}</td>
                  <td className="desc">{issue.description ? issue.description : '—'}</td>
                  <td>{issue.deviceId ? issue.deviceId : '—'}</td>
                  <td>{issue.area ? issue.area : '—'}</td>
                  <td>
                    <span className={`badge priority-${issue.priority}`}>
                      {priorityText[issue.priority] ?? issue.priority}
                    </span>
                  </td>
                  <td onClick={(e) => e.stopPropagation()}>
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
                  <td onClick={(e) => e.stopPropagation()}>
                    <button
                      type="button"
                      className="delete-btn"
                      disabled={updatingId === issue.id}
                      onClick={() => handleDelete(issue)}
                    >
                      删除
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <nav className="pagination" aria-label="分页">
            <button
              type="button"
              disabled={currentPage <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              上一页
            </button>
            <span className="page-info">
              第 {currentPage} / {totalPages} 页（每页 {PAGE_SIZE} 条）
            </span>
            <button
              type="button"
              disabled={currentPage >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              下一页
            </button>
            <label className="page-jump">
              跳至
              <input
                type="number"
                min={1}
                max={totalPages}
                defaultValue={currentPage}
                key={currentPage}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handlePageJump(e.currentTarget.value)
                }}
                onBlur={(e) => handlePageJump(e.target.value)}
              />
              页
            </label>
          </nav>
        </>
      )}

      {detail && (
        <div className="detail-overlay" role="dialog" onClick={() => setDetail(null)}>
          <div className="detail-panel" onClick={(e) => e.stopPropagation()}>
            <h2>工单详情</h2>
            <dl className="detail-grid">
              <dt>标题</dt>
              <dd>{detail.title}</dd>
              <dt>描述</dt>
              <dd>{detail.description || '（无）'}</dd>
              <dt>优先级</dt>
              <dd>
                {priorityText[detail.priority] ?? detail.priority}（{detail.priority}）
              </dd>
              <dt>状态</dt>
              <dd>
                {statusText[detail.status] ?? detail.status}（{detail.status}）
              </dd>
              <dt>坐标</dt>
              <dd>{formatPosition(detail)}</dd>
              <dt>设备编号</dt>
              <dd>{detail.deviceId || '（无）'}</dd>
              <dt>所属区域</dt>
              <dd>{detail.area || '（无）'}</dd>
              <dt>上报时间</dt>
              <dd>{formatTime(detail.createdAt)}</dd>
              <dt>更新时间</dt>
              <dd>{formatTime(detail.updatedAt)}</dd>
            </dl>
            <button type="button" className="detail-close" onClick={() => setDetail(null)}>
              关闭
            </button>
          </div>
        </div>
      )}
    </main>
  )
}

export default App
