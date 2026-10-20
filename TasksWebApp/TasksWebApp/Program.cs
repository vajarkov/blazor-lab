using Microsoft.EntityFrameworkCore;
using TasksWebApp.Data;
using TasksWebApp.Client.Pages;
using TasksWebApp.Components;
using TasksWebApp.Services;
using TasksWebApp.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDbContextFactory<TasksDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("TasksDb")
        ?? "Data Source=tasks-webapp.db"));
        
builder.Services.AddScoped<TaskService>();

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5127/")
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TasksWebApp.Client._Imports).Assembly);

app.MapGet("/api/tasks", async (TaskService service) =>
{
    var tasks = await service.GetAllAsync();
    return Results.Ok(tasks);
});

app.MapPost("/api/tasks", async (
    CreateTaskRequest request,
    TaskService service) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest("Название задачи обязательно.");

    var task = await service.AddAsync(request.Title);
    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPut("/api/tasks/{id:int}/toggle", async (
    int id,
    TaskService service) =>
{
    var isCompleted = await service.ToggleAsync(id);

    if (isCompleted is null)
        return Results.NotFound();

    return Results.Ok(new ToggleTaskResponse(isCompleted.Value));
});

app.MapDelete("/api/tasks/{id:int}", async (
    int id,
    TaskService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();
