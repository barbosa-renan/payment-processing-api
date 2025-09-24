using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentProcessingAPI.Models.Entities;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty;

    [StringLength(50)]
    public string? AuthorizationCode { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcessedAmount { get; set; }

    [StringLength(500)]
    public string? Message { get; set; }

    // Customer Information
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string CustomerDocument { get; set; } = string.Empty;

    // Address Information
    [Required]
    [StringLength(200)]
    public string AddressStreet { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string AddressNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string? AddressComplement { get; set; }

    [Required]
    [StringLength(100)]
    public string AddressNeighborhood { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AddressCity { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string AddressState { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string AddressZipCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string AddressCountry { get; set; } = string.Empty;

    // Card Information (masked)
    [StringLength(19)]
    public string? CardNumberMasked { get; set; }

    [StringLength(100)]
    public string? CardHolderName { get; set; }

    [StringLength(20)]
    public string? CardBrand { get; set; }

    // Fees Information
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProcessingFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GatewayFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalFees { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetAmount { get; set; }

    // Metadata stored as JSON
    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}