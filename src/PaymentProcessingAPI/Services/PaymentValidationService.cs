using PaymentProcessingAPI.Services.Interfaces;
using System.Text.RegularExpressions;

namespace PaymentProcessingAPI.Services;

public class PaymentValidationService : IPaymentValidationService
{
    private readonly ILogger<PaymentValidationService> _logger;
    private static readonly Dictionary<string, int> _customerRequestCount = new();
    private static readonly object _lockObject = new();

    public PaymentValidationService(ILogger<PaymentValidationService> logger)
    {
        _logger = logger;
    }

    public bool ValidateCreditCard(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            return false;

        // Remove spaces and dashes
        cardNumber = Regex.Replace(cardNumber, @"[\s-]", "");

        // Check if it's all digits
        if (!Regex.IsMatch(cardNumber, @"^\d+$"))
            return false;

        // Check length (13-19 digits)
        if (cardNumber.Length < 13 || cardNumber.Length > 19)
            return false;

        // Luhn algorithm
        return IsValidLuhn(cardNumber);
    }

    public bool ValidateDocument(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return false;

        // Remove non-digits
        document = Regex.Replace(document, @"[^\d]", "");

        // Check if it's CPF (11 digits) or CNPJ (14 digits)
        return document.Length switch
        {
            11 => IsValidCPF(document),
            14 => IsValidCNPJ(document),
            _ => false
        };
    }

    public bool ValidateTransactionDuplication(string transactionId)
    {
        // This is a simple in-memory check
        // In production, this should check against the database
        return !string.IsNullOrWhiteSpace(transactionId) && Guid.TryParse(transactionId, out _);
    }

    public async Task<bool> ValidateRateLimitAsync(string customerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return false;

        const int maxRequestsPerMinute = 10;
        var currentMinute = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm");
        var key = $"{customerId}:{currentMinute}";

        lock (_lockObject)
        {
            if (!_customerRequestCount.ContainsKey(key))
            {
                _customerRequestCount[key] = 0;
                
                // Clean old entries (keep only current and previous minute)
                var keysToRemove = _customerRequestCount.Keys
                    .Where(k => !k.Contains(currentMinute) && !k.Contains(DateTime.UtcNow.AddMinutes(-1).ToString("yyyy-MM-dd-HH-mm")))
                    .ToList();
                
                foreach (var keyToRemove in keysToRemove)
                {
                    _customerRequestCount.Remove(keyToRemove);
                }
            }

            _customerRequestCount[key]++;
            var currentCount = _customerRequestCount[key];

            if (currentCount > maxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for customer {CustomerId}: {CurrentCount} requests in current minute", 
                    customerId, currentCount);
                return false;
            }
        }

        return true;
    }

    private static bool IsValidLuhn(string cardNumber)
    {
        var sum = 0;
        var alternate = false;

        for (var i = cardNumber.Length - 1; i >= 0; i--)
        {
            var n = int.Parse(cardNumber[i].ToString());

            if (alternate)
            {
                n *= 2;
                if (n > 9)
                    n -= 9;
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    private static bool IsValidCPF(string cpf)
    {
        // Check for known invalid CPFs
        if (cpf.All(c => c == cpf[0]))
            return false;

        // Calculate first digit
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cpf[9].ToString()) != digit1)
            return false;

        // Calculate second digit
        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cpf[10].ToString()) == digit2;
    }

    private static bool IsValidCNPJ(string cnpj)
    {
        // Check for known invalid CNPJs
        if (cnpj.All(c => c == cnpj[0]))
            return false;

        // Calculate first digit
        var weights1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += int.Parse(cnpj[i].ToString()) * weights1[i];

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cnpj[12].ToString()) != digit1)
            return false;

        // Calculate second digit
        var weights2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = 0;
        for (var i = 0; i < 13; i++)
            sum += int.Parse(cnpj[i].ToString()) * weights2[i];

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cnpj[13].ToString()) == digit2;
    }
}