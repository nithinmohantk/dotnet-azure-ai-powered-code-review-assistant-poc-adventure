using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace CodeReviewAssistant.WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenTelemetryObservability(this IServiceCollection services, IConfiguration configuration)
        {
            var serviceName = configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "CodeReviewAssistant";
            var serviceVersion = configuration.GetValue<string>("OpenTelemetry:ServiceVersion") ?? "1.0.0";
            var enableConsoleExporter = configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter", true);
            var enableOtlpExporter = configuration.GetValue<bool>("OpenTelemetry:EnableOtlpExporter", false);
            var otlpEndpoint = configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317";

            // Configure OpenTelemetry Resource
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("service.instance.id", Environment.MachineName),
                    new KeyValuePair<string, object>("service.namespace", "codereview"),
                    new KeyValuePair<string, object>("deployment.environment", 
                        configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? "Development")
                });

            // Add Tracing
            services.AddOpenTelemetryTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(serviceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.method", request.Method);
                            activity.SetTag("http.url", request.Scheme + "://" + request.Host + request.Path);
                            activity.SetTag("http.host", request.Host.ToString());
                            activity.SetTag("http.scheme", request.Scheme);
                            activity.SetTag("http.user_agent", request.Headers["User-Agent"].ToString());
                            activity.SetTag("http.remote_addr", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                        };
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.status_code", response.StatusCode);
                            activity.SetStatus(response.StatusCode < 400 ? 
                                ActivityStatusCode.Ok : ActivityStatusCode.Error);
                        };
                        options.EnrichWithException = (activity, exception) =>
                        {
                            activity.SetTag("error.type", exception.GetType().Name);
                            activity.SetTag("error.message", exception.Message);
                            activity.SetTag("error.stack_trace", exception.StackTrace);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.method", request.Method.Method);
                            activity.SetTag("http.url", request.RequestUri?.ToString());
                            activity.SetTag("http.host", request.RequestUri?.Host);
destination);
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response `${response.StatusCode}`);
                           yota
                        options.Enrichlimited exception
                        });
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddRedisInstrumentation()
                    .AddSource("MediatR")
                    .AddSource("Azure.Messaging.ServiceBus")
                    .AddSource("Azure.Cosmos");

                if (enableConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }

                if (enableOtlpExporter)
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Headers = $"x-api-key={configuration.GetValue<string>("OpenTelemetry:ApiKey")}";
                    });
                }
            });

            // Add Metrics
            services.AddOpenTelemetryMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(serviceName)
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("System.Net.Http")
                    .AddMeter("System.Net.NameResolution");

                if (enableConsoleExporter)
                {
                    builder.AddConsoleExporter();
                }

                if (enableOtlpExporter)
                {
                    builder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Headers = $"x-api-key={configuration.GetValue<string>("OpenTelemetry:ApiKey")}";
                    });
                }
            });

            // Add Logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                
                if (enableOtlpExporter)
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.SetResourceBuilder(resourceBuilder);
                        options.IncludeFormattedMessage = true;
                        options.IncludeScopes = true;
                        options.ParseStateValues = true;
                        
                        options.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(otlpEndpoint);
                            otlpOptions.Headers = $"x-api-key={configuration.GetValue<string>("OpenTelemetry:ApiKey")}";
                        });
                    });
                }
            });

            return services;
        }

        public static IServiceCollection AddCustomMetrics(this IServiceCollection services)
        {
            services.AddSingleton<IMetricsService, MetricsService>();
            return services;
        }
    }

    public interface IMetricsService
    {
        void IncrementCodeReviewCreated();
        void IncrementCodeReviewCompleted();
        void IncrementAIAnalysisStarted();
        void IncrementAIAnalysisCompleted();
        void RecordCodeReviewProcessingTime(TimeSpan duration);
        void RecordAIAnalysisProcessingTime(TimeSpan duration);
        void RecordActiveConnections(int count);
        void IncrementErrorCount(string errorType);
    }

    public class MetricsService : IMetricsService
    {
        private readonly Counter<int> _codeReviewCreatedCounter;
        private readonly Counter<int> _codeReviewCompletedCounter;
        private readonly Counter<int> _aiAnalysisStartedCounter;
        private readonly Counter<int> _aiAnalysisCompletedCounter;
        private readonly Histogram<double> _codeReviewProcessingTime;
        private readonly Histogram<double> _aiAnalysisProcessingTime;
        private readonly ObservableGauge<int> _activeConnectionsGauge;
        private readonly Counter<int> _errorCounter;

        private int _activeConnections = 0;

        public MetricsService(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("CodeReviewAssistant");

            _codeReviewCreatedCounter = meter.CreateCounter<int>("codereview_created_total", "Total number of code reviews created");
            _codeReviewCompletedCounter = meter.CreateCounter<int>("codereview_completed_total", "Total number of code reviews completed");
            _aiAnalysisStartedCounter = meter.CreateCounter<int>("ai_analysis_started_total", "Total number of AI analyses started");
            _aiAnalysisCompletedCounter = meter.CreateCounter<int>("ai_analysis_completed_total", "Total number of AI analyses completed");
            _codeReviewProcessingTime = meter.CreateHistogram<double>("codereview_processing_duration_seconds", "Code review processing time in seconds");
            _aiAnalysisProcessingTime = meter.CreateHistogram<double>("ai_analysis_processing_duration_seconds", "AI analysis processing time in seconds");
            _activeConnectionsGauge = meter.CreateObservableGauge<int>("active_connections", () => _activeConnections, "Number of active connections");
            _errorCounter = meter.CreateCounter<int>("errors_total", "Total number of errors", "error_type");
        }

        public void IncrementCodeReviewCreated()
        {
            _codeReviewCreatedCounter.Add(1);
        }

        public void IncrementCodeReviewCompleted()
        {
            _codeReviewCompletedCounter.Add(1);
        }

        public void IncrementAIAnalysisStarted()
        {
            _aiAnalysisStartedCounter.Add(1);
        }

        public void IncrementAIAnalysisCompleted()
        {
            _aiAnalysisCompletedCounter.Add(1);
        }

        public void RecordCodeReviewProcessingTime(TimeSpan duration)
        {
            _codeReviewProcessingTime.Record(duration.TotalSeconds);
        }

        public void RecordAIAnalysisProcessingTime(TimeSpan duration)
        {
            _aiAnalysisProcessingTime.Record(duration.TotalSeconds);
        }

        public void RecordActiveConnections(int count)
        {
            _activeConnections = count;
        }

        public void IncrementErrorCount(string errorType)
        {
            _errorCounter.Add(1, new KeyValuePair<string, object>("error_type", errorType));
        }
    }
}
