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
        try
        {
            using var scope = services.CreateScope();
            var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
            var created = await notifications.GenerateExpirationNotificationsAsync(ct);

            var pushed = 0;
            if (created > 0)
            {
                var push = scope.ServiceProvider.GetRequiredService<PushService>();
                pushed = await push.SendUnreadAsync(ct);
            }

            log.LogInformation(
                "Expiration check completed, {Count} notification(s) created, {Pushed} push(es) sent.",
                created,
                pushed);
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
