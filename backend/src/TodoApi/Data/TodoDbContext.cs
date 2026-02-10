using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

/// <summary>
/// EF Core 数据库上下文
/// 管理 TodoItem 实体与 SQLite 数据库的映射
/// </summary>
public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    /// <summary>待办事项表</summary>
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置表名和字段映射
        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.ToTable("Todos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Todo).IsRequired();
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);
        });
    }
}
