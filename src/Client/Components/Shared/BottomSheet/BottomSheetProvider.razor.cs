using BlazorBlueprint.Components;

using Client.Services;

namespace Client.Components.Shared;

public partial class BottomSheetProvider : IAsyncDisposable
{
    private BottomSheet? _sheet;
    private IDialogReference _dialogRef = default!;

    protected override void OnInitialized()
    {
        _dialogRef = new BottomSheetDialogReference(Service);
        Service.OnChange += OnServiceChanged;
        Service.OnDismissRequested += OnDismissRequested;
    }

    private async void OnServiceChanged()
    {
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async void OnDismissRequested()
    {
        try
        {
            if (_sheet is not null)
                await _sheet.DismissAsync();
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private Task OnSheetDismissed()
    {
        Service.NotifyDismissed();
        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Service.OnChange -= OnServiceChanged;
        Service.OnDismissRequested -= OnDismissRequested;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Adapts the bottom sheet service to <see cref="IDialogReference"/>, so
    /// sheet content uses the exact same pattern as Blueprint dialog content.
    /// </summary>
    private sealed class BottomSheetDialogReference(BottomSheetService service) : IDialogReference
    {
        public Task CloseAsync(DialogResult result)
            => service.CloseAsync(result.Cancelled ? null : result.GetData<object>());

        public Task CancelAsync() => service.CloseAsync();
    }
}