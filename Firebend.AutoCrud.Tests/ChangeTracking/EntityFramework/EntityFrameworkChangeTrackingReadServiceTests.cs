using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.DbContexts;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Implementations;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Implementations.Defaults;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.EntityFramework.Client;
using Firebend.AutoCrud.EntityFramework.Including;
using Firebend.AutoCrud.EntityFramework.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Firebend.AutoCrud.Tests.ChangeTracking.EntityFramework;

public class ReadServiceTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class ReadServiceCustomRow : ChangeTrackingEntity<Guid, ReadServiceTestEntity>
{
    public string RealActorEmail { get; set; }
}

public class FakeReadServiceTableNameProvider : IChangeTrackingTableNameProvider<Guid, ReadServiceTestEntity>
{
    public TableNameResult GetTableName() => new("ReadServiceTestEntity_Changes", "dbo");
}

public class InMemoryReadServiceDbContextProvider<TChangeTrackingEntity> : IDbContextProvider<Guid, TChangeTrackingEntity>
    where TChangeTrackingEntity : ChangeTrackingEntity<Guid, ReadServiceTestEntity>
{
    private readonly DbContextOptions<ChangeTrackingDbContext<Guid, ReadServiceTestEntity, TChangeTrackingEntity>> _options;

    public InMemoryReadServiceDbContextProvider(
        DbContextOptions<ChangeTrackingDbContext<Guid, ReadServiceTestEntity, TChangeTrackingEntity>> options)
    {
        _options = options;
    }

    public Task<IDbContext> GetDbContextAsync(CancellationToken cancellationToken)
        => Task.FromResult<IDbContext>(new ChangeTrackingDbContext<Guid, ReadServiceTestEntity, TChangeTrackingEntity>(
            _options,
            new FakeReadServiceTableNameProvider()));

    public Task<IDbContext> GetDbContextAsync(DbTransaction connection, CancellationToken cancellationToken)
        => GetDbContextAsync(cancellationToken);
}

[TestFixture]
public class EntityFrameworkChangeTrackingReadServiceTests
{
    [Test]
    public async Task GetChangesByEntityId_WithCustomRowType_ReturnsExtraPropertiesFromStore()
    {
        var options = new DbContextOptionsBuilder<ChangeTrackingDbContext<Guid, ReadServiceTestEntity, ReadServiceCustomRow>>()
            .UseInMemoryDatabase(nameof(GetChangesByEntityId_WithCustomRowType_ReturnsExtraPropertiesFromStore))
            .Options;

        var entityId = Guid.NewGuid();

        await using (var seedContext = new ChangeTrackingDbContext<Guid, ReadServiceTestEntity, ReadServiceCustomRow>(
                         options, new FakeReadServiceTableNameProvider()))
        {
            seedContext.Changes.Add(new ReadServiceCustomRow
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                Action = "Added",
                RealActorEmail = "actor@test.com",
                ModifiedDate = DateTimeOffset.UtcNow
            });

            await seedContext.SaveChangesAsync();
        }

        var provider = new InMemoryReadServiceDbContextProvider<ReadServiceCustomRow>(options);
        var queryClient = new EntityFrameworkQueryClient<Guid, ReadServiceCustomRow>(
            provider,
            new DefaultEntityQueryOrderByHandler<Guid, ReadServiceCustomRow>(),
            new DefaultEntityFrameworkIncludesProvider<Guid, ReadServiceCustomRow>());

        var sut = new EntityFrameworkChangeTrackingReadService<Guid, ReadServiceTestEntity, ReadServiceCustomRow>(queryClient, null);

        var result = await sut.GetChangesByEntityId(
            new ChangeTrackingSearchRequest<Guid> { EntityId = entityId },
            CancellationToken.None);

        result.Data.Should().ContainSingle();
        result.Data.Single().RealActorEmail.Should().Be("actor@test.com");
    }
}
