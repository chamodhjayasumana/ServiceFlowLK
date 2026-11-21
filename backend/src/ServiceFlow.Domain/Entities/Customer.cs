using ServiceFlow.Domain.Common;

namespace ServiceFlow.Domain.Entities;

public class Customer : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? AddressLine1 { get; private set; }

    public string? City { get; private set; }

    public bool IsActive { get; private set; } = true;

    private Customer()
    {
    }

    public static Customer Create(
        Guid tenantId,
        string fullName,
        string phone,
        string? email = null,
        string? addressLine1 = null,
        string? city = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required and must be server-assigned.", nameof(tenantId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        return new Customer
        {
            TenantId = tenantId,
            FullName = fullName.Trim(),
            Phone = NormalizePhone(phone),
            Email = NormalizeOptional(email),
            AddressLine1 = NormalizeOptional(addressLine1),
            City = NormalizeOptional(city),
            IsActive = true
        };
    }

    public void Update(
        string fullName,
        string phone,
        string? email,
        string? addressLine1,
        string? city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);

        FullName = fullName.Trim();
        Phone = NormalizePhone(phone);
        Email = NormalizeOptional(email);
        AddressLine1 = NormalizeOptional(addressLine1);
        City = NormalizeOptional(city);
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>
    /// Basic normalization for Sri Lankan phone numbers. Full validation belongs in Application validators.
    /// </summary>
    public static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("94", StringComparison.Ordinal) && digits.Length >= 11)
        {
            return $"+{digits}";
        }

        if (digits.StartsWith('0') && digits.Length == 10)
        {
            return $"+94{digits[1..]}";
        }

        return phone.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
