using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Doario.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Doario.Web.Services
{
    /// <summary>
    /// Singleton queue that processes AI summaries in parallel batches.
    /// Limits concurrency to avoid hitting Azure OpenAI TPM limits.
    /// Retries with exponential backoff on rate limit errors.
    /// </summary>
    public class AiProcessingQueue
    {
        private readonly ConcurrentQueue<Guid> _queue = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AiProcessingQueue> _logger;

        // Max concurrent AI calls — tune based on your TPM limit:
        // 10K TPM  → 2 concurrent
        // 40K TPM  → 5 concurrent
        // 100K TPM → 10 concurrent
        // 2M TPM   → 20 concurrent
        private const int MaxConcurrent = 20;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 10000; // 10s on rate limit

        public AiProcessingQueue(
            IServiceScopeFactory scopeFactory,
            ILogger<AiProcessingQueue> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _semaphore = new SemaphoreSlim(MaxConcurrent, MaxConcurrent);
        }

        /// <summary>
        /// Enqueue a document for AI processing. Returns immediately.
        /// Processing happens in the background with concurrency control.
        /// </summary>
        public void Enqueue(Guid documentId)
        {
            _ = Task.Run(() => ProcessWithSemaphoreAsync(documentId));
        }

        /// <summary>
        /// Enqueue multiple documents. Each gets a small stagger to smooth out the load.
        /// </summary>
        public void EnqueueBatch(IEnumerable<Guid> documentIds)
        {
            var list = new List<Guid>(documentIds);
            _logger.LogInformation("AiProcessingQueue: enqueuing batch of {Count} documents.", list.Count);

            for (int i = 0; i < list.Count; i++)
            {
                var id = list[i];
                var delay = i * 200; // 200ms stagger between enqueues
                _ = Task.Run(async () =>
                {
                    if (delay > 0) await Task.Delay(delay);
                    await ProcessWithSemaphoreAsync(id);
                });
            }
        }

        private async Task ProcessWithSemaphoreAsync(Guid documentId)
        {
            await _semaphore.WaitAsync();
            try
            {
                await ProcessWithRetryAsync(documentId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ProcessWithRetryAsync(Guid documentId)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var aiSummaryService = scope.ServiceProvider.GetRequiredService<AiSummaryService>();
                    await aiSummaryService.GenerateAndSaveAsync(documentId);
                    return; // success
                }
                catch (Exception ex) when (IsRateLimitError(ex))
                {
                    var wait = RetryDelayMs * attempt;
                    _logger.LogWarning(
                        "AiProcessingQueue: rate limited on document {Id}, attempt {Attempt}/{Max}. Waiting {Wait}ms.",
                        documentId, attempt, MaxRetries, wait);

                    if (attempt < MaxRetries)
                        await Task.Delay(wait);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "AiProcessingQueue: failed to process document {Id} on attempt {Attempt}.",
                        documentId, attempt);
                    return; // non-rate-limit error — don't retry
                }
            }

            _logger.LogError(
                "AiProcessingQueue: gave up on document {Id} after {Max} attempts.",
                documentId, MaxRetries);
        }

        private static bool IsRateLimitError(Exception ex)
        {
            var msg = ex.Message + (ex.InnerException?.Message ?? "");
            return msg.Contains("429") ||
                   msg.Contains("rate limit") ||
                   msg.Contains("Rate limit") ||
                   msg.Contains("TooManyRequests") ||
                   msg.Contains("quota");
        }
    }
}