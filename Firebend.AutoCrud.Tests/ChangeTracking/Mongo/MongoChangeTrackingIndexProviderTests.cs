using System;
using System.Linq;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.ChangeTracking.Mongo.Implementations;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Mongo.Interfaces;
using Firebend.AutoCrud.Mongo.Models;
using FluentAssertions;
using MongoDB.Driver;
using NUnit.Framework;

namespace Firebend.AutoCrud.Tests.ChangeTracking.Mongo;

public class IndexProviderTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
}

public class IndexProviderCustomRow : ChangeTrackingEntity<Guid, IndexProviderTestEntity>
{
    public string RealActorEmail { get; set; }
}

public class FakeIndexConfiguration<TChangeTrackingEntity> : IMongoEntityIndexConfiguration<Guid, TChangeTrackingEntity>
    where TChangeTrackingEntity : IEntity<Guid>
{
    public string CollectionName => "IndexProviderTestEntity_ChangeTracking";
    public string DatabaseName => "TestDb";
    public AggregateOptions AggregateOption { get; set; } = new();
    public MongoTenantShardMode ShardMode => MongoTenantShardMode.Unknown;
    public string Locale => null;
    public string ShardKey => null;
}

[TestFixture]
public class MongoChangeTrackingIndexProviderTests
{
    [Test]
    public void GetIndexes_ForCustomRowType_ReturnsEntityIdTextAndModifiedIndexes()
    {
        var sut = new MongoChangeTrackingIndexProvider<Guid, IndexProviderTestEntity, IndexProviderCustomRow>();
        var builder = new IndexKeysDefinitionBuilder<IndexProviderCustomRow>();

        var indexes = sut.GetIndexes(builder, new FakeIndexConfiguration<IndexProviderCustomRow>()).ToList();

        indexes.Should().HaveCount(3);
        indexes.Select(x => x.Options.Name).Should().Contain(new[] { "changeTrackingEntityId", "text", "modified" });
    }
}
