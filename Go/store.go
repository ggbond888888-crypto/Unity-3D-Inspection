package main

import (
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"errors"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// Store 用本地 JSON 文件持久化问题列表，所有读写都在锁内进行。
type Store struct {
	mu     sync.RWMutex
	path   string
	issues []Issue
}

// NewStore 创建 Store，path 为 JSON 数据文件路径。
func NewStore(path string) *Store {
	return &Store{
		path:   path,
		issues: []Issue{},
	}
}

// Load 从 JSON 文件读取数据；文件不存在时视为空列表。
func (s *Store) Load() error {
	s.mu.Lock()
	defer s.mu.Unlock()

	data, err := os.ReadFile(s.path)
	if errors.Is(err, os.ErrNotExist) {
		s.issues = []Issue{}
		return nil
	}
	if err != nil {
		return err
	}

	if len(data) == 0 {
		s.issues = []Issue{}
		return nil
	}

	var issues []Issue
	if err := json.Unmarshal(data, &issues); err != nil {
		return err
	}
	if issues == nil {
		issues = []Issue{}
	}
	s.issues = issues
	return nil
}

// Save 把内存中的问题列表写入 JSON 文件。
// 先写临时文件再重命名，避免写入中断导致文件损坏。
func (s *Store) Save() error {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.saveLocked()
}

func (s *Store) saveLocked() error {
	data, err := json.MarshalIndent(s.issues, "", "  ")
	if err != nil {
		return err
	}

	dir := filepath.Dir(s.path)
	if dir != "." && dir != "" {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			return err
		}
	}

	tmp := s.path + ".tmp"
	if err := os.WriteFile(tmp, data, 0o644); err != nil {
		return err
	}
	return os.Rename(tmp, s.path)
}

// List 返回当前问题列表的副本。
func (s *Store) List() []Issue {
	s.mu.RLock()
	defer s.mu.RUnlock()

	out := make([]Issue, len(s.issues))
	copy(out, s.issues)
	return out
}

// Create 生成唯一 ID 与时间戳后保存新问题，并立即落盘。
func (s *Store) Create(issue Issue) (Issue, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	now := time.Now().Format(time.RFC3339Nano)
	issue.ID = newID()
	issue.Status = StatusOpen
	issue.CreatedAt = now
	issue.UpdatedAt = now

	s.issues = append(s.issues, issue)
	if err := s.saveLocked(); err != nil {
		// 落盘失败时回滚内存状态，保证内存与文件一致。
		s.issues = s.issues[:len(s.issues)-1]
		return Issue{}, err
	}
	return issue, nil
}

// UpdateStatus 按 ID 修改状态，并立即落盘。
func (s *Store) UpdateStatus(id, status string) (Issue, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	for i := range s.issues {
		if s.issues[i].ID != id {
			continue
		}
		oldStatus, oldUpdatedAt := s.issues[i].Status, s.issues[i].UpdatedAt
		s.issues[i].Status = status
		s.issues[i].UpdatedAt = time.Now().Format(time.RFC3339Nano)
		if err := s.saveLocked(); err != nil {
			s.issues[i].Status, s.issues[i].UpdatedAt = oldStatus, oldUpdatedAt
			return Issue{}, err
		}
		return s.issues[i], nil
	}
	return Issue{}, os.ErrNotExist
}

// Delete 按 ID 删除问题，并立即落盘；不存在时返回 os.ErrNotExist。
func (s *Store) Delete(id string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	for i := range s.issues {
		if s.issues[i].ID != id {
			continue
		}
		removed := s.issues[i]
		s.issues = append(s.issues[:i], s.issues[i+1:]...)
		if err := s.saveLocked(); err != nil {
			// 落盘失败时回滚内存状态，保证内存与文件一致。
			s.issues = append(s.issues[:i], append([]Issue{removed}, s.issues[i:]...)...)
			return err
		}
		return nil
	}
	return os.ErrNotExist
}

// newID 生成 32 位十六进制随机 ID。
func newID() string {
	b := make([]byte, 16)
	if _, err := rand.Read(b); err != nil {
		return time.Now().Format("20060102150405.000000000")
	}
	return hex.EncodeToString(b)
}
