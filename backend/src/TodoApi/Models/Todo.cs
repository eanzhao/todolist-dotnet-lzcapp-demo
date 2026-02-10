namespace TodoApi.Models;

/// <summary>
/// 待办事项数据模型
/// 对应数据库中的 Todos 表
/// </summary>
public class TodoItem
{
    /// <summary>主键，自增 ID</summary>
    public int Id { get; set; }

    /// <summary>待办事项内容</summary>
    public string Todo { get; set; } = string.Empty;

    /// <summary>是否已完成</summary>
    public bool IsCompleted { get; set; } = false;
}

/// <summary>
/// 创建/更新待办事项的请求体
/// 与数据库模型分离，遵循 DTO 模式
/// </summary>
public class TodoRequest
{
    public string Todo { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
}
