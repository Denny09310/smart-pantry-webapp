using BlazorBlueprint.Components;
using Microsoft.AspNetCore.Components;

namespace Client.Services;

/// <summary>How the global bottom sheet opens.</summary>
public sealed class SheetOptions
{
    public static readonly SheetOptions Default = new();

    /// <summary>Visible heights as fractions of the viewport height.</summary>
    public double[] SnapPoints { get; init; } = [0.55, 0.92];

    public int InitialSnapIndex { get; init; } = 1;

    public bool CloseOnScrimClick { get; init; } = true;
}

/// <summary>
/// Content hosted inside the global bottom sheet.
/// </summary>
public sealed record BottomSheetEntry(
    Type ComponentType,
    Dictionary<string, object?> Parameters,
    SheetOptions Options);

/// <summary>
/// Opens and closes one global bottom sheet from anywhere. Results are
/// <see cref="DialogResult"/>-typed: bodies close with
/// <c>CloseAsync(data)</c> and openers read <c>GetData&lt;T&gt;()</c>,
/// or observe <c>Cancelled</c> for scrim/Escape/cancel dismissals.
/// Replacing an open sheet cancels the previous open.
/// </summary>
public sealed class BottomSheetService
{
    public event Action? Changed;

    public BottomSheetEntry? Current { get; private set; }

    public bool IsOpen => Current is not null;

    private TaskCompletionSource<DialogResult>? _tcs;

    public Task<DialogResult> OpenAsync<TBody>(
        Dictionary<string, object?>? parameters = null,
        SheetOptions? options = null) where TBody : IComponent
    {
        _tcs?.TrySetResult(DialogResult.Cancel());

        _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = new BottomSheetEntry(
            typeof(TBody),
            parameters is null ? [] : new Dictionary<string, object?>(parameters),
            options ?? SheetOptions.Default);
        Changed?.Invoke();

        return _tcs.Task;
    }

    public Task CloseAsync(object? data = null)
    {
        if (Current is null)
            return Task.CompletedTask;

        var tcs = _tcs;
        Current = null;
        _tcs = null;
        Changed?.Invoke();

        tcs?.TrySetResult(data is null ? DialogResult.Cancel() : DialogResult.Ok(data));
        return Task.CompletedTask;
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
