using System;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.DbContexts;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Implementations;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.EntityFramework.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Firebend.AutoCrud.Tests.ChangeTracking.EntityFramework;

public class ProviderTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
}

public class ProviderCustomRow : ChangeTrackingEntity<Guid, ProviderTestEntity>
{
    public string RealActorEmail { get; set; }
}

public class FakeProviderTableNameProvider : IChangeTrackingTableNameProvider<Guid, ProviderTestEntity>
{
    public TableNameResult GetTableName() => new("ProviderTestEntity_Changes", "dbo");
}

[TestFixture]
public class ChangeTrackingDbContextProviderTests
{
    [Test]
    public void Provider_ImplementsGenericInterfaces_ForCustomRowType()
    {
        // ChangeTrackingDbContext takes a second constructor parameter (the table name provider),
        // so the pooled factory must be built through DI rather than `new PooledDbContextFactory<T>(options)`,
        // which only supports context types with a single DbContextOptions constructor parameter.
        var services = new ServiceCollection();
        services.AddSingleton<IChangeTrackingTableNameProvider<Guid, ProviderTestEntity>>(new FakeProviderTableNameProvider());
        services.AddPooledDbContextFactory<ChangeTrackingDbContext<Guid, ProviderTestEntity, ProviderCustomRow>>(options =>
            options.UseSqlServer("Data Source=.;Initial Catalog=Ignored;Integrated Security=True;"));

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider
            .GetRequiredService<IDbContextFactory<ChangeTrackingDbContext<Guid, ProviderTestEntity, ProviderCustomRow>>>();

        var provider = new ChangeTrackingDbContextProvider<Guid, ProviderTestEntity, ProviderCustomRow>(
            factory,
            NullLogger<ChangeTrackingDbContextProvider<Guid, ProviderTestEntity, ProviderCustomRow>>.Instance,
            new FakeProviderTableNameProvider());

        provider.Should().BeAssignableTo<IChangeTrackingDbContextProvider<Guid, ProviderTestEntity, ProviderCustomRow>>();
        provider.Should().BeAssignableTo<IDbContextProvider<Guid, ProviderCustomRow>>();
    }
}
