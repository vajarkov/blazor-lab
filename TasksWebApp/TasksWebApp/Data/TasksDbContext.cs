
using Microsoft.EntityFrameworkCore;

namespace TasksWebApp.Data;

public class TasksDbContext(DbContextOptions<TasksDbContext> options)
    : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}
