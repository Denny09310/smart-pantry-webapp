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

    public async Task EnableAsync()
    {
        try
        {
            if (!await notification.IsSupported())
                return;

            var result = await notification.RequestPermission();

            if (result is not Bit.Butil.NotificationPermission.Granted)
                return;

            if (!await push.IsSupported())
                return;

            using var response = await api.Push.GetPushNotificationKeyAsync();

            if (!response.IsSuccessfulWithContent)
                return;

            var info = await push.Subscribe(response.Content.PublicKey);
            Enabled = info.IsActive;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Enabling notification failed.");
        }
    }

    private void SetCount(int count)
    {
        if (Unread == count)
            return;

        Unread = count;
        Changed?.Invoke();
    }
}