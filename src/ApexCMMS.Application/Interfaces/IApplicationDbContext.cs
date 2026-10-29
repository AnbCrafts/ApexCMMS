using Microsoft.EntityFrameworkCore;
using ApexCMMS.Domain.Entities;

namespace ApexCMMS.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<AssetCategory> AssetCategories { get; }
    DbSet<Asset> Assets { get; }
    DbSet<WorkOrder> WorkOrders { get; }
    DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems { get; }
    DbSet<SparePart> SpareParts { get; }
    DbSet<WorkOrderPartAllocation> WorkOrderPartAllocations { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
