using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Capstone.Configuration;
using Capstone.EntityFrameworkCore;
using Capstone.EntityFrameworkCore.Seed;
using Capstone.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;
using System;

namespace Capstone.Migrator;

[DependsOn(typeof(CapstoneEntityFrameworkModule))]
public class CapstoneMigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public CapstoneMigratorModule(CapstoneEntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(CapstoneMigratorModule).GetAssembly().GetDirectoryPathOrNull(),
            addUserSecrets: true
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            CapstoneConsts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        SeedOptions.SeedSimpleSampleData = Program.SampleData
            || (bool.TryParse(_appConfiguration["Seed:SimpleSampleData"], out var simpleData)
                ? simpleData
                : string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase));
        SeedOptions.SeedLogbookSampleData = Program.LogbookData
            || (bool.TryParse(_appConfiguration["Seed:LogbookSampleData"], out var logbookData) && logbookData);
        SeedOptions.SystemAdminInitialPassword = _appConfiguration["SystemAdmin:InitialPassword"];
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(CapstoneMigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
