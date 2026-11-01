package main

import (
	"errors"
	"net/http"
	"os"
	"strings"
	"time"

	"github.com/gin-gonic/gin"
)

// createIssueRequest 是 Unity 上报问题时提交的字段。
// DeviceId / Area 可选：命中绑定设备编号的设备时才有 deviceId，area 随所有打点上报。
type createIssueRequest struct {
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    string   `json:"priority"`
	Position    Position `json:"position"`
	DeviceId    string   `json:"deviceId"`
	Area        string   `json:"area"`
}

// updateStatusRequest 是修改处理状态时提交的字段。
type updateStatusRequest struct {
	Status string `json:"status"`
}

// corsMiddleware 允许浏览器跨域访问接口。
func corsMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		c.Header("Access-Control-Allow-Origin", "*")
		c.Header("Access-Control-Allow-Methods", "GET, POST, PATCH, DELETE, OPTIONS")
		c.Header("Access-Control-Allow-Headers", "Content-Type")

		if c.Request.Method == http.MethodOptions {
			c.AbortWithStatus(http.StatusNoContent)
			return
		}
		c.Next()
	}
}

// registerRoutes 注册全部接口。
func registerRoutes(r *gin.Engine, store *Store) {
	r.GET("/health", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{
			"status": "ok",
			"time":   time.Now().Format(time.RFC3339),
		})
	})

	r.GET("/issues", func(c *gin.Context) {
		c.JSON(http.StatusOK, store.List())
	})

	r.POST("/issues", func(c *gin.Context) {
		var req createIssueRequest
		if err := c.ShouldBindJSON(&req); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": "请求体不是合法的 JSON"})
			return
		}

		req.Title = strings.TrimSpace(req.Title)
		if req.Title == "" {
			c.JSON(http.StatusBadRequest, gin.H{"error": "title 不能为空"})
			return
		}
		if !IsValidPriority(req.Priority) {
			c.JSON(http.StatusBadRequest, gin.H{"error": "priority 必须是 low、medium 或 high"})
			return
		}

		issue, err := store.Create(Issue{
			Title:       req.Title,
			Description: req.Description,
			Priority:    req.Priority,
			Position:    req.Position,
			DeviceId:    req.DeviceId,
			Area:        req.Area,
		})
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "保存问题失败: " + err.Error()})
			return
		}
		c.JSON(http.StatusCreated, issue)
	})

	r.PATCH("/issues/:id/status", func(c *gin.Context) {
		var req updateStatusRequest
		if err := c.ShouldBindJSON(&req); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": "请求体不是合法的 JSON"})
			return
		}
		if !IsValidStatus(req.Status) {
			c.JSON(http.StatusBadRequest, gin.H{"error": "status 必须是 open、in_progress 或 resolved"})
			return
		}

		issue, err := store.UpdateStatus(c.Param("id"), req.Status)
		if errors.Is(err, os.ErrNotExist) {
			c.JSON(http.StatusNotFound, gin.H{"error": "问题不存在"})
			return
		}
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "修改状态失败: " + err.Error()})
			return
		}
		c.JSON(http.StatusOK, issue)
	})

	r.DELETE("/issues/:id", func(c *gin.Context) {
		err := store.Delete(c.Param("id"))
		if errors.Is(err, os.ErrNotExist) {
			c.JSON(http.StatusNotFound, gin.H{"error": "问题不存在"})
			return
		}
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "删除问题失败: " + err.Error()})
			return
		}
		c.Status(http.StatusNoContent)
	})
}
