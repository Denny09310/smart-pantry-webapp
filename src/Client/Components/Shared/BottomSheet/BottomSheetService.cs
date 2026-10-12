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
/// Opens and closes one global bottom sheet from anywhere. The body is any
/// component; it closes the sheet via <see cref="CloseAsync"/> and can pass a
/// result back to whoever awaited <see cref="OpenAsync{TBody}"/>.
/// </summary>
public sealed class BottomSheetService
{
    public event Action? Changed;

    public bool IsOpen { get; private set; }
    public Type? BodyType { get; private set; }
    public Dictionary<string, object?> BodyParameters { get; private set; } = new();
    public SheetOptions Options { get; private set; } = SheetOptions.Default;

    private TaskCompletionSource<object?>? _resultSource;

    public Task<object?> OpenAsync<TBody>(
        Dictionary<string, object?>? parameters = null,
        SheetOptions? options = null) where TBody : IComponent
    {
        _resultSource?.TrySetResult(null);

        BodyType = typeof(TBody);
        BodyParameters = parameters ?? new();
        Options = options ?? SheetOptions.Default;
        IsOpen = true;

        _resultSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Changed?.Invoke();
        return _resultSource.Task;
    }

    public Task CloseAsync(object? result = null)
    {
        if (!IsOpen)
            return Task.CompletedTask;

        IsOpen = false;
        Changed?.Invoke();

        _resultSource?.TrySetResult(result);
        _resultSource = null;
        return Task.CompletedTask;
    }
}
