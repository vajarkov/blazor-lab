using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TasksWebApp.Data;
using TasksWebApp.Services;

namespace TasksServer.Tests;

public sealed class TaskServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TasksDbContext> _options;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<TasksDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new TasksDbContext(_options);
        db.Database.EnsureCreated();

        _service = new TaskService(new TestDbContextFactory(_options));
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var tasks = await _service.GetAllAsync();

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTasksOrderedById()
    {
        int firstId;
        int secondId;

        await using (var db = new TasksDbContext(_options))
        {
            var first = new TaskItem { Title = "First" };
            var second = new TaskItem { Title = "Second" };

            db.Tasks.AddRange(first, second);
            await db.SaveChangesAsync();

            firstId = first.Id;
            secondId = second.Id;
        }

        var tasks = await _service.GetAllAsync();

        Assert.Equal(2, tasks.Count);
        Assert.Equal(firstId, tasks[0].Id);
        Assert.Equal(secondId, tasks[1].Id);
    }

    [Fact]
    public async Task AddAsync_TrimsTitleAndCreatesIncompleteTask()
    {
        var task = await _service.AddAsync("  Test task  ");

        Assert.True(task.Id > 0);
        Assert.Equal("Test task", task.Title);
        Assert.False(task.IsCompleted);
    }

    [Fact]
    public async Task ToggleAsync_ExistingTask_TogglesCompletion()
    {
        var task = await _service.AddAsync("Test task");

        var firstResult = await _service.ToggleAsync(task.Id);
        var secondResult = await _service.ToggleAsync(task.Id);

        Assert.True(firstResult);
        Assert.False(secondResult);
    }

    [Fact]
    public async Task ToggleAsync_MissingTask_ReturnsNull()
    {
        var result = await _service.ToggleAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_DeletesIt()
    {
        var task = await _service.AddAsync("Test task");

        var deleted = await _service.DeleteAsync(task.Id);
        var tasks = await _service.GetAllAsync();

        Assert.True(deleted);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task DeleteAsync_MissingTask_ReturnsFalse()
    {
        var deleted = await _service.DeleteAsync(999);

        Assert.False(deleted);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<TasksDbContext> options)
        : IDbContextFactory<TasksDbContext>
    {
        public TasksDbContext CreateDbContext() => new(options);

        public Task<TasksDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new TasksDbContext(options));
        }
    }
}
