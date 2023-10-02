// Copyright (c) Sundry OSS. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;

namespace Sundry.Extensions.Http.Polly.DependencyInjection;

/// <summary>
/// Contains opinionated convenience methods for configuring policies to handle conditions typically representing transient faults when making <see cref="HttpClient"/> requests.
/// </summary>
public static class PollyHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with the provided
    /// <see cref="ResiliencePipeline{HttpResponseMessage}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="resiliencePipeline">The <see cref="ResiliencePipeline{HttpResponseMessage}"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddResiliencePipelineHandler(this IHttpClientBuilder builder, ResiliencePipeline<HttpResponseMessage> resiliencePipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(resiliencePipeline);
        builder.AddHttpMessageHandler(() => new PollyStrategyHttpMessageHandler(resiliencePipeline));
        return builder;
    }
    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with a resilience pipeline returned
    /// by the <paramref name="policySelector"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policySelector">
    /// Selects an <see cref="ResiliencePipeline{HttpResponseMessage}"/> to apply to the current request.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddResiliencePipelineHandler(
        this IHttpClientBuilder builder,
        Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> policySelector)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policySelector);
        builder.AddHttpMessageHandler(() => new PollyStrategyHttpMessageHandler(policySelector));
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with a resilience pipeline returned
    /// by the <paramref name="policySelector"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policySelector">
    /// Selects an <see cref="ResiliencePipeline{HttpResponseMessage}"/> to apply to the current request.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddResiliencePipelineHandler(
        this IHttpClientBuilder builder,
        Func<IServiceProvider, HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> policySelector)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policySelector);

        builder.AddHttpMessageHandler((services) =>
        {
            return new PollyStrategyHttpMessageHandler((request) => policySelector(services, request));
        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with a resilience pipeline returned
    /// by executing provided key selection logic <paramref name="keySelector"/> and <paramref name="policyFactory"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policyFactory">Selects an <see cref="ResiliencePipeline{HttpResponseMessage}"/> to apply to the current request based on key selection.</param>
    /// <param name="keySelector">A delegate used to generate a resilience pipeline key based on the <see cref="HttpRequestMessage"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// Key generated by <paramref name="policyFactory"/> is first used to lookup existing policies from IPolicyRegistry. If resilience pipeline does not exist in the registry, create a new resilience pipeline with <paramref name="policyFactory"/> and add it in IPolicyRegistry.
    /// </para>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddResiliencePipelineHandler(this IHttpClientBuilder builder, Func<IServiceProvider, HttpRequestMessage, string, ResiliencePipeline<HttpResponseMessage>> policyFactory, Func<HttpRequestMessage, string> keySelector)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(policyFactory);

        builder.AddHttpMessageHandler((services) =>
        {
            var registry = services.GetRequiredService<ResiliencePipelineRegistry<string>>();
            return new PollyStrategyHttpMessageHandler((request) =>
            {
                var key = keySelector(request);

                if (registry.TryGetPipeline<HttpResponseMessage>(key, out var pipeline))
                {
                    return pipeline;
                }

                var newPolicy = policyFactory(services, request, key);
                registry.TryAddBuilder<HttpResponseMessage>(key, (builder, context) => builder.AddPipeline(newPolicy));
                return newPolicy;
            });
        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with a resilience pipeline returned
    /// by the <see cref="IReadOnlyPolicyRegistry{String}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policyKey">
    /// The key used to resolve a resilience pipeline from the <see cref="IReadOnlyPolicyRegistry{String}"/>.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddResiliencePipelineHandlerFromRegistry(this IHttpClientBuilder builder, string policyKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policyKey);

        builder.AddHttpMessageHandler((services) =>
        {
            var registry = services.GetRequiredService<ResiliencePipelineRegistry<string>>();

            var resiliencePipeline = registry.TryGetPipeline<HttpResponseMessage>(policyKey, out var pipeline)
                ? pipeline
                : throw new InvalidOperationException($"No resilience pipeline found with the name '{policyKey}'.");

            return new PollyStrategyHttpMessageHandler(resiliencePipeline);

        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with a resilience pipeline returned
    /// by the <see cref="IReadOnlyPolicyRegistry{String}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="policySelector">
    /// Selects an <see cref="ResiliencePipeline{HttpResponseMessage}"/> to apply to the current request.
    /// </param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddResiliencePipelineHandlerFromRegistry(
        this IHttpClientBuilder builder,
        Func<ResiliencePipelineRegistry<string>, HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> policySelector)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policySelector);

        builder.AddHttpMessageHandler((services) =>
        {
            var registry = services.GetRequiredService<ResiliencePipelineRegistry<string>>();
            return new PollyStrategyHttpMessageHandler((request) => policySelector(registry, request));
        });
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="PollyStrategyHttpMessageHandler"/> which will surround request execution with a <see cref="ResiliencePipeline"/>
    /// created by executing the provided configuration delegate. The resilience pipeline builder will be preconfigured to trigger
    /// application of the resilience pipeline for requests that fail with conditions that indicate a transient failure.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="configureresiliencePipeline">A delegate used to create a <see cref="ResiliencePipeline{HttpResponseMessage}"/>.</param>
    /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
    /// <remarks>
    /// <para>
    /// See the remarks on <see cref="PollyStrategyHttpMessageHandler"/> for guidance on configuring policies.
    /// </para>
    /// <para>
    /// The <see cref="PredicateBuilder{HttpResponseMessage}"/> provided to <paramref name="configureresiliencePipeline"/> has been
    /// preconfigured errors to handle errors in the following categories:
    /// <list type="bullet">
    /// <item><description>Network failures (as <see cref="HttpRequestException"/>)</description></item>
    /// <item><description>HTTP 5XX status codes (server errors)</description></item>
    /// <item><description>HTTP 408 status code (request timeout)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The resilience pipeline created by <paramref name="configureresiliencePipeline"/> will be cached indefinitely per named client. Policies
    /// are generally designed to act as singletons, and can be shared when appropriate. To share a resilience pipeline across multiple
    /// named clients, first create the resilience pipeline and then pass it to multiple calls to
    /// <see cref="AddResiliencePipelineHandler(IHttpClientBuilder, ResiliencePipeline{HttpResponseMessage})"/> as desired.
    /// </para>
    /// </remarks>
    public static IHttpClientBuilder AddTransientHttpErrorResiliencePipeline(
        this IHttpClientBuilder builder,
        Func<PredicateBuilder<HttpResponseMessage>, ResiliencePipeline<HttpResponseMessage>> configureresiliencePipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureresiliencePipeline);

        var predicateBuilder = HttpPolicyExtensions.HandleTransientHttpError();

        // Important - cache resilience pipeline instances so that they are singletons per handler.
        var resiliencePipeline = configureresiliencePipeline(predicateBuilder);

        builder.AddHttpMessageHandler(() => new PollyStrategyHttpMessageHandler(resiliencePipeline));
        return builder;
    }
}