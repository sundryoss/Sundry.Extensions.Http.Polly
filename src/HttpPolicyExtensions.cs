// Copyright (c) Sundry OSS. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Polly;

namespace Sundry.Extensions.Http.Polly;

/// <summary>
/// Contains opinionated convenience methods for configuring policies to handle conditions typically representing transient faults when making <see cref="HttpClient"/> requests.
/// </summary>
public static class HttpPolicyExtensions
{
    /// <summary>
    /// Builds a <see cref="PredicateBuilder{HttpResponseMessage}"/> to configure a <see cref="ResiliencePipeline{HttpResponseMessage}"/> which will handle <see cref="HttpClient"/> requests that fail with conditions indicating a transient failure. 
    /// <para>The conditions configured to be handled are:
    /// <list type="bullet">
    /// <item><description>Network failures (as <see cref="HttpRequestException"/>)</description></item>
    /// <item><description>HTTP 5XX status codes (server errors)</description></item>
    /// <item><description>HTTP 408 status code (request timeout)</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <returns>The <see cref="PredicateBuilder{HttpResponseMessage}"/> pre-configured to handle <see cref="HttpClient"/> requests that fail with conditions indicating a transient failure. </returns>
    public static PredicateBuilder<HttpResponseMessage> HandleTransientHttpError()
    {
        return new PredicateBuilder<HttpResponseMessage>().Handle<HttpRequestException>().HandleTransientHttpStatusCode();
    }

    /// <summary>
    /// Configures the <see cref="PredicateBuilder{HttpResponseMessage}"/> to handle <see cref="HttpClient"/> requests that fail with <see cref="HttpStatusCode"/>s indicating a transient failure. 
    /// <para>The <see cref="HttpStatusCode"/>s configured to be handled are:
    /// <list type="bullet">
    /// <item><description>HTTP 5XX status codes (server errors)</description></item>
    /// <item><description>HTTP 408 status code (request timeout)</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <returns>The <see cref="PredicateBuilder{HttpResponseMessage}"/> pre-configured to handle <see cref="HttpClient"/> requests that fail with <see cref="HttpStatusCode"/>s indicating a transient failure. </returns>
    public static PredicateBuilder<HttpResponseMessage> HandleTransientHttpStatusCode(this PredicateBuilder<HttpResponseMessage> PredicateBuilder)
    {
        if (PredicateBuilder == null)
        {
            throw new ArgumentNullException(nameof(PredicateBuilder));
        }

        return PredicateBuilder.HandleResult(TransientHttpStatusCodePredicate);
    }

    private static readonly Func<HttpResponseMessage, bool> TransientHttpStatusCodePredicate = (response) =>
   {
       return (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout;
   };

}
