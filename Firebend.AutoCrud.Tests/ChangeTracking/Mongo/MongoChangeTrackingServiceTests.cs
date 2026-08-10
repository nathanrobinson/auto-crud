using System;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.ChangeTracking.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.ChangeTracking.Mongo.Implementations;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Models.DomainEvents;
using Firebend.AutoCrud.Mongo.Configuration;
using Firebend.AutoCrud.Mongo.Interfaces;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace Firebend.AutoCrud.Tests.ChangeTracking.Mongo;

public class MongoServiceTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

public class MongoServiceCustomRow : ChangeTrackingEntity<Guid, MongoServiceTestEntity>, IAuditContextProperties
{
    public string RealActorEmail { get; set; }

    public void PopulateFrom(DomainEventContext context) => RealActorEmail = context?.UserEmail;
}

public class CapturingMongoCreateClient<TEntity> : IMongoCreateClient<Guid, TEntity>
    where TEntity : IEntity<Guid>
{
    public TEntity Captured { get; private set; }

    public Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        Captured = entity;
        return Task.FromResult(entity);
    }

    public Task<TEntity> CreateAsync(TEntity entity, Firebend.AutoCrud.Core.Interfaces.Models.IEntityTransaction entityTransaction, CancellationToken cancellationToken)
        => CreateAsync(entity, cancellationToken);
}

[TestFixture]
public class MongoChangeTrackingServiceTests
{
    [Test]
    public async Task TrackAddedAsync_WithCustomRowType_PopulatesExtraPropertiesFromDomainEventContext()
    {
        var createClient = new CapturingMongoCreateClient<MongoServiceCustomRow>();
        var sut = new MongoChangeTrackingService<Guid, MongoServiceTestEntity, MongoServiceCustomRow>(createClient);

        var entity = new MongoServiceTestEntity { Id = Guid.NewGuid(), Name = "Ada" };
        var domainEvent = new EntityAddedDomainEvent<MongoServiceTestEntity>
        {
            Entity = entity,
            Time = DateTimeOffset.UtcNow,
            EventContext = new DomainEventContext { Source = "Test", UserEmail = "actor@test.com" }
        };

        await sut.TrackAddedAsync(domainEvent, CancellationToken.None);

        createClient.Captured.Action.Should().Be("Added");
        createClient.Captured.RealActorEmail.Should().Be("actor@test.com");
    }

    [Test]
    public void CustomRowType_ExtraProperties_RoundTripThroughBsonSerialization()
    {
        // The Mongo project's conventions (camelCase element names, string-typed Guid
        // serialization, etc.) are registered process-wide, mirroring how the real
        // Mongo bootstrapper configures the driver before any collection is used.
        new MongoDbConfigurator().Configure();

        var row = new MongoServiceCustomRow
        {
            Id = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            Action = "Added",
            RealActorEmail = "actor@test.com"
        };

        var document = row.ToBsonDocument();

        document.Contains("realActorEmail").Should().BeTrue();
        document["realActorEmail"].AsString.Should().Be("actor@test.com");

        var roundTripped = BsonSerializer.Deserialize<MongoServiceCustomRow>(document);
        roundTripped.RealActorEmail.Should().Be("actor@test.com");
    }
}
