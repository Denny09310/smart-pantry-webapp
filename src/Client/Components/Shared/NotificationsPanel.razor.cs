using Shared.Models;

namespace Client.Components.Shared;

public partial class NotificationsPanel : IAsyncDisposable
{
    private bool _loading = true;
    private bool _markingAll;
    private readonly List<NotificationDto> _items = [];
    private readonly HashSet<string> _busyIds = [];

    private bool _pushSupported;
    private bool _pushEnabled;
    private bool _pushBusy;
    private bool _pushChecked;

    private int UnreadCount => _items.Count(n => n.ReadAt == null);

    private string PushStatusText => _pushEnabled
        ? "On for this browser"
        : _pushSupported
            ? "Get expiry alerts even with the app closed"
            : "Not supported in this browser";

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _pushChecked)
            return;

        _pushChecked = true;

        try
        {
            _pushSupported = await Push.IsSupported();

            if (_pushSupported)
                _pushEnabled = (await Push.GetSubscription()).IsActive;
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Push availability check failed.");
            _pushSupported = false;
            _pushEnabled = false;
        }

        StateHasChanged();
    }

    private async Task EnablePushAsync()
    {
        if (_pushBusy)
            return;

        _pushBusy = true;
        try
        {
            using var key = await Api.Push.GetPublicKeyAsync();

            if (!key.IsSuccessfulWithContent)
            {
                Toast.Error("Push is not configured on the server.");
                return;
            }

            Bit.Butil.PushSubscriptionInfo subscription;
            try
            {
                subscription = await Push.Subscribe(key.Content.PublicKey);
            }
            catch (Exception ex)
            {
                Log.LogWarning(ex, "Browser push subscription failed.");
                Toast.Info("Push was blocked. Allow notifications to enable it.");
                return;
            }

            if (!subscription.IsActive)
            {
                Toast.Info("Push was blocked. Allow notifications to enable it.");
                return;
            }

            using var saved = await Api.Push.SubscribeAsync(new PushSubscriptionRequest(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth));

            if (!saved.IsSuccessfulWithContent)
            {
                // Roll back the browser side: a subscription the server
                // doesn't know about would never receive pushes.
                try
                {
                    await Push.Unsubscribe();
                }
                catch (Exception ex)
                {
                    Log.LogWarning(ex, "Push rollback unsubscribe failed.");
                }

                Toast.Error("Can't save push subscription.");
                return;
            }

            _pushEnabled = true;
            Toast.Success("Push notifications enabled.");
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Enabling push notifications failed.");
            Toast.Error("Can't enable push notifications.");
        }
        finally
        {
            _pushBusy = false;
        }
    }

    private async Task DisablePushAsync()
    {
        if (_pushBusy)
            return;

        _pushBusy = true;
        try
        {
            var existing = await Push.GetSubscription();

            if (existing.IsActive)
            {
                await Push.Unsubscribe();
                using var removed = await Api.Push.UnsubscribeAsync(existing.Endpoint);

                if (!removed.IsSuccessStatusCode)
                {
                    // Browser is unsubscribed but the server still lists the
                    // endpoint: report it instead of claiming success.
                    _pushEnabled = (await Push.GetSubscription()).IsActive;
                    Toast.Error("Can't remove push subscription from the server.");
                    return;
                }
            }

            _pushEnabled = false;
            Toast.Success("Push notifications disabled.");
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Disabling push notifications failed.");
            Toast.Error("Can't disable push notifications.");
        }
        finally
        {
            _pushBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            using var response = await Api.Notifications.GetAsync();

            if (!response.IsSuccessfulWithContent)
            {
                Toast.Error("Can't retrieve notifications.");
                return;
            }

            _items.Clear();
            _items.AddRange(response.Content.Items);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task MarkReadAsync(NotificationDto notification)
    {
        if (!_busyIds.Add(notification.Id))
            return;

        try
        {
            using var response = await Api.Notifications.MarkReadAsync(notification.Id);

            if (!response.IsSuccessfulWithContent)
            {
                Toast.Error("Can't update notification.");
                return;
            }

            var index = _items.FindIndex(n => n.Id == notification.Id);
            if (index >= 0)
                _items[index] = response.Content;
        }
        finally
        {
            _busyIds.Remove(notification.Id);
        }
    }

    private async Task MarkAllReadAsync()
    {
        if (_markingAll)
            return;

        _markingAll = true;
        try
        {
            using var response = await Api.Notifications.MarkAllReadAsync();

            if (!response.IsSuccessfulWithContent)
            {
                Toast.Error("Can't update notifications.");
                return;
            }

            await LoadAsync();
        }
        finally
        {
            _markingAll = false;
        }
    }

    private bool IsBusy(NotificationDto notification) => _busyIds.Contains(notification.Id);

    private static string FormatWhen(DateTimeOffset createdAt)
        => createdAt.ToLocalTime().ToString("dd MMM HH:mm");

    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}