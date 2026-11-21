using ServiceFlow.Domain.Common;
using ServiceFlow.Domain.Enums;

namespace ServiceFlow.Domain.Entities;

public class Tenant : Entity
{
    public string Name { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public string? ContactEmail { get; private set; }

    public string? ContactPhone { get; private set; }

    public string? AddressLine1 { get; private set; }

    public string? AddressLine2 { get; private set; }

    public string? City { get; private set; }

    public string TimeZoneId { get; private set; } = "Asia/Colombo";

    public TenantStatus Status { get; private set; } = TenantStatus.PendingSetup;

    private Tenant()
    {
    }

    public static Tenant Create(
        string name,
        string slug,
        string? contactEmail = null,
        string? contactPhone = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string timeZoneId = "Asia/Colombo")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Tenant
        {
            Name = name.Trim(),
            Slug = NormalizeSlug(slug),
            ContactEmail = NormalizeOptional(contactEmail),
            ContactPhone = NormalizeOptional(contactPhone),
            AddressLine1 = NormalizeOptional(addressLine1),
            AddressLine2 = NormalizeOptional(addressLine2),
            City = NormalizeOptional(city),
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Colombo" : timeZoneId.Trim(),
            Status = TenantStatus.PendingSetup
        };
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        Touch();
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        Touch();
    }

    public void UpdateProfile(
        string name,
        string? contactEmail,
        string? contactPhone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string timeZoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        ContactEmail = NormalizeOptional(contactEmail);
        ContactPhone = NormalizeOptional(contactPhone);
        AddressLine1 = NormalizeOptional(addressLine1);
        AddressLine2 = NormalizeOptional(addressLine2);
        City = NormalizeOptional(city);
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneId : timeZoneId.Trim();
        Touch();
    }

    private static string NormalizeSlug(string slug)
    {
        return slug.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
