using System.Reflection;
using Bit.Butil;
using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components;

namespace Client.Services;

/// <summary>
/// Options controlling how a bottom sheet opens. Snap points ("stop points")
/// are fractions of the viewport height, e.g. 0.5 = half the screen.
/// </summary>
public sealed class BottomSheetOptions
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public double[] SnapPoints { get; set; } = [0.5, 0.9];

    public int InitialSnapIndex { get; set; }

    public bool ShowHandle { get; set; } = true;

    public bool ShowCloseButton { get; set; } = true;

    /// <summary>CSS max-width of the sheet (sets <c>--bs-max-width</c>).</summary>
    public string? MaxWidth { get; set; }

    public bool Dismissible { get; set; } = true;

    public bool DismissOnOverlayClick { get; set; } = true;

    public bool DismissOnEscape { get; set; } = true;
}

/// <summary>
/// Content hosted inside a programmatic bottom sheet.
/// </summary>
public sealed class BottomSheetEntry(Type componentType, IDictionary<string, object?>? parameters, BottomSheetOptions options)
{
    public Type ComponentType { get; } = componentType;

    public IDictionary<string, object?>? Parameters { get; } = parameters;

    public BottomSheetOptions Options { get; } = options;
}

public sealed record BottomSheetSnapRequest(int Index, int Version);

/// <summary>
/// Global controller for the <c>BottomSheetProvider</c>. Adaptive entry point:
/// <c>OpenAsync</c> takes <c>DialogOpenOptions</c> and shows a bottom sheet on
/// mobile, a Blueprint dialog on desktop. Content components use the same
/// <c>[CascadingParameter] IDialogReference</c> pattern in both hosts:
/// <c>CloseAsync(DialogResult.Ok(data))</c> / <c>CancelAsync()</c>.
/// <c>SnapToAsync</c> / <c>CloseAsync</c> only apply to an open sheet and are
/// no-ops while a desktop dialog is showing.
/// </summary>
public sealed class BottomSheetService(
    DialogService dialog,
    UserAgent userAgent)
{
    private static readonly double[] DefaultSnaps = [0.5, 0.9];

    private static readonly MethodInfo OpenDialogMethod =
        typeof(BottomSheetService).GetMethod(nameof(OpenDialogAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private TaskCompletionSource<DialogResult>? _tcs;
    private object? _pendingData;
    private int _snapVersion;
    private bool _dialogOpen;

    public BottomSheetEntry? Current { get; private set; }

    public BottomSheetSnapRequest? PendingSnap { get; private set; }

    public bool IsOpen => Current is not null || _dialogOpen;

    public event Action? OnChange;

    public event Action? OnDismissRequested;

    public Task<DialogResult> OpenAsync<TComponent>(DialogOpenOptions? options = null)
        where TComponent : IComponent
        => OpenAsync(typeof(TComponent), null, options);

    public Task<DialogResult> OpenAsync<TComponent>(
        IDictionary<string, object?> parameters,
        DialogOpenOptions? options = null)
        where TComponent : IComponent
        => OpenAsync(typeof(TComponent), parameters, options);

    /// <summary>
    /// Bottom sheet on mobile, Blueprint dialog on desktop (via Bit.Butil
    /// <c>UserAgent.IsMobile</c>, evaluated at open time; without a JS
    /// runtime it reads false, so desktop dialog).
    /// </summary>
    public async Task<DialogResult> OpenAsync(
        Type componentType,
        IDictionary<string, object?>? parameters,
        DialogOpenOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException("Must be a Blazor component.", nameof(componentType));

        options ??= new DialogOpenOptions();

        if (await userAgent.IsMobile().ConfigureAwait(false))
            return await OpenSheetAsync(componentType, parameters, options).ConfigureAwait(false);

        return await ((Task<DialogResult>)OpenDialogMethod
            .MakeGenericMethod(componentType)
            .Invoke(this, [parameters, options])!).ConfigureAwait(false);
    }

    private async Task<DialogResult> OpenDialogAsync<TComponent>(
        IDictionary<string, object?>? parameters,
        DialogOpenOptions options)
        where TComponent : IComponent
    {
        _dialogOpen = true;
        OnChange?.Invoke();

        try
        {
            return parameters is null
                ? await dialog.OpenAsync<TComponent>(options).ConfigureAwait(false)
                : await dialog.OpenAsync<TComponent>(new Dictionary<string, object?>(parameters), options).ConfigureAwait(false);
        }
        finally
        {
            _dialogOpen = false;
            OnChange?.Invoke();
        }
    }

    private Task<DialogResult> OpenSheetAsync(
        Type componentType,
        IDictionary<string, object?>? parameters,
        DialogOpenOptions options)
    {
        if (Current is not null)
        {
            var previous = _tcs;
            Current = null;
            _tcs = null;
            _pendingData = null;
            previous?.TrySetResult(DialogResult.Cancel());
        }

        var sheetOptions = ToSheetOptions(options);
        sheetOptions.SnapPoints = NormalizeSnaps(sheetOptions.SnapPoints);
        sheetOptions.InitialSnapIndex = Math.Clamp(sheetOptions.InitialSnapIndex, 0, sheetOptions.SnapPoints.Length - 1);

        _tcs = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = new BottomSheetEntry(componentType, parameters, sheetOptions);
        PendingSnap = null;
        OnChange?.Invoke();

        return _tcs.Task;
    }

    private static BottomSheetOptions ToSheetOptions(DialogOpenOptions options) => new()
    {
        Title = options.Title,
        Description = options.Description,
        ShowCloseButton = options.ShowClose,
        Dismissible = !options.PreventClose,
        DismissOnOverlayClick = !options.PreventClose,
        DismissOnEscape = !options.PreventClose,
        MaxWidth = options.Size switch
        {
            DialogSize.Small => "24rem",
            DialogSize.Default => "32rem",
            DialogSize.Large => "36rem",
            DialogSize.ExtraLarge => "42rem",
            DialogSize.Full => "100%",
            _ => null,
        },
    };

    /// <summary>Programmatically move the open sheet to a snap point. No-op on desktop.</summary>
    public Task SnapToAsync(int index)
    {
        if (Current is null)
            return Task.CompletedTask;

        index = Math.Clamp(index, 0, Current.Options.SnapPoints.Length - 1);
        PendingSnap = new BottomSheetSnapRequest(index, ++_snapVersion);
        OnChange?.Invoke();

        return Task.CompletedTask;
    }

    public void AcknowledgeSnap(int version)
    {
        if (PendingSnap?.Version == version)
            PendingSnap = null;
    }

    /// <summary>
    /// Close with the exit animation, completing the <c>OpenAsync</c> task
    /// with <c>DialogResult.Ok(data)</c>. Pull-down/overlay/Escape complete
    /// with <c>DialogResult.Cancel()</c>. Sheet only; no-op while a desktop
    /// dialog is showing.
    /// </summary>
    public Task CloseAsync(object? data = null)
    {
        if (Current is null)
            return Task.CompletedTask;

        _pendingData = data;
        OnDismissRequested?.Invoke();

        return Task.CompletedTask;
    }

    /// <summary>Called by the provider once the sheet finished dismissing.</summary>
    public void NotifyDismissed()
    {
        if (Current is null)
            return;

        var tcs = _tcs;
        var data = _pendingData;

        Current = null;
        _tcs = null;
        _pendingData = null;
        PendingSnap = null;

        tcs?.TrySetResult(data is null ? DialogResult.Cancel() : DialogResult.Ok(data));
        OnChange?.Invoke();
    }

    private static double[] NormalizeSnaps(double[]? snaps)
    {
        if (snaps is null || snaps.Length == 0)
            return DefaultSnaps;

        var normalized = snaps
            .Select(s => Math.Clamp(s, 0.15, 1.0))
            .OrderBy(s => s)
            .ToArray();

        return normalized.Length == 0 ? DefaultSnaps : normalized;
    }
}

public static class BottomSheetServiceExtensions
{
    public static IServiceCollection AddBottomSheet(this IServiceCollection services)
    {
        services.AddScoped<BottomSheetService>();
        return services;
    }
}
