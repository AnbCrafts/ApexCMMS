using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ApexCMMS.Domain.Entities;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedDatabaseAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Ensure Database is created
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Departments
        var deptOpsId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var deptElecId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var deptMechId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var deptSafeId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        if (!await context.Departments.AnyAsync())
        {
            var departments = new List<Department>
            {
                new() { Id = deptOpsId, Code = "DEP-OPS", Name = "Plant Operations", Location = "Main Plant - Floor 1", IsActive = true },
                new() { Id = deptElecId, Code = "DEP-ELEC", Name = "Electrical Maintenance", Location = "Substation Building A", IsActive = true },
                new() { Id = deptMechId, Code = "DEP-MECH", Name = "Mechanical & Hydraulics", Location = "Workshop Bay 3", IsActive = true },
                new() { Id = deptSafeId, Code = "DEP-SAFE", Name = "Safety & Compliance", Location = "Safety HQ - Bldg C", IsActive = true }
            };

            await context.Departments.AddRangeAsync(departments);
            await context.SaveChangesAsync();
        }

        // 2. Seed Roles
        string[] roles = { "SuperAdmin", "OperationsManager", "FieldTechnician", "SafetyAuditor" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole(roleName, $"ApexCMMS {roleName} Role");
                await roleManager.CreateAsync(role);
            }
        }

        // 3. Seed Users
        // Admin
        var adminEmail = "admin@apexcmms.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Apex",
                LastName = "Administrator",
                EmployeeCode = "EMP-0001",
                JobTitle = "Global System Administrator",
                IsActive = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }

        // Manager
        var managerEmail = "manager@apexcmms.com";
        var managerUser = await userManager.FindByEmailAsync(managerEmail);
        if (managerUser == null)
        {
            managerUser = new ApplicationUser
            {
                UserName = managerEmail,
                Email = managerEmail,
                EmailConfirmed = true,
                FirstName = "Sarah",
                LastName = "Connor",
                EmployeeCode = "EMP-1042",
                DepartmentId = deptOpsId,
                JobTitle = "Operations Plant Manager",
                IsActive = true
            };
            var result = await userManager.CreateAsync(managerUser, "Manager@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(managerUser, "OperationsManager");
            }
        }

        // Field Technician
        var techEmail = "tech@apexcmms.com";
        var techUser = await userManager.FindByEmailAsync(techEmail);
        if (techUser == null)
        {
            techUser = new ApplicationUser
            {
                UserName = techEmail,
                Email = techEmail,
                EmailConfirmed = true,
                FirstName = "James",
                LastName = "Holden",
                EmployeeCode = "EMP-2088",
                DepartmentId = deptElecId,
                JobTitle = "Senior Lead Field Technician",
                IsActive = true
            };
            var result = await userManager.CreateAsync(techUser, "Tech@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(techUser, "FieldTechnician");
            }
        }

        // Safety Auditor
        var auditorEmail = "auditor@apexcmms.com";
        var auditorUser = await userManager.FindByEmailAsync(auditorEmail);
        if (auditorUser == null)
        {
            auditorUser = new ApplicationUser
            {
                UserName = auditorEmail,
                Email = auditorEmail,
                EmailConfirmed = true,
                FirstName = "Elena",
                LastName = "Rostova",
                EmployeeCode = "EMP-3011",
                DepartmentId = deptSafeId,
                JobTitle = "ISO Compliance Lead Auditor",
                IsActive = true
            };
            var result = await userManager.CreateAsync(auditorUser, "Auditor@123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(auditorUser, "SafetyAuditor");
            }
        }

        // 4. Seed Asset Categories
        var catTurbineId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
        var catHvacId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
        var catHydraulicId = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC");

        if (!await context.AssetCategories.AnyAsync())
        {
            var categories = new List<AssetCategory>
            {
                new() { Id = catTurbineId, Name = "Turbines & Generators", Description = "Heavy power generation equipment" },
                new() { Id = catHvacId, Name = "Industrial HVAC", Description = "Facility chillers and cooling towers" },
                new() { Id = catHydraulicId, Name = "Pumps & Hydraulics", Description = "High-pressure hydraulic pumps and valves" }
            };
            await context.AssetCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 5. Seed Assets
        var asset1Id = Guid.Parse("E1111111-1111-1111-1111-111111111111");
        var asset2Id = Guid.Parse("E2222222-2222-2222-2222-222222222222");

        if (!await context.Assets.AnyAsync())
        {
            var assets = new List<Asset>
            {
                new()
                {
                    Id = asset1Id,
                    AssetTag = "AST-2026-001",
                    Name = "Gas Turbine Generator Alpha",
                    Description = "Primary 45MW gas turbine powering Facility Sector 1",
                    SerialNumber = "SN-GT45-98214",
                    CategoryId = catTurbineId,
                    DepartmentId = deptOpsId,
                    Status = AssetStatus.Operational,
                    Criticality = AssetCriticality.MissionCritical,
                    PurchaseDate = DateTime.UtcNow.AddYears(-3),
                    WarrantyExpiryDate = DateTime.UtcNow.AddYears(2)
                },
                new()
                {
                    Id = asset2Id,
                    AssetTag = "AST-2026-002",
                    Name = "Centrifugal Chiller 500-Ton",
                    Description = "Central industrial chiller unit for server halls",
                    SerialNumber = "SN-CH500-1102",
                    CategoryId = catHvacId,
                    DepartmentId = deptElecId,
                    Status = AssetStatus.Operational,
                    Criticality = AssetCriticality.High,
                    PurchaseDate = DateTime.UtcNow.AddYears(-2),
                    WarrantyExpiryDate = DateTime.UtcNow.AddYears(1)
                }
            };
            await context.Assets.AddRangeAsync(assets);
            await context.SaveChangesAsync();
        }

        // 6. Seed Spare Parts
        if (!await context.SpareParts.AnyAsync())
        {
            var parts = new List<SparePart>
            {
                new() { Id = Guid.NewGuid(), PartNumber = "SP-BRG-6204", Name = "Deep Groove Ball Bearing 6204", Description = "High speed precision bearing", UnitPrice = 45.50m, QuantityOnHand = 48, MinimumStockLevel = 10, BinLocation = "BIN-A1-04" },
                new() { Id = Guid.NewGuid(), PartNumber = "SP-FLT-HVAC01", Name = "HEPA Air Filter 24x24x12", Description = "Commercial air filter for chillers", UnitPrice = 85.00m, QuantityOnHand = 32, MinimumStockLevel = 8, BinLocation = "BIN-B2-12" },
                new() { Id = Guid.NewGuid(), PartNumber = "SP-VLV-HYD02", Name = "High-Pressure Hydraulic Solenoid Valve", Description = "24V DC 350-bar valve", UnitPrice = 320.00m, QuantityOnHand = 4, MinimumStockLevel = 5, BinLocation = "BIN-C3-01" }, // Low stock!
                new() { Id = Guid.NewGuid(), PartNumber = "SP-FUS-HV100", Name = "100A High Voltage Fuse Link", Description = "Distribution cartridge fuse", UnitPrice = 65.00m, QuantityOnHand = 60, MinimumStockLevel = 15, BinLocation = "BIN-D1-09" }
            };
            await context.SpareParts.AddRangeAsync(parts);
            await context.SaveChangesAsync();
        }

        // 7. Seed Work Orders
        if (!await context.WorkOrders.AnyAsync() && adminUser != null)
        {
            var woId = Guid.NewGuid();
            var workOrder = new WorkOrder
            {
                Id = woId,
                WorkOrderNumber = "WO-2026-00001",
                Title = "Scheduled Q4 Vibration Analysis & Rotor Lubrication",
                Description = "Conduct ISO-certified vibration harmonics analysis and apply synthetic grease to bearing assembly.",
                AssetId = asset1Id,
                Priority = WorkOrderPriority.High,
                Status = WorkOrderStatus.InProgress,
                CreatedById = adminUser.Id,
                ApprovedById = managerUser?.Id,
                AssignedTechnicianId = techUser?.Id,
                EstimatedHours = 4.5m,
                ScheduledDate = DateTime.UtcNow.AddDays(1)
            };

            var checklist = new List<WorkOrderChecklistItem>
            {
                new() { Id = Guid.NewGuid(), WorkOrderId = woId, StepNumber = 1, Description = "Apply Lockout/Tagout (LOTO) to primary 45MW circuit breakers", IsCompleted = true, CompletedAtUtc = DateTime.UtcNow.AddHours(-2), CompletedById = techUser?.Id, Notes = "Breakers locked and tagged #LOTO-881" },
                new() { Id = Guid.NewGuid(), WorkOrderId = woId, StepNumber = 2, Description = "Inspect turbine casing thermal insulation blankets", IsCompleted = true, CompletedAtUtc = DateTime.UtcNow.AddHours(-1), CompletedById = techUser?.Id, Notes = "No thermal degradation found" },
                new() { Id = Guid.NewGuid(), WorkOrderId = woId, StepNumber = 3, Description = "Measure vibration amplitude at bearing housing (Limit: < 2.5 mm/s)", IsCompleted = false },
                new() { Id = Guid.NewGuid(), WorkOrderId = woId, StepNumber = 4, Description = "Purge and replenish high-temp synthetic lubricant", IsCompleted = false }
            };

            await context.WorkOrders.AddAsync(workOrder);
            await context.WorkOrderChecklistItems.AddRangeAsync(checklist);
            await context.SaveChangesAsync();
        }
    }
}
