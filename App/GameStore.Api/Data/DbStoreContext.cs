using GameStore.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Data;

public class DbStoreContext(DbContextOptions<DbStoreContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Task_m> Tasks => Set<Task_m>();
}
