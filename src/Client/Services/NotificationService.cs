namespace Client.Services;

/// <summary>
/// In-app notification state: unread badge count plus read/mark-all
/// operations. The panel itself loads the list on open.
/// </summary>
internal sealed class NotificationService(ServerApi api, ILogger<NotificationService> log)
{
    public int UnreadCount { get; private set; }

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

    private void SetCount(int count)
    {
        if (UnreadCount == count)
            return;

        UnreadCount = count;
        Changed?.Invoke();
    }
}
