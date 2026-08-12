using System;
using Firebend.AutoCrud.ChangeTracking.EntityFramework;
using Firebend.AutoCrud.ChangeTracking.EntityFramework.Implementations;
using Firebend.AutoCrud.ChangeTracking.Interfaces;
using Firebend.AutoCrud.ChangeTracking.Models;
using Firebend.AutoCrud.Core.Configurators;
using Firebend.AutoCrud.Core.Interfaces.Models;
using Firebend.AutoCrud.Core.Models.DomainEvents;
using Firebend.AutoCrud.EntityFramework;
using Firebend.AutoCrud.EntityFramework.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Firebend.AutoCrud.Tests.ChangeTracking.EntityFramework;

public class ExtensionsTestEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
}

public class ExtensionsTestDbContext(DbContextOptions<ExtensionsTestDbContext> options) : AbstractDbContext(options);

public class ExtensionsCustomRow : ChangeTrackingEntity<Guid, ExtensionsTestEntity>, IAuditContextProperties
{
    public string RealActorEmail { get; set; }
    public void PopulateFrom(DomainEventContext context) => RealActorEmail = context?.UserEmail;
}

[TestFixture]
public class ExtensionsTests
{
    private static DomainEventsConfigurator<EntityFrameworkEntityBuilder<Guid, ExtensionsTestEntity>, Guid, ExtensionsTestEntity> BuildConfigurator()
    {
        var builder = new EntityFrameworkEntityBuilder<Guid, ExtensionsTestEntity>(
            new ServiceCollection(),
            typeof(ExtensionsTestDbContext),
            (_, options) => options.UseSqlServer("Data Source=.;Initial Catalog=Ignored;Integrated Security=True;"),
            false);

        return new DomainEventsConfigurator<EntityFrameworkEntityBuilder<Guid, ExtensionsTestEntity>, Guid, ExtensionsTestEntity>(builder);
    }

    [Test]
    public void WithEfChangeTracking_DefaultOverload_RegistersReadService()
    {
        var configurator = BuildConfigurator();

        configurator.WithEfChangeTracking();

        configurator.Builder.Registrations.Should().ContainKey(typeof(IChangeTrackingReadService<Guid, ExtensionsTestEntity>));
        configurator.Builder.Registrations.Should().ContainKey(typeof(IChangeTrackingService<Guid, ExtensionsTestEntity>));
    }

    [Test]
    public void WithEfChangeTracking_CustomRowTypeOverload_RegistersWritePath_ButNotReadService()
    {
        var configurator = BuildConfigurator();

        configurator.WithEfChangeTracking<EntityFrameworkEntityBuilder<Guid, ExtensionsTestEntity>, Guid, ExtensionsTestEntity, ExtensionsCustomRow>();

        configurator.Builder.Registrations.Should().ContainKey(typeof(IChangeTrackingService<Guid, ExtensionsTestEntity>));
        configurator.Builder.Registrations.Should().NotContainKey(typeof(IChangeTrackingReadService<Guid, ExtensionsTestEntity>));
    }

    [Test]
    public void WithEfChangeTracking_CustomRowTypeOverload_RegistersTier2ReadService()
    {
        var configurator = BuildConfigurator();

        configurator.WithEfChangeTracking<EntityFrameworkEntityBuilder<Guid, ExtensionsTestEntity>, Guid, ExtensionsTestEntity, ExtensionsCustomRow>();

        configurator.Builder.Registrations.Should()
            .ContainKey(typeof(IChangeTrackingReadService<Guid, ExtensionsTestEntity, ExtensionsCustomRow>));
    }

    [Test]
    public void WithEfChangeTracking_DefaultOverload_RegistersTier2ReadServiceToo()
    {
        var configurator = BuildConfigurator();

        configurator.WithEfChangeTracking();

        configurator.Builder.Registrations.Should()
            .ContainKey(typeof(IChangeTrackingReadService<Guid, ExtensionsTestEntity, ChangeTrackingEntity<Guid, ExtensionsTestEntity>>));
    }
}
