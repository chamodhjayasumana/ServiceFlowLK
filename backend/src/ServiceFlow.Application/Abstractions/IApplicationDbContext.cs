using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Entities;

namespace ServiceFlow.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<Customer> Customers { get; }

    DbSet<EmployeeInvitation> EmployeeInvitations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
