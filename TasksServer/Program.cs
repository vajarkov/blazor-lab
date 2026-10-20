using Microsoft.EntityFrameworkCore;
using TasksServer.Data;
using TasksServer.Components;
using TasksServer.Services;
using Tasks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("TasksWasm", policy =>
    {
        policy
            .WithOrigins(
    "http://localhost:5057",
    "http://localhost:5055")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<TasksDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("TasksDb")
        ?? "Data Source=tasks.db"));

builder.Services.AddScoped<TaskService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("TasksWasm");

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapGet("/api/tasks", async (TaskService taskService) =>
{
    var tasks = await taskService.GetAllAsync();

    var dtos = tasks.Select(task => new TaskItemDto(
        task.Id,
        task.Title,
        task.IsCompleted));

    return Results.Ok(dtos);
});

app.MapPost("/api/tasks", async (
    CreateTaskRequest request,
    TaskService taskService) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Название задачи обязательно.");
    }

    var task = await taskService.AddAsync(request.Title);

    var dto = new TaskItemDto(
        task.Id,
        task.Title,
        task.IsCompleted);

    return Results.Created($"/api/tasks/{task.Id}", dto);
});

app.MapPut("/api/tasks/{id:int}/toggle", async (
    int id,
    TaskService taskService) =>
{
    var isCompleted = await taskService.ToggleAsync(id);

    if (isCompleted is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new ToggleTaskResponse(isCompleted.Value));
});

app.MapDelete("/api/tasks/{id:int}", async (
    int id,
    TaskService taskService) =>
{
    await taskService.DeleteAsync(id);
    return Results.NoContent();
});
app.Run();
