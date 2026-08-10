using Firebend.AutoCrud.Core.Models.DomainEvents;

namespace Firebend.AutoCrud.ChangeTracking.Interfaces;

/// <summary>
/// Implemented by a custom change-tracking row type to populate its own additional
/// properties from the domain event context, immediately before the row is saved.
/// </summary>
public interface IAuditContextProperties
{
    public void PopulateFrom(DomainEventContext context);
}
