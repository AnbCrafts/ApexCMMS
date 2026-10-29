using System.ComponentModel.DataAnnotations;

namespace ApexCMMS.Application.DTOs.SpareParts;

public class SparePartDto
{
    public Guid Id { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int QuantityOnHand { get; set; }
    public int MinimumStockLevel { get; set; }
    public string BinLocation { get; set; } = string.Empty;
    public bool IsLowStock => QuantityOnHand <= MinimumStockLevel;
    public byte[]? RowVersion { get; set; }
}

public class AllocateSparePartDto
{
    [Required]
    public Guid WorkOrderId { get; set; }

    [Required]
    public Guid SparePartId { get; set; }

    [Required]
    [Range(1, 1000, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
}
