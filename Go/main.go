package main

import (
	"log"
	"os"

	"github.com/gin-gonic/gin"
)

func main() {
	dataFile := os.Getenv("ISSUES_FILE")
	if dataFile == "" {
		dataFile = "issues.json"
	}

	store := NewStore(dataFile)
	if err := store.Load(); err != nil {
		log.Fatalf("加载数据文件失败: %v", err)
	}
	log.Printf("数据文件: %s，已加载问题数: %d", dataFile, len(store.List()))

	r := gin.Default()
	r.Use(corsMiddleware())
	registerRoutes(r, store)

	if err := r.Run(":8080"); err != nil {
		log.Fatalf("服务启动失败: %v", err)
	}
}
