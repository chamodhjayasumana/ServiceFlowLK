using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Domain.Entities;
using ServiceFlow.Infrastructure.Persistence;
using ServiceFlow.Infrastructure.Tenancy;

namespace ServiceFlow.IntegrationTests.Persistence;

public class TenantIsolationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private TenantContext _tenantContext = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        _tenantContext = new TenantContext();

        await using var setup = CreateContext();
        await setup.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task QueryFilter_HidesOtherTenantCustomers()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seed = CreateContext())
        {
            seed.Tenants.Add(Tenant.Create("A", "tenant-a"));
            seed.Tenants.Add(Tenant.Create("B", "tenant-b"));
            // Replace generated IDs by re-creating with known IDs via tracked entities is awkward;
            // seed customers with explicit tenant ids matching newly created tenants.
            await seed.SaveChangesAsync();

            var tenants = await seed.Tenants.AsNoTracking().ToListAsync();
            tenantA = tenants.Single(t => t.Slug == "tenant-a").Id;
            tenantB = tenants.Single(t => t.Slug == "tenant-b").Id;

            seed.Customers.Add(Customer.Create(tenantA, "Alice", "0771111111"));
            seed.Customers.Add(Customer.Create(tenantB, "Bob", "0772222222"));
            await seed.SaveChangesAsync();
        }

        _tenantContext.SetTenant(tenantA);

        await using var query = CreateContext();
        var visible = await query.Customers.ToListAsync();

        visible.Should().ContainSingle();
        visible[0].FullName.Should().Be("Alice");
        visible[0].TenantId.Should().Be(tenantA);
    }

    [Fact]
    public async Task SaveChanges_RejectsCrossTenantWrite_WhenTenantContextIsSet()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var seed = CreateContext())
        {
            var a = Tenant.Create("A", "a-shop");
            var b = Tenant.Create("B", "b-shop");
            seed.Tenants.AddRange(a, b);
            await seed.SaveChangesAsync();
            tenantA = a.Id;
            tenantB = b.Id;
        }

        _tenantContext.SetTenant(tenantA);

        await using var db = CreateContext();
        db.Customers.Add(Customer.Create(tenantB, "Intruder", "0773333333"));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cross-tenant write rejected*");
    }

    [Fact]
    public async Task QueryFilter_ReturnsEmpty_WhenNoTenantContext()
    {
        await using (var seed = CreateContext())
        {
            var tenant = Tenant.Create("Solo", "solo");
            seed.Tenants.Add(tenant);
            await seed.SaveChangesAsync();
            seed.Customers.Add(Customer.Create(tenant.Id, "Visible Only With Context", "0774444444"));
            await seed.SaveChangesAsync();
        }

        await using var query = CreateContext();
        var visible = await query.Customers.ToListAsync();

        visible.Should().BeEmpty();
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options, _tenantContext);
    }
}
