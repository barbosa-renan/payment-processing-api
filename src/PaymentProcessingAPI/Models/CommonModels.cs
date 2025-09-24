using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingAPI.Models;

public class AddressInfo
{
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Number { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Complement { get; set; }

    [Required]
    [StringLength(100)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
}

public class CustomerInfo
{
    [Required]
    [StringLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Document { get; set; } = string.Empty; // CPF/CNPJ

    [Required]
    public AddressInfo Address { get; set; } = new();
}

public class CardInfo
{
    [Required]
    [StringLength(19)]
    public string Number { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string HolderName { get; set; } = string.Empty;

    [Required]
    [StringLength(2)]
    public string ExpiryMonth { get; set; } = string.Empty;

    [Required]
    [StringLength(4)]
    public string ExpiryYear { get; set; } = string.Empty;

    [Required]
    [StringLength(4)]
    public string CVV { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Brand { get; set; } = string.Empty; // VISA, MASTERCARD, etc
}

public class PaymentFees
{
    public decimal ProcessingFee { get; set; }
    public decimal GatewayFee { get; set; }
    public decimal TotalFees { get; set; }
    public decimal NetAmount { get; set; }
}