using Microsoft.EntityFrameworkCore;

namespace TasksServer.Data;

public class TasksDbContext(DbContextOptions<TasksDbContext> options)
    : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}