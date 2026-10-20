using Microsoft.EntityFrameworkCore;
using TasksServer.Data;

namespace TasksServer.Services;

public class TaskService(IDbContextFactory<TasksDbContext> dbFactory)
{
    public async Task<List<TaskItem>> GetAllAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        return await db.Tasks
            .AsNoTracking()
            .OrderBy(task => task.Id)
            .ToListAsync();
    }

    public async Task<TaskItem> AddAsync(string title)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var task = new TaskItem
        {
            Title = title.Trim()
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return task;
    }

    public async Task<bool?> ToggleAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var task = await db.Tasks.FindAsync(id);

        if (task is null)
        {
            return null;
        }

        task.IsCompleted = !task.IsCompleted;
        await db.SaveChangesAsync();

        return task.IsCompleted;
    }
    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var task = await db.Tasks.FindAsync(id);

        if (task is null)
        {
            return;
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }
}