// Copyright (c) Sundry OSS. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Polly;

namespace Sundry.Extensions.Http.Polly;

public class PollyStrategyHttpMessageHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;
    private readonly Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> _resiliencePipelineSelector;

    /// <summary>
    /// Creates a new <see cref="PollyStrategyHttpMessageHandler"/>.
    /// </summary>
    /// <param name="resiliencePipeline">The Resilience Pipeline.</param>
    public PollyStrategyHttpMessageHandler(ResiliencePipeline<HttpResponseMessage> resiliencePipeline)
    {
        _resiliencePipeline = resiliencePipeline ?? throw new ArgumentNullException(nameof(resiliencePipeline));
        _resiliencePipelineSelector = default!;
    }

    /// <summary>
    /// Creates a new <see cref="PollyStrategyHttpMessageHandler"/>.
    /// </summary>
    /// <param name="resiliencePipelineSelector">A function which can select the desired resilience pipeline for a given <see cref="HttpRequestMessage"/>.</param>
    public PollyStrategyHttpMessageHandler(Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> resiliencePipelineSelector)
    {
        _resiliencePipelineSelector = resiliencePipelineSelector ?? throw new ArgumentNullException(nameof(resiliencePipelineSelector));
        _resiliencePipeline = default!;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Guarantee the existence of a context for every resilience pipeline execution, but only create a new one if needed. This
        // allows later handlers to flow state if desired.
        var cleanUpContext = false;
        var context = request.GetPolicyExecutionContext();
        if (context == null)
        {
            context = ResilienceContextPool.Shared.Get(cancellationToken);
            request.SetPolicyExecutionContext(context);
            cleanUpContext = true;
        }

        HttpResponseMessage response;
        try
        {
            var resiliencePipeline = _resiliencePipeline ?? SelectPolicy(request);
            response = await resiliencePipeline.ExecuteAsync((ctx) => SendCoreAsync(request, ctx.CancellationToken), context).ConfigureAwait(false);
        }
        finally
        {
            if (cleanUpContext)
            {
                request.SetPolicyExecutionContext(null);
                ResilienceContextPool.Shared.Return(context);
            }
        }

        return response;
    }

    /// <summary>
    /// Called inside the execution of the <see cref="resiliencePipeline"/> to perform request processing.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>Returns a <see cref="Task{HttpResponseMessage}"/> that will yield a response when completed.</returns>
    protected virtual async ValueTask<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(request);

        var result = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private ResiliencePipeline<HttpResponseMessage> SelectPolicy(HttpRequestMessage request)
    {
        var resiliencePipeline = _resiliencePipelineSelector(request);
        if (resiliencePipeline == null)
        {
            var message = $"The resilience pipeline selector returned a null resiliencePipeline for the request '{request.Method} {request.RequestUri}'.";
            throw new InvalidOperationException(message);
        }
        return resiliencePipeline;
    }
}