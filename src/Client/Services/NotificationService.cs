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
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Push subscription lookup failed.");
        }

        await RefreshAsync();
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

            using var saveResponse = await api.Push.SubscribeAsync(
                new Shared.Models.PushSubscriptionRequest(info.Endpoint, info.P256dh, info.Auth));

            if (!saveResponse.IsSuccessStatusCode)
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

    private void SetCount(int count)
    {
        if (Unread == count)
            return;

        Unread = count;
        Changed?.Invoke();
    }
}
