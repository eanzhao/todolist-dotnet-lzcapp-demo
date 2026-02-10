# 待办清单 — .NET 版懒猫微服应用 Demo

基于 **ASP.NET Core 10.0 + Vue 3** 的待办清单应用，演示如何在懒猫微服平台上使用 .NET 技术栈开发、构建和部署 LPK 应用。

本项目是 [Python 版 todolist Demo](https://gitee.com/lazycatcloud/todolist-py-lzcapp-demo) 的 .NET 移植版，功能完全一致，方便两个版本对照学习。

## 目录结构

```
.
├── ui/                                  # 前端代码（Vue 3 + Tailwind CSS）
│   ├── src/
│   │   ├── App.vue                      # 主组件：待办清单 UI
│   │   ├── main.js                      # 应用入口
│   │   └── style.css                    # 全局样式（Tailwind）
│   ├── package.json                     # 前端依赖声明
│   └── vite.config.js                   # Vite 配置（含开发代理）
├── backend/                             # 后端代码
│   ├── src/
│   │   ├── TodoApi/                     # ASP.NET Core 项目
│   │   │    ├── TodoApi.csproj          # .NET 项目文件
│   │   │    ├── Program.cs              # 主程序：API 路由定义
│   │   │    ├── Models/Todo.cs          # 数据模型
│   │   │    └── Data/TodoDbContext.cs   # EF Core 数据库上下文
│   └── run.sh                           # LPK 容器内的启动脚本
├── build.sh                             # 项目构建脚本
├── lzc-build.yml                        # 懒猫应用构建配置
├── lzc-manifest.yml                     # 懒猫应用元信息配置
├── lzc-icon.png                         # 应用图标
└── README.md                            # 本文件
```

---

## 原理详解

### 1. 懒猫微服的 LPK 容器

LPK（Lazy PacKage）是懒猫微服的应用包格式。每个 LPK 应用运行在一个独立的容器中，基于 **Alpine Linux**（linux/amd64 架构）。

与 Docker 的主要区别：
- LPK 容器有统一的文件系统布局规范
- 内置了网络路由和安全隔离机制
- 只有 `/lzcapp/var/` 目录是持久化的，其他位置在容器重启后会被清除

### 2. 路由机制 — exec:// 协议

这是整个架构的核心。看 `lzc-manifest.yml` 中的路由配置：

```yaml
application:
  routes:
    - /=file:///lzcapp/pkg/content/web           # 静态文件路由
    - /api/=exec://3000,/lzcapp/pkg/content/backend/run.sh  # 后端服务路由
```

两种路由协议的工作方式：

| 协议 | 格式 | 作用 |
|------|------|------|
| `file://` | `file:///绝对路径` | 将请求映射到静态文件目录，类似 Nginx 的 `root` 指令 |
| `exec://` | `exec://端口号,脚本路径` | 执行指定脚本启动后端服务，将请求代理到该端口 |

**`exec://` 协议的运行流程：**

1. 用户首次访问 `/api/...` 路径
2. LPK 路由层执行 `/lzcapp/pkg/content/backend/run.sh` 脚本
3. 脚本启动 .NET 应用，监听 3000 端口
4. 路由层将 `/api/` 下的请求代理到 `127.0.0.1:3000`
5. URL 中的 `/api/` 前缀会被自动去掉

例如：用户访问 `/api/todos` → 后端收到的是 `/todos`

### 3. 为什么 .NET 可以在懒猫微服上运行？

懒猫微服的后端路由是 **语言无关** 的。只要满足两个条件：

1. 你的程序能在 Linux (Alpine/x86_64) 上运行
2. 你的程序能监听指定的 TCP 端口

那么任何语言、任何框架都可以使用。官方 Demo 用了 Python (FastAPI) 和 Go，.NET 也完全适用。

### 4. Self-Contained 发布策略

本项目采用 **自包含发布**（Self-Contained Deployment），这是 .NET 在容器环境中的推荐方式：

```bash
dotnet publish -c Release -r linux-musl-x64 --self-contained true \
    -p:PublishSingleFile=true
```

| 参数 | 含义 |
|------|------|
| `-r linux-musl-x64` | 目标运行时：Alpine Linux（musl libc），不是 `linux-x64`（glibc） |
| `--self-contained true` | 将 .NET 运行时打包进去，容器中不需要预装 .NET |
| `-p:PublishSingleFile=true` | 输出为单个可执行文件，部署简洁 |

> **为什么不用 `PublishTrimmed`？** EF Core 大量依赖运行时反射，IL Trimmer 会裁掉必要代码，导致数据库操作在运行时失败。构建时出现的 `IL2026` 警告就是这个意思。详见 [EF Core Trimming 文档](https://aka.ms/efcore-docs-trimming)。

**为什么选 `linux-musl-x64` 而不是 `linux-x64`？**

Alpine Linux 使用 musl libc 而非 glibc。如果用错 RID，程序启动时会报动态链接错误。这是 .NET 开发者在 Alpine 上最常见的问题。

### 5. 数据持久化

```
/lzcapp/pkg/content/  ← 你的应用文件（只读）
/lzcapp/var/          ← 持久化存储（唯一不会被清除的目录）
```

SQLite 数据库文件必须存放在 `/lzcapp/var/` 下。这就是 `run.sh` 中使用 `--db /lzcapp/var/todo.db` 的原因。

### 6. Python 版 vs .NET 版对比

| 维度 | Python 版 | .NET 版 |
|------|-----------|---------|
| 后端框架 | FastAPI + Uvicorn | ASP.NET Core Minimal API |
| ORM | SQLAlchemy | Entity Framework Core |
| 数据库 | SQLite | SQLite |
| 运行时依赖 | 需要在 run.sh 中 `apk add python3` | 自包含发布，无需安装运行时 |
| 启动脚本 | 安装依赖 + 启动 | 直接执行二进制文件 |
| 产物体积 | 小（Python 源码），但需要运行时 | 较大（~30MB），但零依赖 |
| 首次启动速度 | 慢（需要 apk + pip install） | 快（直接运行） |
| 前端 | Vue 3 + Tailwind CSS | Vue 3 + Tailwind CSS（一致） |

---

## 开发

### 环境要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 18+](https://nodejs.org/)
- [lzc-cli](https://developer.lazycat.cloud/dev-environment.html)（懒猫开发工具）

### 方式一：DevShell 远程开发

使用懒猫微服的 DevShell，在远程容器中开发：

```bash
# 终端 1：启动后端
lzc-cli project devshell
cd backend/TodoApi
dotnet run -- --db /lzcapp/var/todo.db

# 终端 2：启动前端
lzc-cli project devshell
cd ui
npm install
npm run dev
```

`lzc-build.yml` 中配置了 `image: mcr.microsoft.com/dotnet/sdk:10.0-alpine`，DevShell 容器中已预装 .NET SDK。

### 方式二：本地开发

```bash
# 终端 1：启动后端
cd backend/TodoApi
dotnet run -- --db ./todo.db
# 后端启动在 http://localhost:3000

# 终端 2：启动前端
cd ui
npm install
npm run dev
# 前端启动在 http://localhost:5173
# Vite 代理会将 /api 请求转发到后端 :3000
```

打开浏览器访问 `http://localhost:5173` 即可看到待办清单。

---

## 构建

```bash
# 1. 安装前端依赖（在本地执行）
cd ui
npm install
cd ..

# 2. 构建 LPK 包
lzc-cli project build -o release.lpk
```

构建过程会执行 `build.sh`，依次完成：
1. `dotnet publish` 编译 .NET 后端为自包含可执行文件
2. `vite build` 编译前端为静态文件
3. 将后端和前端产物复制到 `dist/` 目录
4. `lzc-cli` 将 `dist/` 打包为 `.lpk` 文件

构建产物结构：

```
dist/
├── backend/
│   ├── TodoApi       # .NET 自包含可执行文件（~30MB）
│   ├── run.sh        # 启动脚本
│   ├── todo.db       # （运行时生成，在 /lzcapp/var/ 下）
│   └── ...           # 其他运行时文件
└── web/
    ├── index.html    # 前端入口
    └── assets/       # JS/CSS 资源
```

---

## 安装

```bash
lzc-cli app install release.lpk
```

安装完成后，在懒猫微服启动器中点击应用图标即可运行。

> lpk 软件包除了可以通过 lzc-cli 命令安装，还可以上传到懒猫网盘双击安装。

---

## API 接口

| 方法 | 路径 | 说明 |
|------|------|------|
| `GET` | `/api/todos?skip=0&limit=10` | 获取待办列表 |
| `POST` | `/api/todos` | 创建待办 |
| `PUT` | `/api/todos/{id}` | 更新待办 |
| `DELETE` | `/api/todos/{id}` | 删除待办 |

请求/响应示例：

```json
// POST /api/todos
// 请求体
{ "todo": "学习懒猫微服开发", "isCompleted": false }

// 响应体
{ "id": 1, "todo": "学习懒猫微服开发", "isCompleted": false }
```

---

## 常见问题

### Q: 构建时报 `linux-musl-x64` 相关错误？

确保你安装的是 .NET 10.0 SDK，并且网络可以访问 NuGet 包源。如果在国内网络，可以配置镜像：

```bash
dotnet nuget add source https://nuget.cdn.azure.cn/v3/index.json -n azure-china
```

### Q: 应用启动后数据丢失？

检查 `run.sh` 中的数据库路径是否指向 `/lzcapp/var/`。只有这个目录的数据才会在容器重启后保留。

### Q: DevShell 中 `dotnet` 命令找不到？

确认 `lzc-build.yml` 中的 `image` 配置为 `mcr.microsoft.com/dotnet/sdk:10.0-alpine`，然后执行 `lzc-cli project devshell -f` 强制重建容器。

---

## 交流和帮助

你可以在 https://bbs.lazycat.cloud/ 畅所欲言。

参考资料：
- [懒猫微服开发者手册](https://developer.lazycat.cloud/)
- [Python 版 Demo 源码](https://gitee.com/lazycatcloud/todolist-py-lzcapp-demo)
- [ASP.NET Core 文档](https://learn.microsoft.com/aspnet/core/)
