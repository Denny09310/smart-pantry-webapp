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

    public double[] SnapPoints { get; set; } = [0.5, 0.9];

    public int InitialSnapIndex { get; set; }

    public bool ShowHandle { get; set; } = true;

    public bool ShowCloseButton { get; set; } = true;

    public bool Dismissible { get; set; } = true;

    public bool DismissOnOverlayClick { get; set; } = true;

    public bool DismissOnEscape { get; set; } = true;
}

/// <summary>
/// Content hosted inside a programmatic bottom sheet.
/// </summary>
public sealed class BottomSheetEntry
{
    public BottomSheetEntry(Type componentType, IDictionary<string, object?>? parameters, BottomSheetOptions options)
    {
        ComponentType = componentType;
        Parameters = parameters;
        Options = options;
    }

    public Type ComponentType { get; }

    public IDictionary<string, object?>? Parameters { get; }

    public BottomSheetOptions Options { get; }
}

public sealed record BottomSheetSnapRequest(int Index, int Version);

/// <summary>
/// Global controller for the <c>BottomSheetProvider</c>. Mirrors the
/// <c>DialogService.OpenAsync</c> pattern: open any component in a bottom
/// sheet and await a <c>DialogResult</c>. Content components use the same
/// <c>[CascadingParameter] IDialogReference</c> pattern as dialog content:
/// <c>CloseAsync(DialogResult.Ok(data))</c> / <c>CancelAsync()</c>.
/// </summary>
public sealed class BottomSheetService
{
    private static readonly double[] DefaultSnaps = [0.5, 0.9];

    private TaskCompletionSource<DialogResult>? _tcs;
    private object? _pendingData;
    private int _snapVersion;

    public BottomSheetEntry? Current { get; private set; }

    public BottomSheetSnapRequest? PendingSnap { get; private set; }

    public bool IsOpen => Current is not null;

    public event Action? OnChange;

    public event Action? OnDismissRequested;

    public Task<DialogResult> OpenAsync<TComponent>(BottomSheetOptions? options = null)
        where TComponent : IComponent
        => OpenAsync(typeof(TComponent), null, options);

    public Task<DialogResult> OpenAsync<TComponent>(
        IDictionary<string, object?> parameters,
        BottomSheetOptions? options = null)
        where TComponent : IComponent
        => OpenAsync(typeof(TComponent), parameters, options);

    public Task<DialogResult> OpenAsync(
        Type componentType,
        IDictionary<string, object?>? parameters,
        BottomSheetOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException("Must be a Blazor component.", nameof(componentType));

        if (Current is not null)
        {
            var previous = _tcs;
            Current = null;
            _tcs = null;
            _pendingData = null;
            previous?.TrySetResult(DialogResult.Cancel());
        }

        options ??= new BottomSheetOptions();
        options.SnapPoints = NormalizeSnaps(options.SnapPoints);
        options.InitialSnapIndex = Math.Clamp(options.InitialSnapIndex, 0, options.SnapPoints.Length - 1);

        _tcs = new TaskCompletionSource<DialogResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = new BottomSheetEntry(componentType, parameters, options);
        PendingSnap = null;
        OnChange?.Invoke();

        return _tcs.Task;
    }

    /// <summary>Programmatically move the open sheet to a snap point.</summary>
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
    /// with <c>DialogResult.Cancel()</c>.
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
