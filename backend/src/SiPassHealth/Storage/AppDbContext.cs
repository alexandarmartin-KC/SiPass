using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SiPassHealth.Security;

namespace SiPassHealth.Storage;

public sealed class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ObjectEntity> Objects => Set<ObjectEntity>();
    public DbSet<StatusSnapshot> StatusSnapshots => Set<StatusSnapshot>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ObjectEntity>()
            .HasKey(x => x.ObjectId);

        builder.Entity<StatusSnapshot>()
            .HasKey(x => x.ObjectId);

        builder.Entity<StatusSnapshot>()
            .HasIndex(x => x.ObjectId);

        builder.Entity<EventLog>()
            .HasIndex(x => x.Timestamp);
    }
}
