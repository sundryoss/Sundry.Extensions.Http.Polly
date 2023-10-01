// Copyright (c) Sundry OSS. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly.Registry;

namespace Sundry.Extensions.Http.Polly;

/// <summary>
/// Provides convenience extension methods to register <see cref="ResiliencePipelineRegistry{String}"/> and
/// <see cref="ResiliencePipelineRegistry{String}"/> in the service collection.
/// </summary>
public static class PollyServiceCollectionExtensions
{
    /// <summary>
    /// Registers an empty <see cref="ResiliencePipelineRegistry{String}"/> in the service collection with service types
    /// <see cref="ResiliencePipelineRegistry{String}"/>,
    /// <see cref="ResiliencePipelineProvider{String}"/> if the service types haven't already been registered
    /// and returns the existing or newly created registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The existing or newly created <see cref="ResiliencePipelineRegistry{String}"/>.</returns>
    public static ResiliencePipelineRegistry<string> AddResiliencePipelineRegistry(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Get existing registry or an empty instance
        var registry = services.BuildServiceProvider().GetService<ResiliencePipelineRegistry<string>>() ?? new ResiliencePipelineRegistry<string>();

        // Try to register for the missing interfaces
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ResiliencePipelineProvider<string>>(registry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton(registry));

        return registry;
    }

    /// <summary>
    /// Registers the provided <see cref="ResiliencePipelineRegistry{String}"/> in the service collection with service types
    /// <see cref="ResiliencePipelineRegistry{String}"/>,
    /// <see cref="ResiliencePipelineProvider{String}"/> and returns the provided registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="registry">The <see cref="ResiliencePipelineRegistry{String}"/>.</param>
    /// <returns>The provided <see cref="ResiliencePipelineRegistry{String}"/>.</returns>
    public static ResiliencePipelineRegistry<string> AddResiliencePipelineRegistry(this IServiceCollection services, ResiliencePipelineRegistry<string> registry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registry);

        services.AddSingleton<ResiliencePipelineProvider<string>>(registry);
        services.AddSingleton(registry);

        return registry;
    }
    /// <summary>
    /// Registers an empty <see cref="ResiliencePipelineRegistry{String}"/> in the service collection with service types
    /// <see cref="ResiliencePipelineRegistry{String}"/>, 
    /// <see cref="ResiliencePipelineProvider{String}"/> and uses the specified delegate to configure it.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureRegistry">A delegate that is used to configure an <see cref="ResiliencePipelineRegistry{String}"/>.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddResiliencePipelineRegistry(this IServiceCollection services, Action<IServiceProvider, ResiliencePipelineRegistry<string>> configureRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureRegistry);
        
        // Create an empty registry, configure it and register it as an instance.
        // This is the best way to get a single instance registered using all the interfaces.
        services.AddSingleton(serviceProvider =>
        {
            var registry = new ResiliencePipelineRegistry<string>();
            configureRegistry(serviceProvider, registry);
            return registry;
        });

        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>());
        return services;
    }
}
