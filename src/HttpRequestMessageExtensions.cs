// Copyright (c) Sundry OSS. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Polly;

namespace Sundry.Extensions.Http.Polly;

/// <summary>
/// Extension methods for <see cref="HttpRequestMessage"/> Polly integration.
/// </summary>
public static class HttpRequestMessageExtensions
{
#pragma warning disable CA1802 //  Use literals where appropriate. Using a static field for reference equality
    internal static readonly HttpRequestOptionsKey<ResilienceContext?> PolicyExecutionContextKey = new("PolicyExecutionContextKey");
#pragma warning restore CA1802

    /// <summary>
    /// Gets the <see cref="ResilienceContext"/> associated with the provided <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <returns>The <see cref="ResilienceContext"/> if set, otherwise <c>null</c>.</returns>
    /// <remarks>
    /// The <see cref="PollyStrategyHttpMessageHandler"/> will attach a ResilienceContext to the <see cref="HttpResponseMessage"/> prior
    /// to executing a <see cref="ResiliencePipeline{HttpResponseMessage}"/>, if one does not already exist. The <see cref="ResilienceContext"/> will be provided
    /// to the resilience pipeline for use inside the <see cref="ResiliencePipeline{HttpResponseMessage}"/> and in other message handlers.
    /// </remarks>
    public static ResilienceContext? GetPolicyExecutionContext(this HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Options.TryGetValue(PolicyExecutionContextKey, out var resilienceContext);
        return resilienceContext;
    }

    /// <summary>
    /// Sets the <see cref="ResilienceContext"/> associated with the provided <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="resilienceContext">The <see cref="ResilienceContext"/>, may be <c>null</c>.</param>
    /// <remarks>
    /// The <see cref="PollyStrategyHttpMessageHandler"/> will attach a ResilienceContext to the <see cref="HttpResponseMessage"/> prior
    /// to executing a <see cref="ResiliencePipeline{HttpResponseMessage}"/>, if one does not already exist. The <see cref="ResilienceContext"/> will be provided
    /// to the resilience pipeline for use inside the <see cref="ResiliencePipeline{HttpResponseMessage}"/> and in other message handlers.
    /// </remarks>
    public static void SetPolicyExecutionContext(this HttpRequestMessage request, ResilienceContext? resilienceContext)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Options.Set(PolicyExecutionContextKey, resilienceContext);
    }
}
