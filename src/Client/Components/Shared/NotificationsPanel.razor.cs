using System.Globalization;

using Shared.Models;

namespace Client.Components.Shared;

public partial class NotificationsPanel
{
    /// <summary>
    /// Mutable UI state for the panel, kept in one object so the booleans
    /// read as a single group instead of scattered fields.
    /// </summary>
    private sealed class PanelState
    {
        public bool Loading { get; set; } = true;
        public bool MarkingAll { get; set; }
        public bool PushSupported { get; set; }
        public bool PushEnabled { get; set; }
        public bool PushBusy { get; set; }
        public bool PushChecked { get; set; }
        public bool PermissionDenied { get; set; }
    }

    private readonly PanelState _state = new();
    private readonly List<NotificationDto> _items = [];
    private readonly HashSet<string> _busyIds = [];

    private int UnreadCount => _items.Count(n => n.ReadAt == null);

    private string PushStatusText => !_state.PushChecked
        ? L["Np_PushChecking"]
        : _state.PushEnabled
            ? L["Np_PushOn"]
            : _state.PermissionDenied
                ? L["Np_PushDenied"]
                : _state.PushSupported
                    ? L["Np_PushPromo"]
                    : L["Np_PushUnsupported"];

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _state.PushChecked)
            return;

        _state.PushChecked = true;

        try
        {
            _state.PushSupported = await Push.IsSupported();

            if (_state.PushSupported)
            {
                _state.PushEnabled = (await Push.GetSubscription()).IsActive;

                if (!_state.PushEnabled)
                {
                    try
                    {
                        _state.PermissionDenied = await Notifications.GetPermission()
                            == Bit.Butil.NotificationPermission.Denied;
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning(ex, "Notification permission read failed.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Push availability check failed.");
            _state.PushSupported = false;
            _state.PushEnabled = false;
        }

        StateHasChanged();
    }

    private async Task EnablePushAsync()
    {
        if (_state.PushBusy)
            return;

        _state.PushBusy = true;
        try
        {
            if (_state.PushChecked && !_state.PushSupported)
            {
                Toast.Info(L["Np_PushUnsupported"]);
                return;
            }

            var permission = await ResolvePermissionAsync();

            if (permission == Bit.Butil.NotificationPermission.Denied)
            {
                // While denied no prompt will ever appear: subscribing
                // would just throw. Clearing the cache or reinstalling the
                // PWA does not reset the site permission either.
                _state.PermissionDenied = true;
                Toast.Info(L["Np_PushDeniedSettings"]);
                return;
            }

            if (permission != Bit.Butil.NotificationPermission.Granted)
            {
                Toast.Info(L["Np_PushBlocked"]);
                return;
            }

            _state.PermissionDenied = false;

            // Subscribing without an active worker registration fails even
            // with permission granted (and can return inactive instead of
            // throwing): catch it here with an actionable message.
            var workerReady = await WorkerReadyAsync();

            if (!workerReady)
            {
                Toast.Info(L["Np_PushWorkerNotReady"]);
                return;
            }

            using var key = await Api.Push.GetPublicKeyAsync();

            if (!key.IsSuccessfulWithContent)
            {
                Toast.Error(L["Np_PushNotConfigured"]);
                return;
            }

            Bit.Butil.PushSubscriptionInfo? subscription = null;
            try
            {
                subscription = await Push.Subscribe(key.Content.PublicKey);
            }
            catch (Exception ex)
            {
                // A stale subscription (e.g. from before a VAPID rotation)
                // can reject a fresh subscribe despite granted permission:
                // clear it and retry once before giving up.
                Log.LogWarning(ex, "Browser push subscription failed; retrying after unsubscribe.");

                try
                {
                    await Push.Unsubscribe();
                    subscription = await Push.Subscribe(key.Content.PublicKey);
                }
                catch (Exception retryEx)
                {
                    Log.LogWarning(retryEx, "Browser push subscription retry failed.");
                }
            }

            if (subscription is not { IsActive: true })
            {
                // Permission is granted and the worker is registered here,
                // so this is a subscription failure (push service error,
                // invalid VAPID key) — never blame the permission for it.
                Log.LogWarning("Browser push subscription inactive despite granted permission.");
                Toast.Error(L["Np_PushSubscribeFailed"]);
                return;
            }

            using var saved = await Api.Push.SubscribeAsync(new PushSubscriptionRequest(
                subscription.Endpoint,
                subscription.P256dh,
                subscription.Auth,
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

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

                Toast.Error(L["Np_PushSaveFailed"]);
                return;
            }

            _state.PushEnabled = true;
            Toast.Success(L["Np_PushEnabled"]);
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Enabling push notifications failed.");
            Toast.Error(L["Np_PushEnableFailed"]);
        }
        finally
        {
            _state.PushBusy = false;
        }
    }

    private async Task<Bit.Butil.NotificationPermission> ResolvePermissionAsync()
    {
        try
        {
            var current = await Notifications.GetPermission();

            if (current == Bit.Butil.NotificationPermission.Granted
                || current == Bit.Butil.NotificationPermission.Denied)
                return current;

            return await Notifications.RequestPermission();
        }
        catch (Exception ex)
        {
            // Permission APIs missing or failing: fall through to the
            // subscribe attempt, which is the historical behavior.
            Log.LogWarning(ex, "Notification permission resolution failed; attempting subscribe.");

            return Bit.Butil.NotificationPermission.Granted;
        }
    }

    private async Task<bool> WorkerReadyAsync()
    {
        try
        {
            return (await Worker.GetRegistration()).IsRegistered;
        }
        catch (Exception ex)
        {
            // Registration read failing: fall through to the subscribe
            // attempt rather than blocking on diagnostics.
            Log.LogWarning(ex, "Service worker registration read failed; attempting subscribe.");

            return true;
        }
    }

    private async Task DisablePushAsync()
    {
        if (_state.PushBusy)
            return;

        _state.PushBusy = true;
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
                    _state.PushEnabled = (await Push.GetSubscription()).IsActive;
                    Toast.Error(L["Np_PushRemoveFailed"]);
                    return;
                }
            }

            _state.PushEnabled = false;
            Toast.Success(L["Np_PushDisabled"]);
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Disabling push notifications failed.");
            Toast.Error(L["Np_PushDisableFailed"]);
        }
        finally
        {
            _state.PushBusy = false;
        }
    }

    private async Task LoadAsync()
    {
        _state.Loading = true;
        try
        {
            using var response = await Api.Notifications.GetAsync();

            if (!response.IsSuccessfulWithContent)
            {
                Toast.Error(L["Np_LoadFailed"]);
                return;
            }

            _items.Clear();
            _items.AddRange(response.Content.Items);
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Notifications load failed.");
            Toast.Error(L["Np_LoadFailed"]);
        }
        finally
        {
            _state.Loading = false;
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
                Toast.Error(L["Np_MarkReadFailed"]);
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
        if (_state.MarkingAll)
            return;

        _state.MarkingAll = true;
        try
        {
            using var response = await Api.Notifications.MarkAllReadAsync();

            if (!response.IsSuccessfulWithContent)
            {
                Toast.Error(L["Np_MarkAllReadFailed"]);
                return;
            }

            await LoadAsync();
        }
        finally
        {
            _state.MarkingAll = false;
        }
    }

    private bool IsBusy(NotificationDto notification) => _busyIds.Contains(notification.Id);

    private static string FormatWhen(DateTimeOffset createdAt)
        => createdAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}