package main

// Position 是 Unity 上报的标记坐标（三端字段名保持一致）。
type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

// Issue 是巡检问题，字段与 Unity / React 约定一致。
type Issue struct {
	ID          string   `json:"id"`
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    string   `json:"priority"`
	Status      string   `json:"status"`
	Position    Position `json:"position"`
	CreatedAt   string   `json:"createdAt"`
	UpdatedAt   string   `json:"updatedAt"`
}

// 优先级与状态的合法取值。
const (
	PriorityLow    = "low"
	PriorityMedium = "medium"
	PriorityHigh   = "high"

	StatusOpen       = "open"
	StatusInProgress = "in_progress"
	StatusResolved   = "resolved"
)

var validPriorities = map[string]bool{
	PriorityLow:    true,
	PriorityMedium: true,
	PriorityHigh:   true,
}

var validStatuses = map[string]bool{
	StatusOpen:       true,
	StatusInProgress: true,
	StatusResolved:   true,
}

// IsValidPriority 判断优先级是否为 low / medium / high。
func IsValidPriority(p string) bool {
	return validPriorities[p]
}

// IsValidStatus 判断状态是否为 open / in_progress / resolved。
func IsValidStatus(s string) bool {
	return validStatuses[s]
}
