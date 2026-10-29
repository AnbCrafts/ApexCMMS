# ApexCMMS: Enterprise Asset & Maintenance Management System

An industrial-grade Enterprise Asset and Maintenance Management System (CMMS) built with **ASP.NET Core 10 / 9 MVC** following **Clean Architecture** principles.

Designed for high-reliability operational facilities (manufacturing plants, power generation, data centers, hospitals), this application demonstrates a complete production-grade feature set including **Hybrid Security (Identity Cookies + JWT Bearer with Refresh Token Rotation)**, **Claims- and Policy-based RBAC**, **Department Isolation**, **State & Session-backed Multi-Step Diagnostic Wizards**, **EF Core Optimistic Concurrency Protection**, and an **Automated Entity ChangeTracker Audit Trail**.

---

## Architecture Overview

ApexCMMS adheres strictly to **Clean Architecture** (Separation of Concerns & Dependency Inversion):

```
D:\Anubhaw\Projects\Structure\ApexCMMS\
├── ApexCMMS.sln
└── src/
    ├── ApexCMMS.Domain/          # Core entities, Enums, Value Objects, Auditable interfaces (Zero external dependencies)
    │   ├── Common/               # BaseEntity, IAuditableEntity
    │   ├── Entities/             # Asset, WorkOrder, SparePart, Department, AuditLog, ApplicationUser, RefreshToken
    │   └── Enums/                # AssetStatus, AssetCriticality, WorkOrderPriority, WorkOrderStatus
    │
    ├── ApexCMMS.Application/     # Application contracts, DTOs, Business Rules
    │   ├── DTOs/                 # Auth, Assets, WorkOrders, SpareParts, Dashboard, Inspection Wizard
    │   └── Interfaces/           # IApplicationDbContext, ICurrentUserService, IJwtTokenService
    │
    ├── ApexCMMS.Infrastructure/  # EF Core, Data Persistence, Security, Identity, Services
    │   ├── Data/                 # ApplicationDbContext (Projects schema, Auto-Audit), DbInitializer (Seed data)
    │   └── Services/             # JwtTokenService (Token rotation & revocation), CurrentUserService
    │
    └── ApexCMMS.Web/             # ASP.NET Core MVC Presentation & REST API
        ├── Controllers/          # Dashboard, Assets, WorkOrders, InspectionWizard, SpareParts, Audit, Account
        ├── Controllers/Api/      # REST API: AuthController (JWT), TechnicianApiController (Field App)
        ├── Security/             # DepartmentAuthorizationHandler, Policies
        ├── Middleware/           # SecurityHeadersMiddleware (Anti-clickjacking, CSP, XSS)
        └── Views/                # Responsive Razor Views, Multi-step wizard, KPI dashboards
```

---

## Database Architecture (`Projects` Schema)

The complete SQL DDL and Stored Procedures are maintained in:
📂 `D:\Anubhaw\Projects\DB\Enterprise CMMS DB.sql`

### Core Relational Tables
1. **`Projects.Departments`**: Facility departments with code and physical location.
2. **`Projects.AspNetUsers` / `Projects.AspNetRoles`**: ASP.NET Identity extended with `EmployeeCode`, `DepartmentId`, and `JobTitle`.
3. **`Projects.RefreshTokens`**: Cryptographically secure JWT refresh tokens supporting token families and rotation.
4. **`Projects.AssetCategories`**: Equipment taxonomy (Turbines, Industrial HVAC, Hydraulics, Switchgear).
5. **`Projects.Assets`**: Physical assets with QR/Barcode tags, criticality levels, operational status, and `RowVersion` concurrency tokens.
6. **`Projects.WorkOrders`**: Maintenance lifecycle orders with state transitions, priority, and labor hours.
7. **`Projects.WorkOrderChecklistItems`**: Step-by-step diagnostic/safety checklists with technician sign-off timestamps.
8. **`Projects.SpareParts`**: Warehouse inventory with stock on hand, unit prices, minimum reorder thresholds, and bin locations.
9. **`Projects.WorkOrderPartAllocations`**: Parts consumed during repairs snapshotting unit price at usage.
10. **`Projects.AuditLogs`**: Immutable audit logs capturing `OldValues` and `NewValues` in JSON diff format.

### Production Stored Procedures
- `Projects.sp_CreateWorkOrder`: Transactional creation with auto-generated sequential number (`WO-YYYY-XXXXX`) and initial audit log.
- `Projects.sp_ApproveWorkOrder`: Managerial authorization, status change, and asset status locking.
- `Projects.sp_AllocateSparePart`: Atomic inventory deduction with stock level validation and race condition prevention.
- `Projects.sp_GetAssetMaintenanceSummary`: KPI metrics, total orders, completed orders, and cost incurred.
- `Projects.sp_RotateRefreshToken`: Cryptographic JWT refresh token rotation with replay attack detection.

---

## Security & RBAC Specification

### Hybrid Authentication Engine
- **Web UI (Razor MVC)**: ASP.NET Core Identity secure cookies (`HttpOnly`, `SameSite=Strict`, `SecurePolicy=SameAsRequest`, Anti-CSRF token verification on all POST/PUT/DELETE requests).
- **Field Technician REST API**: JWT Bearer tokens signed with HMAC-SHA256, carrying user claims (`sub`, `email`, `role`, `DepartmentId`, `EmployeeCode`, `jti`). Refresh tokens rotated on every exchange.

### Role & Permission Matrix
| Capability | SuperAdmin | OperationsManager | FieldTechnician | SafetyAuditor |
| :--- | :---: | :---: | :---: | :---: |
| View Operations Dashboard | Yes | Yes | Yes | Yes |
| Department Asset Management | All Depts | Assigned Dept | Assigned Dept | Read-Only |
| Create Work Orders | Yes | Yes | Yes | No |
| Approve Work Orders | Yes | Yes | No | No |
| Execute & Check Off Checklist | Yes | Yes | Yes | No |
| Allocate Warehouse Spare Parts | Yes | Yes | Yes | No |
| Multi-Step Inspection Wizard | Yes | Yes | Yes | No |
| View Security & Audit Logs | Full Access | No | No | Read-Only |

---

## State & Session Management
- **Multi-Step Inspection Wizard**: Allows technicians to record environmental pre-checks (**Step 1**), diagnostic findings and task lists (**Step 2**), and reserve spare parts in a session cart (**Step 3**) before atomically committing the work order and checklist to the database.
- **Optimistic Concurrency**: Assets and Spare Parts use `byte[] RowVersion` concurrency tokens. Simultaneous edits from multiple operators trigger a `DbUpdateConcurrencyException`, preventing silent data overwrites.

---

## Demo Credentials (1-Click Login Available)

| Role | Email | Password | Scope |
| :--- | :--- | :--- | :--- |
| **SuperAdmin** | `admin@apexcmms.com` | `Admin@123!` | Global administrative access |
| **Operations Manager** | `manager@apexcmms.com` | `Manager@123!` | Plant Operations (`DEP-OPS`) |
| **Field Technician** | `tech@apexcmms.com` | `Tech@123!` | Electrical Maintenance (`DEP-ELEC`) |
| **Safety Auditor** | `auditor@apexcmms.com` | `Auditor@123!` | ISO Compliance & Audit Logs (`DEP-SAFE`) |

---

## How to Run the Application

```powershell
cd 'D:\Anubhaw\Projects\Structure\ApexCMMS'
dotnet run --project src/ApexCMMS.Web
```

Open your browser at: **`http://localhost:5164`** (or `https://localhost:7142`).

---

## Field Technician REST API Testing (PowerShell)

### 1. Authenticate & Obtain JWT Token
```powershell
$body = @{ usernameOrEmail = "tech@apexcmms.com"; password = "Tech@123!" } | ConvertTo-Json
$auth = Invoke-RestMethod -Uri "http://localhost:5164/api/v1/auth/token" -Method Post -ContentType "application/json" -Body $body
$token = $auth.accessToken
```

### 2. Fetch Assigned Work Orders (Protected with Bearer Token)
```powershell
$headers = @{ Authorization = "Bearer " + $token }
Invoke-RestMethod -Uri "http://localhost:5164/api/v1/technician/work-orders" -Method Get -Headers $headers
```
