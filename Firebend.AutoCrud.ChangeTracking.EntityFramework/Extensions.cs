using System;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.DbContexts;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Implementations;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Interfaces;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Searching;
using Firebend.AutoCrud.ChangeTracking.Implementations;
using Firebend.AutoCrud.ChangeTracking.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Abstractions.Builders;
using Firebend.AutoCrud.Core.Configurators;
using Firebend.AutoCrud.Core.Implementations.Defaults;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Interfaces.Services.Entities;
using Firebend.AutoCrud.EntityFramework;
using Firebend.AutoCrud.EntityFramework.Client;
using Firebend.AutoCrud.EntityFramework.Including;
using Firebend.AutoCrud.EntityFramework.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Firebend.AutoCrud.ChangeTracking.EntityFramework;

public static class Extensions
{
    /// <summary>
    /// Adds change tracking for a given entity and persists it to a data store using Entity Framework,
    /// using the default <see cref="ChangeTrackingEntity{TKey,TEntity}"/> row type.
    /// This function registers a <see cref="EntityFrameworkChangeTrackingService{TEntityKey,TEntity,TChangeTrackingEntity}"/> to track changes and
    /// a <see cref="EntityFrameworkChangeTrackingReadService{TEntityKey,TEntity}"/> to read changes.
    /// It also registers <see cref="ChangeTrackingAddedDomainEventHandler{TKey,TEntity}"/>, <see cref="ChangeTrackingUpdatedDomainEventHandler{TKey,TEntity}"/>,
    /// and <see cref="ChangeTrackingDeleteDomainEventHandler{TKey,TEntity}"/> to hook into the domain event pipeline and persist the changes.
    /// A <see cref="ChangeTrackingDbContextProvider{TEntityKey,TEntity,TChangeTrackingEntity}"/> is registered so that a special change tracking Entity Framework context
    /// can be used to persist the changes.
    /// </summary>
    /// <param name="configurator">
    /// The <see cref="DomainEventsConfigurator{TBuilder,TKey,TEntity}"/> to configure Entity Framework persistence for.
    /// </param>
    /// <typeparam name="TBuilder">
    /// The type of <see cref="EntityCrudBuilder{TKey,TEntity}"/> builder. Must inherit <see cref="EntityFrameworkEntityBuilder{TKey,TEntity}"/>
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of key the entity uses.
    /// </typeparam>
    /// <typeparam name="TEntity">
    /// The type of entity.
    /// </typeparam>
    /// <returns>
    /// A <see cref="DomainEventsConfigurator{TBuilder,TKey,TEntity}"/>
    /// </returns>
    /// <exception cref="Exception">
    /// Throws an exception if <paramref name="configurator"/> does not implement <see cref="EntityFrameworkEntityBuilder{TKey,TEntity}"/>
    /// </exception>
    public static DomainEventsConfigurator<TBuilder, TKey, TEntity> WithEfChangeTracking<TBuilder, TKey, TEntity>(
        this DomainEventsConfigurator<TBuilder, TKey, TEntity> configurator
        )
        where TKey : struct
        where TEntity : class, IEntity<TKey>, new()
        where TBuilder : EntityCrudBuilder<TKey, TEntity>
        => configurator.WithEfChangeTracking<TBuilder, TKey, TEntity, ChangeTrackingEntity<TKey, TEntity>>();

    /// <summary>
    /// Adds change tracking for a given entity and persists it to a data store using Entity Framework,
    /// using a custom <typeparamref name="TChangeTrackingEntity"/> row type. Implement
    /// <see cref="IAuditContextProperties"/> on <typeparamref name="TChangeTrackingEntity"/> to populate
    /// its own additional columns from the domain event context.
    /// This function registers a <see cref="EntityFrameworkChangeTrackingService{TEntityKey,TEntity,TChangeTrackingEntity}"/> to track changes and,
    /// when <typeparamref name="TChangeTrackingEntity"/> is the default <see cref="ChangeTrackingEntity{TKey,TEntity}"/> row type,
    /// a <see cref="EntityFrameworkChangeTrackingReadService{TEntityKey,TEntity}"/> to read changes.
    /// It always also registers a <see cref="EntityFrameworkChangeTrackingReadService{TEntityKey,TEntity,TChangeTrackingEntity}"/>
    /// keyed on <typeparamref name="TChangeTrackingEntity"/>, so a consumer using a custom row type can still read
    /// changes back (including its extra columns) via <c>WithChangeTrackingControllers</c> once it is registered
    /// for that row type.
    /// It also registers <see cref="ChangeTrackingAddedDomainEventHandler{TKey,TEntity}"/>, <see cref="ChangeTrackingUpdatedDomainEventHandler{TKey,TEntity}"/>,
    /// and <see cref="ChangeTrackingDeleteDomainEventHandler{TKey,TEntity}"/> to hook into the domain event pipeline and persist the changes.
    /// A <see cref="ChangeTrackingDbContextProvider{TEntityKey,TEntity,TChangeTrackingEntity}"/> is registered so that a special change tracking Entity Framework context
    /// can be used to persist the changes.
    /// </summary>
    /// <param name="configurator">
    /// The <see cref="DomainEventsConfigurator{TBuilder,TKey,TEntity}"/> to configure Entity Framework persistence for.
    /// </param>
    /// <typeparam name="TBuilder">
    /// The type of <see cref="EntityCrudBuilder{TKey,TEntity}"/> builder. Must inherit <see cref="EntityFrameworkEntityBuilder{TKey,TEntity}"/>
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of key the entity uses.
    /// </typeparam>
    /// <typeparam name="TEntity">
    /// The type of entity.
    /// </typeparam>
    /// <typeparam name="TChangeTrackingEntity">
    /// The type of row persisted for each change. Must inherit <see cref="ChangeTrackingEntity{TKey,TEntity}"/>. Implement
    /// <see cref="IAuditContextProperties"/> on this type to populate its own additional columns from the domain event context.
    /// </typeparam>
    /// <returns>
    /// A <see cref="DomainEventsConfigurator{TBuilder,TKey,TEntity}"/>
    /// </returns>
    /// <exception cref="Exception">
    /// Throws an exception if <paramref name="configurator"/> does not implement <see cref="EntityFrameworkEntityBuilder{TKey,TEntity}"/>,
    /// or if the builder's db context has not been configured yet.
    /// </exception>
    public static DomainEventsConfigurator<TBuilder, TKey, TEntity> WithEfChangeTracking<TBuilder, TKey, TEntity, TChangeTrackingEntity>(
        this DomainEventsConfigurator<TBuilder, TKey, TEntity> configurator
        )
        where TKey : struct
        where TEntity : class, IEntity<TKey>, new()
        where TBuilder : EntityCrudBuilder<TKey, TEntity>
        where TChangeTrackingEntity : ChangeTrackingEntity<TKey, TEntity>, new()
    {
        if (configurator.Builder is not EntityFrameworkEntityBuilder<TKey, TEntity> efBuilder)
        {
            throw new Exception($"Configuration Error! This builder is not a {nameof(EntityFrameworkEntityBuilder<Guid, FooEntity>)}");
        }

        if (efBuilder.DbContextType is null)
        {
            throw new Exception("Please configure the builder's db context first.");
        }

        if (efBuilder.UsePooled)
        {
            efBuilder.Services.AddPooledDbContextFactory<ChangeTrackingDbContext<TKey, TEntity, TChangeTrackingEntity>>(efBuilder.DbContextOptionsBuilder);
        }
        else
        {
            efBuilder.Services.AddDbContextFactory<ChangeTrackingDbContext<TKey, TEntity, TChangeTrackingEntity>>(efBuilder.DbContextOptionsBuilder);
        }

        var providerType =
            typeof(ChangeTrackingTableNameProvider<,,>).MakeGenericType(efBuilder.EntityKeyType, efBuilder.EntityType, efBuilder.DbContextType);
        configurator.Builder.WithRegistration<IChangeTrackingTableNameProvider<TKey, TEntity>>(providerType, serviceLifetime: ServiceLifetime.Singleton);

        configurator.Builder.WithRegistration<IDbContextProvider<Guid, TChangeTrackingEntity>, ChangeTrackingDbContextProvider<TKey, TEntity, TChangeTrackingEntity>>();
        configurator.Builder.WithRegistration<IChangeTrackingDbContextProvider<TKey, TEntity, TChangeTrackingEntity>, ChangeTrackingDbContextProvider<TKey, TEntity, TChangeTrackingEntity>>();

        configurator.Builder.WithRegistration<IChangeTrackingService<TKey, TEntity>,
            EntityFrameworkChangeTrackingService<TKey, TEntity, TChangeTrackingEntity>>();

        // The old 2-arg IChangeTrackingReadService<TKey,TEntity> interface predates custom row
        // types and can only be wired up when the row type in use actually is the base
        // ChangeTrackingEntity<TKey,TEntity> type: EF Core does not expose a custom row type's CLR
        // base class as a separately queryable entity type once only the derived type is mapped, so
        // this interface would fail at query time against a custom TChangeTrackingEntity. Its own
        // dependencies (query client, order-by, search handler, includes provider) are NOT registered
        // here - the unconditional block below always supplies them, keyed on TChangeTrackingEntity,
        // and when TChangeTrackingEntity is this same default type, that's exactly what this
        // registration needs too, so registering them twice would be redundant.
        if (typeof(TChangeTrackingEntity) == typeof(ChangeTrackingEntity<TKey, TEntity>))
        {
            configurator.Builder.WithRegistration<IChangeTrackingReadService<TKey, TEntity>,
                EntityFrameworkChangeTrackingReadService<TKey, TEntity>>();
        }

        // Registers the tier-2/3 read path - the 3-arg IChangeTrackingReadService and everything it
        // depends on (query client, order-by, search handler, includes provider) - keyed on
        // TChangeTrackingEntity itself, so it works for both the default and any custom row type.
        // When TChangeTrackingEntity is the default ChangeTrackingEntity<TKey,TEntity>, these same
        // registrations also satisfy the old 2-arg service registered above.
        configurator.Builder.WithRegistration<IChangeTrackingReadService<TKey, TEntity, TChangeTrackingEntity>,
            EntityFrameworkChangeTrackingReadService<TKey, TEntity, TChangeTrackingEntity>>();

        configurator.Builder.WithRegistration<IEntityFrameworkQueryClient<Guid, TChangeTrackingEntity>,
            EntityFrameworkQueryClient<Guid, TChangeTrackingEntity>>();

        configurator.Builder.WithRegistration<IDefaultEntityOrderByProvider<Guid, TChangeTrackingEntity>,
            DefaultEntityOrderByProviderModified<Guid, TChangeTrackingEntity>>();

        configurator.Builder.WithRegistration<IEntityQueryOrderByHandler<Guid, TChangeTrackingEntity>,
            DefaultEntityQueryOrderByHandler<Guid, TChangeTrackingEntity>>();

        configurator.Builder.WithRegistration<IEntitySearchHandler<Guid, TChangeTrackingEntity, ChangeTrackingSearchRequest<TKey>>,
            EntityFrameworkChangeTrackingSearchHandler<TKey, TEntity, TChangeTrackingEntity>>();

        configurator.Builder.WithRegistration<IEntityFrameworkIncludesProvider<Guid, TChangeTrackingEntity>,
            DefaultEntityFrameworkIncludesProvider<Guid, TChangeTrackingEntity>>();

        configurator.WithDomainEventEntityAddedSubscriber<ChangeTrackingAddedDomainEventHandler<TKey, TEntity>>();
        configurator.WithDomainEventEntityUpdatedSubscriber<ChangeTrackingUpdatedDomainEventHandler<TKey, TEntity>>();
        configurator.WithDomainEventEntityDeletedSubscriber<ChangeTrackingDeleteDomainEventHandler<TKey, TEntity>>();

        return configurator;
    }
}
