using Deployment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Deployment.Infrastructure.Data;

public class DeploymentDbContext(DbContextOptions<DeploymentDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<AppEnvironment> Environments => Set<AppEnvironment>();
    public DbSet<DeploymentTarget> Targets => Set<DeploymentTarget>();
    public DbSet<Release> Releases => Set<Release>();
    public DbSet<ReleaseFile> ReleaseFiles => Set<ReleaseFile>();
    public DbSet<DeploymentRecord> Deployments => Set<DeploymentRecord>();
    public DbSet<DeploymentStep> DeploymentSteps => Set<DeploymentStep>();
    public DbSet<Backup> Backups => Set<Backup>();
    public DbSet<BackupFile> BackupFiles => Set<BackupFile>();
    public DbSet<RetentionPolicy> RetentionPolicies => Set<RetentionPolicy>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.MultipleCollectionIncludeWarning));
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Domain.Entities.Application>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.Environments).WithOne(x => x.Application).HasForeignKey(x => x.ApplicationId);
        });

        model.Entity<AppEnvironment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Targets).WithOne(x => x.Environment).HasForeignKey(x => x.EnvironmentId);
            e.HasOne(x => x.RetentionPolicy).WithOne(x => x.Environment).HasForeignKey<RetentionPolicy>(x => x.EnvironmentId);
        });

        model.Entity<DeploymentTarget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.OS).HasConversion<string>();
            e.HasMany(x => x.Deployments).WithOne(x => x.Target).HasForeignKey(x => x.TargetId);
            e.HasMany(x => x.Backups).WithOne(x => x.Target).HasForeignKey(x => x.TargetId);
        });

        model.Entity<Release>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ReleaseId).IsUnique();
            e.HasMany(x => x.Files).WithOne(x => x.Release).HasForeignKey(x => x.ReleaseId);
        });

        model.Entity<DeploymentRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DeploymentId).IsUnique();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.Steps).WithOne(x => x.Deployment).HasForeignKey(x => x.DeploymentId);
        });

        model.Entity<DeploymentStep>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasConversion<string>();
        });

        model.Entity<Backup>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.BackupId).IsUnique();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasMany(x => x.Files).WithOne(x => x.Backup).HasForeignKey(x => x.BackupId);
        });

        model.Entity<AuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.DeploymentId);
        });
    }
}
