using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Sundry.Extensions.Http.Polly;
using Sundry.Extensions.Http.Polly.Console.Services;
using Sundry.Extensions.Http.Polly.DependencyInjection;

namespace SystemUnderTest
{
    public static class Program
    {
        [ExcludeFromCodeCoverage]
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var weatherService = host.Services.GetRequiredService<IWeatherService>();
            for (int i = 0; i < 5; i++)
            {
                await GetWeatherforecast(weatherService);
            }

            await host.RunAsync();
        }
        [ExcludeFromCodeCoverage]
        static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureLogging((context, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddResiliencePipelineRegistry((_,y)=> y.TryAddBuilder<HttpResponseMessage>("my-key", (builder, _) => builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                   {
                       ShouldHandle = HttpPolicyExtensions.HandleTransientHttpError(),
                       Delay = TimeSpan.FromSeconds(1),
                       MaxRetryAttempts = 5,
                       UseJitter = true,
                       BackoffType = DelayBackoffType.Exponential,
                       OnRetry = (args) =>
                       {
                           Console.WriteLine($"Using TryAddBuilder:  Retry Attempt Number : {args.AttemptNumber} after {args.RetryDelay.TotalSeconds} seconds.");
                           return default;
                       }
                   })));

                    services
                    .AddHttpClient<IWeatherService, WeatherService>(client =>
                        {
                            client.BaseAddress = new Uri("http://localhost:5087/");
                        })
                      //  .AddResiliencePipelineHandler(PollyResilienceStrategy.Retry());
                    .AddResiliencePipelineHandlerFromRegistry("my-key");
                });
        }

        public static async Task GetWeatherforecast(IWeatherService weatherService)
        {
            var result = await weatherService.GetWeatherforecast();

            if (result)
            {
                Console.WriteLine("Weather forecast retrieved successfully");
            }
            else
            {
                Console.WriteLine("Weather forecast retrieval failed");
            }
        }
    }

    public static class PollyResilienceStrategy
    {
        public static ResiliencePipeline<HttpResponseMessage> Retry(PredicateBuilder<HttpResponseMessage> predicateBuilder)
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                   .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                   {
                       ShouldHandle = predicateBuilder,
                       Delay = TimeSpan.FromSeconds(1),
                       MaxRetryAttempts = 5,
                       UseJitter = true,
                       BackoffType = DelayBackoffType.Exponential,
                       OnRetry = (args) =>
                       {
                           Console.WriteLine($"Retry Attempt Number : {args.AttemptNumber} after {args.RetryDelay.TotalSeconds} seconds.");
                           return default;
                       }
                   })
                   .Build();
        }
        public static ResiliencePipeline<HttpResponseMessage> Retry()
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                   .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                   {
                       ShouldHandle = HttpPolicyExtensions.HandleTransientHttpError(),
                       Delay = TimeSpan.FromSeconds(1),
                       MaxRetryAttempts = 5,
                       UseJitter = true,
                       BackoffType = DelayBackoffType.Exponential,
                       OnRetry = (args) =>
                       {
                           Console.WriteLine($"Retry Attempt Number : {args.AttemptNumber} after {args.RetryDelay.TotalSeconds} seconds.");
                           return default;
                       }
                   })
                   .Build();
        }

        public static ResiliencePipeline<HttpResponseMessage> Retry(string someData)
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                   .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                   {
                       ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                           .Handle<HttpRequestException>()
                           .HandleResult(result => !result.IsSuccessStatusCode),
                       Delay = TimeSpan.FromSeconds(1),
                       MaxRetryAttempts = 5,
                       UseJitter = true,
                       BackoffType = DelayBackoffType.Exponential,
                       OnRetry = args =>
                       {
                           Console.WriteLine($"Retry Attempt Number : {args.AttemptNumber} after {args.RetryDelay.TotalSeconds} seconds.");
                           return default;
                       }
                   })
                   .Build();
        }
    }
}