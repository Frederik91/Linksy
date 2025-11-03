using Linksy.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Linksy.Api.Data;

/// <summary>
/// Application database context for Linksy
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Connector> Connectors => Set<Connector>();
    public DbSet<Binding> Bindings => Set<Binding>();
    public DbSet<SyncJob> SyncJobs => Set<SyncJob>();
    public DbSet<ChangeItem> ChangeItems => Set<ChangeItem>();
    public DbSet<CredentialSecret> CredentialSecrets => Set<CredentialSecret>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // Connector configuration
        modelBuilder.Entity<Connector>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Type });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Configuration).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Connectors)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CredentialSecret)
                .WithOne(c => c.Connector)
                .HasForeignKey<CredentialSecret>(c => c.ConnectorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Binding configuration
        modelBuilder.Entity<Binding>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => new { e.IsActive, e.IsPaused });
            entity.Property(e => e.SourcePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.TargetPath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Bindings)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceConnector)
                .WithMany(c => c.SourceBindings)
                .HasForeignKey(e => e.SourceConnectorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetConnector)
                .WithMany(c => c.TargetBindings)
                .HasForeignKey(e => e.TargetConnectorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SyncJob configuration
        modelBuilder.Entity<SyncJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BindingId);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Binding)
                .WithMany(b => b.SyncJobs)
                .HasForeignKey(e => e.BindingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChangeItem configuration
        modelBuilder.Entity<ChangeItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SyncJobId);
            entity.HasIndex(e => new { e.Status, e.IsConflict });
            entity.Property(e => e.SourcePath).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.TargetPath).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.SyncJob)
                .WithMany(j => j.ChangeItems)
                .HasForeignKey(e => e.SyncJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CredentialSecret configuration
        modelBuilder.Entity<CredentialSecret>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConnectorId).IsUnique();
            entity.Property(e => e.EncryptedCredential).IsRequired();
            entity.Property(e => e.KeyIdentifier).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // AuditEntry configuration
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Timestamp });
            entity.HasIndex(e => e.ActionType);
            entity.Property(e => e.Actor).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Context).IsRequired();
            entity.Property(e => e.Outcome).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.AuditEntries)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
