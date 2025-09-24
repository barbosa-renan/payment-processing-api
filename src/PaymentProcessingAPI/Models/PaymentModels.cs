using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingAPI.Models;

public class PaymentRequest
{
    [Required]
    [StringLength(50)]
    public string TransactionId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Range(0.01, 1000000.00, ErrorMessage = "Amount must be between 0.01 and 1,000,000.00")]
    public decimal Amount { get; set; }

    [Required]
    [EnumDataType(typeof(Currency))]
    public Currency Currency { get; set; }

    [Required]
    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    public CustomerInfo Customer { get; set; } = new();

    public CardInfo? Card { get; set; } // Optional, apenas para cartões

    public Dictionary<string, object>? Metadata { get; set; }
}

public class PaymentResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? AuthorizationCode { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal ProcessedAmount { get; set; }
    public PaymentFees? Fees { get; set; }
}

public class PaymentWebhook
{
    [Required]
    [StringLength(50)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(PaymentStatus))]
    public PaymentStatus Status { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    public Dictionary<string, object>? Data { get; set; }
}

public class RefundRequest
{
    [Required]
    [Range(0.01, 1000000.00)]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class PaymentFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PaymentStatus? Status { get; set; }
    public string? CustomerId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}