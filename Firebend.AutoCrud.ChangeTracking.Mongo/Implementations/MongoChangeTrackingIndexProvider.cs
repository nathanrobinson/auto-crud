using System;
using System.Collections.Generic;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Mongo.Helpers;
using Firebend.AutoCrud.Mongo.Interfaces;
using MongoDB.Driver;

namespace Firebend.AutoCrud.ChangeTracking.Mongo.Implementations;

public class MongoChangeTrackingIndexProvider<TEntityKey, TEntity, TChangeTrackingEntity> :
    IMongoIndexProvider<Guid, TChangeTrackingEntity>
    where TEntityKey : struct
    where TEntity : class, IEntity<TEntityKey>
    where TChangeTrackingEntity : ChangeTrackingEntity<TEntityKey, TEntity>
{
    public IEnumerable<CreateIndexModel<TChangeTrackingEntity>> GetIndexes(
        IndexKeysDefinitionBuilder<TChangeTrackingEntity> builder,
        IMongoEntityIndexConfiguration<Guid, TChangeTrackingEntity> configuration)
    {
        yield return new CreateIndexModel<TChangeTrackingEntity>(
            builder.Ascending(f => f.EntityId),
            new CreateIndexOptions { Name = "changeTrackingEntityId" });

        yield return MongoIndexProviderHelpers.FullText(builder);

        yield return MongoIndexProviderHelpers.DateTimeOffset(builder, configuration.Locale);
    }
}
