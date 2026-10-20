using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tasks.Contracts;
using TasksWebApp.Data;

namespace TasksServer.Tests;

public sealed class TasksApiTests : IClassFixture<TasksApiFactory>
{
    private readonly HttpClient _client;

    public TasksApiTests(TasksApiFactory factory)
    {
        _client = factory.CreateClient();
        factory.InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetTasks_ReturnsOkAndEmptyListInitially()
    {
        var response = await _client.GetAsync("/api/tasks");

        response.EnsureSuccessStatusCode();

        var tasks = await response.Content
            .ReadFromJsonAsync<List<TaskItemDto>>();

        Assert.NotNull(tasks);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task PostTask_CreatesTask()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("  Integration test  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();

        Assert.NotNull(task);
        Assert.True(task.Id > 0);
        Assert.Equal("Integration test", task.Title);
        Assert.False(task.IsCompleted);
    }

    [Fact]
    public async Task PostTask_WithEmptyTitle_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutToggle_ChangesCompletionStatus()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("Toggle test"));

        var created = await createResponse.Content
            .ReadFromJsonAsync<TaskItemDto>();

        Assert.NotNull(created);

        var response = await _client.PutAsync(
            $"/api/tasks/{created.Id}/toggle",
            null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ToggleTaskResponse>();

        Assert.NotNull(result);
        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async Task PutToggle_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.PutAsync("/api/tasks/999/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_DeletesExistingTask()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks",
            new CreateTaskRequest("Delete test"));

        var created = await createResponse.Content
            .ReadFromJsonAsync<TaskItemDto>();

        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync("/api/tasks");
        var tasks = await getResponse.Content
            .ReadFromJsonAsync<List<TaskItemDto>>();

        Assert.NotNull(tasks);
        Assert.DoesNotContain(tasks, task => task.Id == created.Id);
    }

    [Fact]
    public async Task DeleteTask_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/tasks/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class TasksApiFactory : WebApplicationFactory<Program>
{
    public async Task InitializeDatabaseAsync()
{
    using var scope = Services.CreateScope();

    var factory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<TasksWebApp.Data.TasksDbContext>>();

    await using var db = await factory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextFactory<TasksDbContext>>();

            services.AddDbContextFactory<TasksDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }

        base.Dispose(disposing);
    }
}