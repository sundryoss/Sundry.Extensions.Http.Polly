# Sundry.Extensions.Http.Polly
The HttpClient factory is a pattern for configuring and retrieving named HttpClients in a composable way. This package integrates IHttpClientFactory with the Polly (v8.0) library, to add transient-fault-handling and resiliency through fluent policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback.
