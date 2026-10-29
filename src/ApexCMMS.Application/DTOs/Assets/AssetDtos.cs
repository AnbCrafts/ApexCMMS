using System.ComponentModel.DataAnnotations;
using ApexCMMS.Domain.Enums;

namespace ApexCMMS.Application.DTOs.Assets;

public class AssetDto
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public AssetStatus Status { get; set; }
    public AssetCriticality Criticality { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int ActiveWorkOrdersCount { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CreateAssetDto
{
    [Required]
    [StringLength(50)]
    [Display(Name = "Asset Tag (e.g., AST-2026-005)")]
    public string AssetTag { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    [Required]
    [Display(Name = "Category")]
    public Guid CategoryId { get; set; }

    [Required]
    [Display(Name = "Department")]
    public Guid DepartmentId { get; set; }

    [Required]
    public AssetStatus Status { get; set; } = AssetStatus.Operational;

    [Required]
    public AssetCriticality Criticality { get; set; } = AssetCriticality.Medium;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Purchase Date")]
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

    [DataType(DataType.Date)]
    [Display(Name = "Warranty Expiry Date")]
    public DateTime? WarrantyExpiryDate { get; set; }
}

public class EditAssetDto : CreateAssetDto
{
    public Guid Id { get; set; }
    public byte[]? RowVersion { get; set; }
}
