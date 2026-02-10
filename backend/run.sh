#!/bin/sh

# ============================================
# .NET 后端启动脚本
# ============================================
# 此脚本由 LPK 的 exec:// 路由协议在应用启动时自动调用
#
# .NET 采用 self-contained 发布，运行时已打包在可执行文件中
# 但仍然依赖以下系统级原生库：
#   - libgcc    : GCC 运行时库
#   - libstdc++ : C++ 标准库（.NET CoreCLR 依赖）
# LPK 的精简 Alpine 容器默认不带这些库，需要手动安装
# （和 Python Demo 的 run.sh 中 apk add python3 同理）

# 切换到脚本所在目录
cd "$(dirname "$0")"

# 安装 .NET 运行时所需的系统原生依赖
sed -i 's/dl-cdn.alpinelinux.org/mirrors.ustc.edu.cn/g' /etc/apk/repositories
apk update
apk add --no-cache libgcc libstdc++

# 确保持久化目录存在
mkdir -p /lzcapp/var

# 启动 .NET 应用
# --db 参数指定 SQLite 数据库路径
# /lzcapp/var/ 是 LPK 容器中唯一的持久化目录
# 应用数据必须存放在此目录下，否则容器重启后数据会丢失
exec ./src/TodoApi --db /lzcapp/var/todo.db
