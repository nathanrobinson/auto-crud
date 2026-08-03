using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Extensions;
using Firebend.AutoCrud.ChangeTracking.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Models.DomainEvents;
using Firebend.AutoCrud.EntityFramework.Client;
using Microsoft.AspNetCore.JsonPatch.Operations;

namespace Firebend.AutoCrud.ChangeTracking.EntityFramework.Implementations;

public class EntityFrameworkChangeTrackingService<TEntityKey, TEntity, TChangeTrackingEntity> :
    EntityFrameworkCreateClient<Guid, TChangeTrackingEntity>,
    IChangeTrackingService<TEntityKey, TEntity>
    where TEntityKey : struct
    where TEntity : class, IEntity<TEntityKey>
    where TChangeTrackingEntity : ChangeTrackingEntity<TEntityKey, TEntity>, new()
{
    public EntityFrameworkChangeTrackingService(
        IChangeTrackingDbContextProvider<TEntityKey, TEntity, TChangeTrackingEntity> provider) :
        base(provider, null, null)
    {
    }

    public Task TrackAddedAsync(EntityAddedDomainEvent<TEntity> domainEvent, CancellationToken cancellationToken)
        => AddAsync(
            GetChangeTrackingEntityBase(domainEvent,
                "Added",
                domainEvent.Entity,
                domainEvent.Entity.Id),
            cancellationToken);

    public Task TrackDeleteAsync(EntityDeletedDomainEvent<TEntity> domainEvent, CancellationToken cancellationToken)
        => AddAsync(
            GetChangeTrackingEntityBase(domainEvent,
                "Delete",
                domainEvent.Entity,
                domainEvent.Entity.Id),
            cancellationToken);

    public Task TrackUpdateAsync(EntityUpdatedDomainEvent<TEntity> domainEvent, CancellationToken cancellationToken)
        => AddAsync(
            GetChangeTrackingEntityBase(domainEvent,
                "Update",
                domainEvent.Previous,
                domainEvent.Previous.Id,
                domainEvent.Operations),
            cancellationToken);

    private static TChangeTrackingEntity GetChangeTrackingEntityBase(DomainEventBase domainEvent,
        string action,
        TEntity entity,
        TEntityKey id,
        List<Operation<TEntity>> operations = null)
    {
        var changeEntity = new TChangeTrackingEntity
        {
            ModifiedDate = domainEvent.Time,
            Source = domainEvent.EventContext?.Source,
            UserEmail = domainEvent.EventContext?.UserEmail,
            Action = action,
            Changes = operations.ToNonGenericOperations(),
            Entity = entity,
            EntityId = id,
            DomainEventCustomContext = domainEvent.EventContext?.CustomContext
        };

        if (changeEntity is IAuditContextProperties auditRow)
        {
            auditRow.PopulateFrom(domainEvent.EventContext);
        }

        return changeEntity;
    }
}
