namespace Client.Services;

/// <summary>
/// In-app notification state: unread badge count plus read/mark-all
/// operations. The panel itself loads the list on open.
/// </summary>
internal sealed class NotificationService(
    ServerApi api,
    Bit.Butil.Notification notification,
    Bit.Butil.Push push,
    ILogger<NotificationService> log)
{
    public int Unread { get; private set; }
    public bool Enabled { get; private set; }

    public event Action? Changed;

    private bool _initialized;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            var subscription = await push.GetSubscription();
            SetEnabled(subscription.IsActive);

            // The browser keeps its subscription across reloads, but the server
            // row can disappear: a subscribe that lost a race, an endpoint the
            // push service rejected, or simply a new database. Once that happens
            // the UI still says "enabled" and no push ever arrives again, so
            // re-register on startup to heal the drift.
            if (subscription.IsActive && !string.IsNullOrWhiteSpace(subscription.Endpoint))
                await RegisterAsync(subscription.Endpoint, subscription.P256dh, subscription.Auth);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Push subscription lookup failed.");
        }

        await RefreshAsync();
    }

    /// <summary>
    /// Upserts a browser subscription with the server. False means the server
    /// refused it, which is the only signal that push cannot work.
    /// </summary>
    private async Task<bool> RegisterAsync(string endpoint, string p256dh, string auth)
    {
        try
        {
            using var response = await api.Push.SubscribeAsync(
                new Shared.Models.PushSubscriptionRequest(endpoint, p256dh, auth));

            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning("Push subscription save failed with {StatusCode}.", response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Push subscription save failed.");
            return false;
        }
    }

    public async Task RefreshAsync()
    {
        try
        {
            using var response = await api.Notifications.GetAsync();
            if (response.IsSuccessfulWithContent)
                SetCount(response.Content.UnreadCount);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Notification refresh failed.");
        }
    }

    public async Task<bool> MarkReadAsync(string id)
    {
        try
        {
            using var response = await api.Notifications.MarkReadAsync(id);
            if (!response.IsSuccessStatusCode)
                return false;

            await RefreshAsync();
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Marking notification {NotificationId} read failed.", id);
            return false;
        }
    }

    public async Task MarkAllReadAsync()
    {
        try
        {
            using var response = await api.Notifications.MarkAllReadAsync();
            if (response.IsSuccessStatusCode)
                SetCount(0);
            else
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Marking all notifications read failed.");
        }
    }

    public async Task<bool> EnableAsync()
    {
        try
        {
            if (!await notification.IsSupported() || !await push.IsSupported())
                return false;

            if (await notification.RequestPermission() is not Bit.Butil.NotificationPermission.Granted)
                return false;

            using var keyResponse = await api.Push.GetPushNotificationKeyAsync();
            if (!keyResponse.IsSuccessfulWithContent
                || string.IsNullOrWhiteSpace(keyResponse.Content.PublicKey))
                return false;

            var info = await push.Subscribe(keyResponse.Content.PublicKey);
            if (!info.IsActive || string.IsNullOrWhiteSpace(info.Endpoint))
                return false;

            // Keep the browser subscription only when the server accepted it:
            // a local subscription the server does not know about silently
            // never receives anything.
            if (!await RegisterAsync(info.Endpoint, info.P256dh, info.Auth))
            {
                await push.Unsubscribe();
                return false;
            }

            SetEnabled(true);
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Enabling notifications failed.");
            return false;
        }
    }

    public async Task<bool> DisableAsync()
    {
        try
        {
            var current = await push.GetSubscription();
            if (current.IsActive && !string.IsNullOrWhiteSpace(current.Endpoint))
            {
                using var _ = await api.Push.UnsubscribeAsync(current.Endpoint);
            }

            await push.Unsubscribe();
            SetEnabled(false);
            return true;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Disabling notifications failed.");
            return false;
        }
    }

    private void SetEnabled(bool enabled)
    {
        if (Enabled == enabled)
            return;

        Enabled = enabled;
        Changed?.Invoke();
    }

    /// <summary>Adopts a count already fetched elsewhere (panel loads).</summary>
    public void SyncUnreadCount(int count) => SetCount(count);

    private void SetCount(int count)
    {
        if (Unread == count)
            return;

        Unread = count;
        Changed?.Invoke();
    }
}