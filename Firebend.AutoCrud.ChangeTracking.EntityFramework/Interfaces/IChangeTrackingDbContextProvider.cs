using System;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.EntityFramework.Interfaces;

namespace Firebend.AutoCrud.ChangeTracking.EntityFramework.Interfaces;

/// <summary>
/// Encapsulates logic for getting an Entity Framework context that will persist changes.
/// </summary>
/// <typeparam name="TEntityKey">
/// The type of key the entity uses.
/// </typeparam>
/// <typeparam name="TEntity">
/// The type of entity.
/// </typeparam>
/// <typeparam name="TChangeTrackingEntity">
/// The type of row persisted for each change.
/// </typeparam>
public interface IChangeTrackingDbContextProvider<TEntityKey, TEntity, TChangeTrackingEntity> :
    IDbContextProvider<Guid, TChangeTrackingEntity>
    where TEntityKey : struct
    where TEntity : class, IEntity<TEntityKey>
    where TChangeTrackingEntity : ChangeTrackingEntity<TEntityKey, TEntity>;
