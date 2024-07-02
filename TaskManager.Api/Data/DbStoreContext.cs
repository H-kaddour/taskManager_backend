using TaskManager.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Api.Data;

public class DbStoreContext(DbContextOptions<DbStoreContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Task_m> Tasks => Set<Task_m>();
}
