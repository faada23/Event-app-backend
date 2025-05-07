using System.Reflection;
using Microsoft.EntityFrameworkCore;

public class DatabaseContext : DbContext{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) {}

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!; 
    public DbSet<Image> Images { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<EventParticipant> EventParticipants { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

}
