using System;
using System.Threading;
using System.Threading.Tasks;

using Doario.Data.Models.Mail;
using Doario.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Doario.Web.Services
{
    /// <summary>
    /// Runs every 5 minutes. Retries Failed deliveries up to 3 times.
    /// On 3rd failure marks PermanentFail (SystemStatusId=9) and stops.
    /// </summary>
    public class EmailRetryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailRetryService> _logger;

        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
        private const int MaxRetries = 3;
        private const int SystemStatus_Failed = 5;
        private const int SystemStatus_Sent = 8;
        private const int SystemStatus_PermanentFail = 9;

        public EmailRetryService(IServiceScopeFactory scopeFactory, ILogger<EmailRetryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailRetryService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Interval, stoppingToken);

                try
                {
                    await ProcessRetries(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EmailRetryService top-level error.");
                }
            }
        }

        private async Task ProcessRetries(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var deliveryRepo = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
            var errorLogRepo = scope.ServiceProvider.GetRequiredService<IErrorLogRepository>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailDeliveryService>();

            var failed = await deliveryRepo.GetFailedForRetryAsync();

            if (failed.Count == 0)
                return;

            _logger.LogInformation("EmailRetryService: {Count} failed deliveries to retry.", failed.Count);

            foreach (var delivery in failed)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    // Attempt re-delivery using the existing EmailDeliveryService
                    await emailService.RetryDeliveryAsync(delivery);

                    delivery.SystemStatusId = SystemStatus_Sent;
                    delivery.ErrorMessage = null;
                    _logger.LogInformation("Retry succeeded for delivery {Id}.", delivery.DocumentDeliveryId);
                }
                catch (Exception ex)
                {
                    delivery.RetryCount++;
                    delivery.LastRetryAt = DateTime.UtcNow;
                    delivery.ErrorMessage = ex.Message;

                    if (delivery.RetryCount >= MaxRetries)
                    {
                        delivery.SystemStatusId = SystemStatus_PermanentFail;
                        _logger.LogWarning(
                            "Delivery {Id} permanently failed after {Max} retries.",
                            delivery.DocumentDeliveryId, MaxRetries);
                    }
                    else
                    {
                        delivery.SystemStatusId = SystemStatus_Failed; // stays Failed for next cycle
                        _logger.LogWarning(
                            "Retry {Count}/{Max} failed for delivery {Id}: {Msg}",
                            delivery.RetryCount, MaxRetries, delivery.DocumentDeliveryId, ex.Message);
                    }

                    // Log to ErrorLog table
                    await errorLogRepo.AddAsync(new ErrorLog
                    {
                        ErrorLogId = Guid.NewGuid(),
                        TenantId = delivery.TenantId,
                        DocumentId = delivery.DocumentId,
                        ErrorType = "Delivery",
                        Message = ex.Message,
                        StackTrace = ex.StackTrace,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await deliveryRepo.SaveAsync();
                await errorLogRepo.SaveAsync();
            }
        }
    }
}