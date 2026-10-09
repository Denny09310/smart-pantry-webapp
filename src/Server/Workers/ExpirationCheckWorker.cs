using System.Diagnostics;

using Microsoft.Extensions.Options;

using Server.Services;

namespace Server.Workers;

/// <summary>
/// Once per day (plus a catch-up run at startup): asks the
/// <see cref="NotificationService"/> to generate expiration notifications,
/// then fans the unread ones out as web pushes.
/// </summary>
internal sealed class ExpirationCheckWorker(IServiceProvider services, ILogger<ExpirationCheckWorker> log)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunOnceAsync(stoppingToken);
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = services.CreateScope();
            var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
            var generated = await notifications.GenerateExpirationNotificationsAsync(ct);

            // Always fan out: pre-existing unread notifications and devices
            // subscribed after they were created still need waking.
            // Unconfigured push is a normal state, not an error.
            var pushed = new PushService.PushResult(0, 0, 0);
            var pushOptions = scope.ServiceProvider.GetRequiredService<IOptions<PushOptions>>().Value;

            if (pushOptions.IsConfigured)
            {
                var push = scope.ServiceProvider.GetRequiredService<PushService>();
                pushed = await push.SendUnreadAsync(ct);
            }
            else
            {
                log.LogDebug("Push skipped: VAPID keys are not configured.");
            }

            log.LogInformation(
                "Expiration check completed in {ElapsedMs}ms: {Evaluated} item(s) evaluated, " +
                "{Created} notification(s) created, {Sent} push(es) sent, " +
                "{Failed} push(es) failed, {Pruned} subscription(s) pruned.",
                stopwatch.ElapsedMilliseconds,
                generated.Evaluated,
                generated.Created,
                pushed.Sent,
                pushed.Failed,
                pushed.Pruned);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Host is stopping; let it.
        }
        catch (Exception ex)
        {
            // Never crash the host because of a background check.
            log.LogError(ex, "Expiration check failed.");
        }
    }
}