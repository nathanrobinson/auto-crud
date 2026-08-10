using System;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Abstractions.Builders;
using Firebend.AutoCrud.Core.Abstractions.Configurators;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Mongo.Client;
using Firebend.AutoCrud.Mongo.Client.Indexing;
using Firebend.AutoCrud.Mongo.Implementations;
using Firebend.AutoCrud.Mongo.Interfaces;

namespace Firebend.AutoCrud.ChangeTracking.Mongo;

public class MongoChangeTrackingConfigurator<TBuilder, TKey, TEntity, TChangeTrackingEntity> : EntityBuilderConfigurator<TBuilder, TKey, TEntity>
    where TBuilder : EntityCrudBuilder<TKey, TEntity>
    where TKey : struct
    where TEntity : class, IEntity<TKey>
    where TChangeTrackingEntity : ChangeTrackingEntity<TKey, TEntity>
{
    public MongoChangeTrackingConfigurator(TBuilder builder) : base(builder)
    {
    }

    public MongoChangeTrackingConfigurator<TBuilder, TKey, TEntity, TChangeTrackingEntity> WithConnectionStringProvider<TConnectionStringProvider>()
        where TConnectionStringProvider : class, IMongoConnectionStringProvider<Guid, TChangeTrackingEntity>
    {
        Builder.WithRegistration<IMongoConnectionStringProvider<Guid, TChangeTrackingEntity>, TConnectionStringProvider>();
        Builder.WithRegistration<IMongoClientFactory<Guid, TChangeTrackingEntity>,
            MongoClientFactory<Guid, TChangeTrackingEntity>>();
        Builder.WithRegistration<IMongoIndexMergeService<Guid, TChangeTrackingEntity>,
            MongoIndexMergeService<Guid, TChangeTrackingEntity>>();

        return this;
    }

    public MongoChangeTrackingConfigurator<TBuilder, TKey, TEntity, TChangeTrackingEntity> WithConnectionString(string connectionString)
    {
        Builder.WithRegistrationInstance<IMongoConnectionStringProvider<Guid, TChangeTrackingEntity>>(
            new StaticMongoConnectionStringProvider<Guid, TChangeTrackingEntity>(connectionString));
        Builder.WithRegistration<IMongoClientFactory<Guid, TChangeTrackingEntity>,
            MongoClientFactory<Guid, TChangeTrackingEntity>>();
        Builder.WithRegistration<IMongoIndexMergeService<Guid, TChangeTrackingEntity>,
            MongoIndexMergeService<Guid, TChangeTrackingEntity>>();

        return this;
    }
}
