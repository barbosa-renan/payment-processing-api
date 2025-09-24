using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingAPI.Models;

public enum PaymentStatus
{
    Pending,
    Processing,
    Approved,
    Declined,
    Cancelled,
    Refunded,
    Failed,
    Expired
}

public enum PaymentMethod
{
    CreditCard,
    Pix,
    Boleto,
    Debit
}

public enum Currency
{
    BRL,
    USD,
    EUR
}