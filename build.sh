#!/bin/sh
set -e

rm -rf ./dist
mkdir -p dist/backend
mkdir -p dist/web

# ============================================
# 1. 构建 .NET 后端
# ============================================
# 使用 self-contained 发布，将 .NET 运行时一起打包
# RID 使用 linux-musl-x64，因为懒猫微服容器基于 Alpine Linux (musl libc)
# PublishSingleFile 将所有依赖打包为单个可执行文件
#
# 注意：不要使用 PublishTrimmed=true！
# EF Core 大量依赖运行时反射，trimmer 会裁掉必要代码导致数据库操作失败
# 参见 https://aka.ms/efcore-docs-trimming
cd backend/TodoApi
dotnet publish -c Release \
    -r linux-musl-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o ../../dist/backend/

cd ../..

# 复制后端启动脚本
cp backend/run.sh dist/backend/run.sh
chmod +x dist/backend/run.sh
chmod +x dist/backend/TodoApi

# ============================================
# 2. 构建前端
# ============================================
cd ui && npx vite build --emptyOutDir --outDir ../dist/web
