using Shared.Models;

namespace Client.Components.Shared;

public partial class NotificationsPanel : IAsyncDisposable
{
    private bool _loading = true;
    private bool _markingAll;
    private readonly List<NotificationDto> _items = [];
    private readonly HashSet<string> _busyIds = [];

    private int UnreadCount => _items.Count(n => n.ReadAt == null);

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
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
