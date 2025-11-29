using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace Serilog.Sinks.Mongo.Sample.Services;

internal class LoggingService : BackgroundService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly IConfiguration? _configuration;
    private static readonly ActivitySource _activitySource = new("Serilog.Sinks.SqlServer.Sample");
    private static readonly ActivityListener _activityListener;

    static LoggingService()
    {
        // Configure Activity listener to enable activity creation
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Serilog.Sinks.SqlServer.Sample",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllDataAndRecorded
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    public LoggingService(ILogger<LoggingService> logger, IConfiguration? configuration = null)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var appName = _configuration?["Serilog:Properties:Application"] ?? "Serilog.Sinks.Mongo.Sample";
        _logger.LogInformation("LoggingService started for application: {ApplicationName}", appName);

        try
        {
            var random = new Random();

            // Demonstrate various logging scenarios
            await DemonstrateLoggingLevels(stoppingToken);
            await DemonstrateStructuredLogging(random, stoppingToken);
            await DemonstrateOperationalLogging(random, stoppingToken);
            await DemonstrateExceptionLogging(stoppingToken);
            await DemonstrateScopedLogging(random, stoppingToken);
            await DemonstrateActivityLogging(random, stoppingToken);

            _logger.LogInformation("All logging demonstrations completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoggingService");
        }
        finally
        {
            _logger.LogInformation("LoggingService stopped");
        }
    }

    private async Task DemonstrateLoggingLevels(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Log Level Examples ===");

        _logger.LogTrace("TRACE: Detailed diagnostic information for troubleshooting");
        _logger.LogDebug("DEBUG: Internal system events useful during development");
        _logger.LogInformation("INFORMATION: General application flow and milestones");
        _logger.LogWarning("WARNING: Abnormal or unexpected events that don't stop execution");
        _logger.LogError("ERROR: Errors and exceptions that affect current operation");
        _logger.LogCritical("CRITICAL: Critical failures requiring immediate attention");

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    private async Task DemonstrateStructuredLogging(Random random, CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Structured Logging Examples ===");

        // Simple structured properties
        _logger.LogInformation("User {UserId} logged in from {IpAddress} at {LoginTime}",
            "user-123", "192.168.1.100", DateTime.UtcNow);

        // Complex object properties using destructuring operator @
        var orderInfo = new
        {
            OrderId = Guid.NewGuid(),
            CustomerId = "CUST-456",
            TotalAmount = 299.99m,
            Items = 3,
            Status = "Confirmed"
        };
        _logger.LogInformation("Order created: {@OrderInfo}", orderInfo);

        // Sensor/metrics data (good for time series)
        _logger.LogInformation("Sensor reading - Temperature: {Temperature}°C, Humidity: {Humidity}%, Pressure: {Pressure}hPa, Location: {Location}",
            random.Next(15, 30),
            random.Next(40, 80),
            random.Next(980, 1020),
            "DataCenter-A");

        // Performance metrics
        var metrics = new Dictionary<string, object>
        {
            ["ResponseTime"] = random.Next(50, 500),
            ["MemoryUsage"] = random.Next(100, 1000),
            ["ActiveConnections"] = random.Next(10, 100),
            ["CpuUsage"] = random.Next(20, 90)
        };
        _logger.LogInformation("Performance metrics: {@Metrics}", metrics);

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    private async Task DemonstrateOperationalLogging(Random random, CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Operational Logging Examples ===");

        var operations = new[] { "Create", "Read", "Update", "Delete", "Query" };
        var statuses = new[] { "Success", "Failed", "Pending", "Timeout" };

        for (int i = 1; i <= 5; i++)
        {
            var operation = operations[random.Next(operations.Length)];
            var status = statuses[random.Next(statuses.Length)];
            var duration = random.Next(10, 500);

            _logger.LogInformation("Operation #{Counter}: {Operation} completed with {Status} in {Duration}ms",
                i, operation, status, duration);

            if (status == "Failed")
            {
                _logger.LogError("Operation failed - Type: {Operation}, ErrorCode: {ErrorCode}, RetryCount: {RetryCount}",
                    operation, $"ERR-{random.Next(1000, 9999)}", random.Next(0, 3));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    private async Task DemonstrateExceptionLogging(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Exception Logging Examples ===");

        // Simple exception
        try
        {
            throw new InvalidOperationException("Simulated database connection error")
            {
                Source = "LoggingService",
                HelpLink = "https://docs.example.com/errors/db-connection"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Database connection failed for connection string: {ConnectionString}",
                "[REDACTED]");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);

        // Nested exception
        try
        {
            try
            {
                throw new ArgumentNullException("userId", "User ID cannot be null");
            }
            catch (ArgumentNullException inner)
            {
                throw new ApplicationException("User validation failed due to invalid input", inner);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation error occurred for operation: {Operation}", "UserAuthentication");
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);

        // Business logic exception with context
        try
        {
            throw new InvalidOperationException("Insufficient inventory")
            {
                Data = { ["ProductId"] = "PROD-789", ["RequestedQty"] = 100, ["AvailableQty"] = 45 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory check failed - Product: {ProductId}, Requested: {Requested}, Available: {Available}",
                ex.Data["ProductId"], ex.Data["RequestedQty"], ex.Data["AvailableQty"]);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    private async Task DemonstrateScopedLogging(Random random, CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Scoped Logging Examples ===");

        for (int i = 1; i <= 3; i++)
        {
            var requestId = Guid.NewGuid().ToString();
            var correlationId = Guid.NewGuid().ToString();
            var userId = $"user-{random.Next(1, 100)}";

            // All logs within this scope will include RequestId, CorrelationId, and UserId
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestId"] = requestId,
                ["CorrelationId"] = correlationId,
                ["UserId"] = userId,
                ["RequestNumber"] = i
            }))
            {
                _logger.LogInformation("Processing request #{RequestNumber}", i);

                _logger.LogDebug("Validating request parameters");
                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(100, 300)), stoppingToken);

                _logger.LogDebug("Executing business logic");
                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(100, 300)), stoppingToken);

                if (i == 2)
                {
                    _logger.LogWarning("Request processing took longer than expected: {ElapsedMs}ms", random.Next(800, 1500));
                }

                _logger.LogInformation("Request completed successfully with {ItemsProcessed} items processed",
                    random.Next(1, 50));
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    private async Task DemonstrateActivityLogging(Random random, CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== Activity Distributed Tracing Examples ===");

        // Example 1: Simple activity with tags
        using (var activity = _activitySource.StartActivity("ProcessOrder"))
        {
            var orderId = Guid.NewGuid().ToString();
            activity?.SetTag("order.id", orderId);
            activity?.SetTag("order.type", "Online");
            activity?.SetTag("customer.tier", "Premium");

            _logger.LogInformation("Started processing order with Activity - OrderId: {OrderId}, TraceId: {TraceId}, SpanId: {SpanId}",
                orderId, activity?.TraceId, activity?.SpanId);

            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(100, 300)), stoppingToken);

            activity?.AddEvent(new ActivityEvent("OrderValidated"));
            _logger.LogInformation("Order validated - OrderId: {OrderId}", orderId);

            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(100, 300)), stoppingToken);

            activity?.SetTag("order.total", random.Next(100, 1000));
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Order processing completed - OrderId: {OrderId}, Duration: {Duration}ms",
                orderId, activity?.Duration.TotalMilliseconds);
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);

        // Example 2: Nested activities for distributed tracing
        using (var parentActivity = _activitySource.StartActivity("CheckoutWorkflow", ActivityKind.Server))
        {
            var workflowId = Guid.NewGuid().ToString();
            parentActivity?.SetTag("workflow.id", workflowId);
            parentActivity?.SetTag("workflow.type", "Checkout");

            _logger.LogInformation("Starting checkout workflow - WorkflowId: {WorkflowId}, TraceId: {TraceId}",
                workflowId, parentActivity?.TraceId);

            // Child activity 1: Validate inventory
            using (var inventoryActivity = _activitySource.StartActivity("ValidateInventory"))
            {
                inventoryActivity?.SetTag("inventory.location", "Warehouse-A");
                inventoryActivity?.SetTag("inventory.items", random.Next(1, 5));

                _logger.LogInformation("Validating inventory - ParentId: {ParentId}, SpanId: {SpanId}",
                    inventoryActivity?.ParentSpanId, inventoryActivity?.SpanId);

                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(50, 150)), stoppingToken);

                if (random.Next(0, 10) > 8)
                {
                    inventoryActivity?.SetStatus(ActivityStatusCode.Error, "Insufficient inventory");
                    _logger.LogWarning("Inventory check failed - Items requested exceed available stock");
                }
                else
                {
                    inventoryActivity?.SetStatus(ActivityStatusCode.Ok);
                    _logger.LogInformation("Inventory validated successfully");
                }
            }

            // Child activity 2: Process payment
            using (var paymentActivity = _activitySource.StartActivity("ProcessPayment"))
            {
                var transactionId = Guid.NewGuid().ToString();
                paymentActivity?.SetTag("payment.method", "CreditCard");
                paymentActivity?.SetTag("payment.processor", "Stripe");
                paymentActivity?.SetTag("payment.transaction_id", transactionId);
                paymentActivity?.SetTag("payment.amount", random.Next(50, 500));

                _logger.LogInformation("Processing payment - TransactionId: {TransactionId}, SpanId: {SpanId}",
                    transactionId, paymentActivity?.SpanId);

                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(100, 400)), stoppingToken);

                paymentActivity?.AddEvent(new ActivityEvent("PaymentAuthorized",
                    tags: new ActivityTagsCollection { { "auth.code", $"AUTH-{random.Next(1000, 9999)}" } }));

                paymentActivity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Payment processed successfully - TransactionId: {TransactionId}", transactionId);
            }

            // Child activity 3: Send confirmation
            using (var notificationActivity = _activitySource.StartActivity("SendNotification"))
            {
                notificationActivity?.SetTag("notification.type", "Email");
                notificationActivity?.SetTag("notification.recipient", "customer@example.com");

                _logger.LogInformation("Sending confirmation notification - SpanId: {SpanId}",
                    notificationActivity?.SpanId);

                await Task.Delay(TimeSpan.FromMilliseconds(random.Next(50, 150)), stoppingToken);

                notificationActivity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Notification sent successfully");
            }

            parentActivity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Checkout workflow completed - WorkflowId: {WorkflowId}, TotalDuration: {Duration}ms, TraceId: {TraceId}",
                workflowId, parentActivity?.Duration.TotalMilliseconds, parentActivity?.TraceId);
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);

        // Example 3: Activity with baggage (context propagation)
        using (var activity = _activitySource.StartActivity("ProcessShipment"))
        {
            var shipmentId = Guid.NewGuid().ToString();
            activity?.SetTag("shipment.id", shipmentId);
            activity?.SetTag("shipment.carrier", "FedEx");
            activity?.SetTag("shipment.priority", "Express");

            // Baggage is propagated across service boundaries
            activity?.SetBaggage("tenant.id", "TENANT-123");
            activity?.SetBaggage("user.region", "US-West");

            _logger.LogInformation("Processing shipment with baggage - ShipmentId: {ShipmentId}, Baggage: {@Baggage}",
                shipmentId, activity?.Baggage.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

            await Task.Delay(TimeSpan.FromMilliseconds(random.Next(100, 300)), stoppingToken);

            activity?.AddEvent(new ActivityEvent("ShipmentLabeled"));
            activity?.AddEvent(new ActivityEvent("ShipmentDispatched"));

            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Shipment processed - ShipmentId: {ShipmentId}, TrackingNumber: {TrackingNumber}",
                shipmentId, $"TRK-{random.Next(100000, 999999)}");
        }

        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }
}
