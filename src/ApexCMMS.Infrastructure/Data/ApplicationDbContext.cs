using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ApexCMMS.Application.Interfaces;
using ApexCMMS.Domain.Common;
using ApexCMMS.Domain.Entities;

namespace ApexCMMS.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems => Set<WorkOrderChecklistItem>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<WorkOrderPartAllocation> WorkOrderPartAllocations => Set<WorkOrderPartAllocation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map to Projects schema matching D:\Anubhaw\Projects\DB\Enterprise CMMS DB.sql
        builder.HasDefaultSchema("Projects");

        // Identity Table Names
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("AspNetUsers");
            b.HasIndex(u => u.EmployeeCode).IsUnique();
            b.Property(u => u.EmployeeCode).HasMaxLength(20).IsRequired();
            b.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(50).IsRequired();
            b.Property(u => u.JobTitle).HasMaxLength(100);

            b.HasOne(u => u.Department)
             .WithMany(d => d.Users)
             .HasForeignKey(u => u.DepartmentId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ApplicationRole>(b =>
        {
            b.ToTable("AspNetRoles");
            b.Property(r => r.Description).HasMaxLength(250);
        });

        builder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.Token).IsUnique();
            b.Property(r => r.Token).HasMaxLength(256).IsRequired();
            b.Property(r => r.JwtId).HasMaxLength(128).IsRequired();

            b.HasOne(r => r.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Departments
        builder.Entity<Department>(b =>
        {
            b.ToTable("Departments");
            b.HasKey(d => d.Id);
            b.HasIndex(d => d.Code).IsUnique();
            b.Property(d => d.Code).HasMaxLength(20).IsRequired();
            b.Property(d => d.Name).HasMaxLength(100).IsRequired();
            b.Property(d => d.Location).HasMaxLength(150);
        });

        // Asset Categories
        builder.Entity<AssetCategory>(b =>
        {
            b.ToTable("AssetCategories");
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.Name).IsUnique();
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
            b.Property(c => c.Description).HasMaxLength(250);
        });

        // Assets
        builder.Entity<Asset>(b =>
        {
            b.ToTable("Assets");
            b.HasKey(a => a.Id);
            b.HasIndex(a => a.AssetTag).IsUnique();
            b.Property(a => a.AssetTag).HasMaxLength(50).IsRequired();
            b.Property(a => a.Name).HasMaxLength(150).IsRequired();
            b.Property(a => a.SerialNumber).HasMaxLength(100);
            b.Property(a => a.Description).HasMaxLength(500);

            // Concurrency Token
            b.Property(a => a.RowVersion).IsRowVersion();

            b.HasOne(a => a.Category)
             .WithMany(c => c.Assets)
             .HasForeignKey(a => a.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.Department)
             .WithMany(d => d.Assets)
             .HasForeignKey(a => a.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Work Orders
        builder.Entity<WorkOrder>(b =>
        {
            b.ToTable("WorkOrders");
            b.HasKey(w => w.Id);
            b.HasIndex(w => w.WorkOrderNumber).IsUnique();
            b.Property(w => w.WorkOrderNumber).HasMaxLength(50).IsRequired();
            b.Property(w => w.Title).HasMaxLength(200).IsRequired();
            b.Property(w => w.EstimatedHours).HasPrecision(5, 2);
            b.Property(w => w.ActualHours).HasPrecision(5, 2);

            // Concurrency Token
            b.Property(w => w.RowVersion).IsRowVersion();

            b.HasOne(w => w.Asset)
             .WithMany(a => a.WorkOrders)
             .HasForeignKey(w => w.AssetId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(w => w.CreatedBy)
             .WithMany(u => u.CreatedWorkOrders)
             .HasForeignKey(w => w.CreatedById)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(w => w.ApprovedBy)
             .WithMany(u => u.ApprovedWorkOrders)
             .HasForeignKey(w => w.ApprovedById)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(w => w.AssignedTechnician)
             .WithMany(u => u.AssignedWorkOrders)
             .HasForeignKey(w => w.AssignedTechnicianId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Work Order Checklist Items
        builder.Entity<WorkOrderChecklistItem>(b =>
        {
            b.ToTable("WorkOrderChecklistItems");
            b.HasKey(c => c.Id);
            b.Property(c => c.Description).HasMaxLength(300).IsRequired();
            b.Property(c => c.Notes).HasMaxLength(500);

            b.HasOne(c => c.WorkOrder)
             .WithMany(w => w.ChecklistItems)
             .HasForeignKey(c => c.WorkOrderId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.CompletedBy)
             .WithMany()
             .HasForeignKey(c => c.CompletedById)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Spare Parts
        builder.Entity<SparePart>(b =>
        {
            b.ToTable("SpareParts");
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.PartNumber).IsUnique();
            b.Property(p => p.PartNumber).HasMaxLength(50).IsRequired();
            b.Property(p => p.Name).HasMaxLength(150).IsRequired();
            b.Property(p => p.UnitPrice).HasPrecision(18, 2);
            b.Property(p => p.BinLocation).HasMaxLength(50).IsRequired();

            // Concurrency Token
            b.Property(p => p.RowVersion).IsRowVersion();
        });

        // Work Order Part Allocations
        builder.Entity<WorkOrderPartAllocation>(b =>
        {
            b.ToTable("WorkOrderPartAllocations");
            b.HasKey(a => a.Id);
            b.Property(a => a.UnitPriceAtUsage).HasPrecision(18, 2);

            b.HasOne(a => a.WorkOrder)
             .WithMany(w => w.PartAllocations)
             .HasForeignKey(a => a.WorkOrderId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.SparePart)
             .WithMany(p => p.Allocations)
             .HasForeignKey(a => a.SparePartId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Audit Logs
        builder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLogs");
            b.HasKey(l => l.Id);
            b.Property(l => l.Action).HasMaxLength(50).IsRequired();
            b.Property(l => l.EntityName).HasMaxLength(100).IsRequired();
            b.Property(l => l.EntityId).HasMaxLength(100).IsRequired();
            b.Property(l => l.UserEmail).HasMaxLength(100);
            b.Property(l => l.IpAddress).HasMaxLength(50);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Automatic Timestamp Updates on Auditable Entities
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        // 2. Automated Enterprise Audit Trail Generation
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            // Do not audit AuditLogs or RefreshTokens
            if (entry.Entity is AuditLog || entry.Entity is RefreshToken || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                EntityName = entry.Entity.GetType().Name,
                UserId = _currentUserService?.UserId,
                UserEmail = _currentUserService?.UserEmail
            };

            auditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "CREATE";
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        auditEntry.Action = "DELETE";
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.Action = "UPDATE";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        return auditEntries.Where(e => e.HasTemporaryProperties).ToList();
    }

    private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries, CancellationToken cancellationToken)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }
            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        await base.SaveChangesAsync(cancellationToken);
    }
}

internal class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public bool HasTemporaryProperties => TemporaryProperties.Count > 0;

    public AuditLog ToAuditLog()
    {
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            UserEmail = UserEmail,
            EntityName = EntityName,
            Action = Action,
            TimestampUtc = DateTime.UtcNow,
            EntityId = JsonSerializer.Serialize(KeyValues),
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues)
        };
        return audit;
    }
}
