using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.DbContexts;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Implementations;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Models.DomainEvents;
using Firebend.AutoCrud.EntityFramework.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Firebend.AutoCrud.Tests.ChangeTracking.EntityFramework;

public class ServiceTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class ServiceCustomRow : ChangeTrackingEntity<Guid, ServiceTestEntity>, IAuditContextProperties
{
    public string RealActorEmail { get; set; }

    public void PopulateFrom(DomainEventContext context) => RealActorEmail = context?.UserEmail;
}

public class FakeServiceTableNameProvider : IChangeTrackingTableNameProvider<Guid, ServiceTestEntity>
{
    public TableNameResult GetTableName() => new("ServiceTestEntity_Changes", "dbo");
}

// EntityFrameworkCreateClient.AddInternalAsync disposes the context it gets from the provider
// (`await using (var context = await GetDbContextAsync(...))`), so handing out one long-lived
// context instance (as the brief originally sketched) leaves it disposed before the test can
// inspect `Changes`. Instead, the provider hands out a fresh context per call, all pointed at the
// same named EF Core InMemory database, mirroring how a real pooled/DbContextFactory-backed
// provider behaves; tests then open their own fresh context against that same database name to
// verify what was persisted.
public class InMemoryChangeTrackingDbContextProvider<TChangeTrackingEntity> :
    IChangeTrackingDbContextProvider<Guid, ServiceTestEntity, TChangeTrackingEntity>
    where TChangeTrackingEntity : ChangeTrackingEntity<Guid, ServiceTestEntity>
{
    private readonly DbContextOptions<ChangeTrackingDbContext<Guid, ServiceTestEntity, TChangeTrackingEntity>> _options;

    public InMemoryChangeTrackingDbContextProvider(
        DbContextOptions<ChangeTrackingDbContext<Guid, ServiceTestEntity, TChangeTrackingEntity>> options)
    {
        _options = options;
    }

    public Task<IDbContext> GetDbContextAsync(CancellationToken cancellationToken)
        => Task.FromResult<IDbContext>(new ChangeTrackingDbContext<Guid, ServiceTestEntity, TChangeTrackingEntity>(
            _options,
            new FakeServiceTableNameProvider()));

    public Task<IDbContext> GetDbContextAsync(System.Data.Common.DbTransaction connection, CancellationToken cancellationToken)
        => GetDbContextAsync(cancellationToken);
}

[TestFixture]
public class EntityFrameworkChangeTrackingServiceTests
{
    private static DbContextOptions<ChangeTrackingDbContext<Guid, ServiceTestEntity, TRow>> BuildInMemoryOptions<TRow>(string dbName)
        where TRow : ChangeTrackingEntity<Guid, ServiceTestEntity>
        => new DbContextOptionsBuilder<ChangeTrackingDbContext<Guid, ServiceTestEntity, TRow>>()
            .UseInMemoryDatabase(dbName)
            .Options;

    [Test]
    public async Task TrackAddedAsync_WithCustomRowType_PopulatesExtraPropertiesFromDomainEventContext()
    {
        var options = BuildInMemoryOptions<ServiceCustomRow>(nameof(TrackAddedAsync_WithCustomRowType_PopulatesExtraPropertiesFromDomainEventContext));
        var provider = new InMemoryChangeTrackingDbContextProvider<ServiceCustomRow>(options);
        var sut = new EntityFrameworkChangeTrackingService<Guid, ServiceTestEntity, ServiceCustomRow>(provider);

        var entity = new ServiceTestEntity { Id = Guid.NewGuid(), Name = "Ada" };
        var domainEvent = new EntityAddedDomainEvent<ServiceTestEntity>
        {
            Entity = entity,
            Time = DateTimeOffset.UtcNow,
            EventContext = new DomainEventContext { Source = "Test", UserEmail = "actor@test.com" }
        };

        await sut.TrackAddedAsync(domainEvent, CancellationToken.None);

        await using var verifyContext = new ChangeTrackingDbContext<Guid, ServiceTestEntity, ServiceCustomRow>(
            options,
            new FakeServiceTableNameProvider());
        var saved = verifyContext.Changes.Single();
        saved.Action.Should().Be("Added");
        saved.UserEmail.Should().Be("actor@test.com");
        saved.RealActorEmail.Should().Be("actor@test.com");
    }

    [Test]
    public async Task TrackAddedAsync_WithDefaultRowType_StillWorksUnaffected()
    {
        var options = BuildInMemoryOptions<ChangeTrackingEntity<Guid, ServiceTestEntity>>(nameof(TrackAddedAsync_WithDefaultRowType_StillWorksUnaffected));
        var provider = new InMemoryChangeTrackingDbContextProvider<ChangeTrackingEntity<Guid, ServiceTestEntity>>(options);
        var sut = new EntityFrameworkChangeTrackingService<Guid, ServiceTestEntity, ChangeTrackingEntity<Guid, ServiceTestEntity>>(provider);

        var entity = new ServiceTestEntity { Id = Guid.NewGuid(), Name = "Ada" };
        var domainEvent = new EntityAddedDomainEvent<ServiceTestEntity>
        {
            Entity = entity,
            Time = DateTimeOffset.UtcNow,
            EventContext = new DomainEventContext { Source = "Test", UserEmail = "actor@test.com" }
        };

        await sut.TrackAddedAsync(domainEvent, CancellationToken.None);

        await using var verifyContext = new ChangeTrackingDbContext<Guid, ServiceTestEntity, ChangeTrackingEntity<Guid, ServiceTestEntity>>(
            options,
            new FakeServiceTableNameProvider());
        verifyContext.Changes.Single().Action.Should().Be("Added");
    }
}
