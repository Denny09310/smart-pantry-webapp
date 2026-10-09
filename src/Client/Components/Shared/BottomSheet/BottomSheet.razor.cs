using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Client.Components.Shared;

public partial class BottomSheet : IAsyncDisposable
{
    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Snap ("stop") points as fractions of the viewport height.</summary>
    [Parameter]
    public double[] SnapPoints { get; set; } = [0.5, 0.9];

    [Parameter]
    public int InitialSnapIndex { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Description { get; set; }

    /// <summary>CSS max-width of the sheet (sets <c>--bs-max-width</c>).</summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    [Parameter]
    public bool ShowHandle { get; set; } = true;

    [Parameter]
    public bool ShowCloseButton { get; set; } = true;

    [Parameter]
    public bool Dismissible { get; set; } = true;

    [Parameter]
    public bool DismissOnOverlayClick { get; set; } = true;

    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<int> OnSnapChanged { get; set; }

    [Parameter]
    public EventCallback OnDismissed { get; set; }

    private readonly string _instanceId = Guid.NewGuid().ToString("N");

    private ElementReference _sheetRef;
    private ElementReference _overlayRef;
    private ElementReference _handleRef;

    private BottomSheetInterop? _interop;
    private DotNetObjectReference<BottomSheet>? _dotNetRef;

    private bool _visible;
    private bool _attached;
    private bool _dismissed;
    private bool _closeRequested;
    private int _snapIndex;

    protected override void OnParametersSet()
    {
        if (IsOpen && (!_visible || _dismissed))
        {
            _dismissed = false;
            _visible = true;
            _attached = false;
            _closeRequested = false;
            _snapIndex = Math.Clamp(InitialSnapIndex, 0, Math.Max(0, SnapPoints.Length - 1));
        }
        else if (!IsOpen && _visible && !_dismissed)
        {
            _closeRequested = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_visible)
            return;

        if (_closeRequested && !_dismissed)
        {
            _closeRequested = false;
            await DismissAsync();
            return;
        }

        if (!_attached)
        {
            _attached = true;
            _interop ??= new BottomSheetInterop(JS);
            _dotNetRef ??= DotNetObjectReference.Create(this);

            await _interop.AttachAsync(
                _instanceId,
                _sheetRef,
                _overlayRef,
                _handleRef,
                _dotNetRef,
                new BottomSheetJsOptions
                {
                    SnapPoints = SnapPoints,
                    InitialSnap = _snapIndex,
                    Dismissible = Dismissible,
                    OverlayDismiss = DismissOnOverlayClick,
                    EscapeDismiss = DismissOnEscape,
                });
        }
    }

    /// <summary>Animate the sheet to a snap point.</summary>
    public async Task SetSnapAsync(int index)
    {
        if (!_attached || _interop is null)
            return;

        await _interop.SetSnapAsync(_instanceId, index, true);
    }

    /// <summary>Play the exit animation, then raise <see cref="OnDismissed"/>.</summary>
    public async Task DismissAsync()
    {
        if (_dismissed)
            return;

        if (!_attached || _interop is null)
        {
            await CompleteDismissAsync();
            return;
        }

        await _interop.DismissAsync(_instanceId);
    }

    [JSInvokable]
    public async Task OnSheetEvent(string type, int snapIndex)
    {
        if (type == "settled")
        {
            _snapIndex = snapIndex;
            await InvokeAsync(async () =>
            {
                await OnSnapChanged.InvokeAsync(snapIndex);
                StateHasChanged();
            });
        }
        else if (type == "dismissed")
        {
            await InvokeAsync(CompleteDismissAsync);
        }
    }

    private async Task CompleteDismissAsync()
    {
        if (_dismissed)
            return;

        _dismissed = true;
        _visible = false;

        if (IsOpenChanged.HasDelegate)
            await IsOpenChanged.InvokeAsync(false);

        await OnDismissed.InvokeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        try
        {
            if (_interop is not null)
            {
                await _interop.DetachAsync(_instanceId);
                await _interop.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }

        _dotNetRef?.Dispose();
    }
}