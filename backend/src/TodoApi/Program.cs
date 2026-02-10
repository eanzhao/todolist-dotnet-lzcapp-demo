using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

// ============================================
// ASP.NET Core Minimal API —— 待办清单后端
// ============================================
// 对标 Python Demo 中的 FastAPI 后端
// Minimal API 风格与 FastAPI 类似：简洁、直观、不需要 Controller

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------
// 1. 数据库路径配置
// ------------------------------------------
// 通过命令行参数 --db 指定数据库路径，与 Python Demo 保持一致
// 在 LPK 容器中运行时，数据库文件应放在 /lzcapp/var/ 下以实现持久化
var dbPath = args.Length >= 2 && args[0] == "--db"
    ? args[1]
    : "./todos.db";

// ------------------------------------------
// 2. 注册服务
// ------------------------------------------
// 注册 EF Core SQLite 数据库上下文
builder.Services.AddDbContext<TodoDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// 注册 CORS（跨域资源共享）
// 开发时前端运行在 5173 端口，需要允许跨域请求
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// OpenAPI 支持（.NET 10 内置，替代 Swashbuckle）
builder.Services.AddOpenApi();

var app = builder.Build();

// ------------------------------------------
// 3. 自动创建数据库和表
// ------------------------------------------
// 类似 Python Demo 中 Base.metadata.create_all(bind=engine)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    db.Database.EnsureCreated();
}

// ------------------------------------------
// 4. 中间件管道
// ------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// ------------------------------------------
// 5. API 路由定义
// ------------------------------------------
// 使用 Minimal API 风格，与 FastAPI 的装饰器路由类似

var todosApi = app.MapGroup("/todos");

// GET /todos —— 获取所有待办事项
// 对应 Python: @app.get("/todos")
todosApi.MapGet("/", async (TodoDbContext db, int skip = 0, int limit = 10) =>
{
    var todos = await db.Todos
        .OrderBy(t => t.Id)
        .Skip(skip)
        .Take(limit)
        .ToListAsync();

    return Results.Ok(todos);
});

// POST /todos —— 创建待办事项
// 对应 Python: @app.post("/todos")
todosApi.MapPost("/", async (TodoRequest request, TodoDbContext db) =>
{
    var todo = new TodoItem
    {
        Todo = request.Todo,
        IsCompleted = request.IsCompleted
    };

    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Ok(todo);
});

// PUT /todos/{id} —— 更新待办事项
// 对应 Python: @app.put("/todos/{todo_id}")
todosApi.MapPut("/{id:int}", async (int id, TodoRequest request, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
        return Results.NotFound(new { detail = "Todo not found" });

    todo.Todo = request.Todo;
    todo.IsCompleted = request.IsCompleted;
    await db.SaveChangesAsync();

    return Results.Ok(todo);
});

// DELETE /todos/{id} —— 删除待办事项
// 对应 Python: @app.delete("/todos/{todo_id}")
todosApi.MapDelete("/{id:int}", async (int id, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null)
        return Results.NotFound(new { detail = "Todo not found" });

    db.Todos.Remove(todo);
    await db.SaveChangesAsync();

    return Results.Ok(todo);
});

// ------------------------------------------
// 6. 启动服务器
// ------------------------------------------
// 监听 0.0.0.0:3000，与 Python Demo 保持一致
// LPK 的 exec:// 路由会将 /api/ 前缀的请求代理到此端口
app.Run("http://0.0.0.0:3000");
