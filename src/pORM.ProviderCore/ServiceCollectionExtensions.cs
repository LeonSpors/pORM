﻿using Microsoft.Extensions.DependencyInjection;
using pORM.Core.Caching;
using pORM.Core.Interfaces;
using pORm.Data;

namespace pORM.ProviderCore;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers base pORM services into the DI container.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <returns>The updated IServiceCollection.</returns>
    public static IServiceCollection AddDatabaseCore(this IServiceCollection services)
    {
        services.AddSingleton<IGlobalContext, GlobalContext>();
        services.AddSingleton<ITableCache, TableCache>();

        return services;
    }
}