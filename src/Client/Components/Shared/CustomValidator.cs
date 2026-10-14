using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Client.Components;

/// <summary>
/// Displays server-side (400 ProblemDetails) errors inside an EditForm.
/// Usage: place next to DataAnnotationsValidator, keep a @ref, call
/// DisplayErrors after a failed save.
/// </summary>
public class CustomValidator : ComponentBase
{
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    private ValidationMessageStore? _messageStore;

    protected override void OnInitialized()
    {
        _messageStore = new ValidationMessageStore(EditContext!);
        EditContext!.OnValidationRequested += (_, _) => _messageStore.Clear();
        EditContext!.OnFieldChanged += (_, e) => _messageStore.Clear(e.FieldIdentifier);
    }

    public void DisplayErrors(Dictionary<string, List<string>> errors)
    {
        foreach (var (field, messages) in errors)
        {
            foreach (var message in messages)
                _messageStore!.Add(EditContext!.Field(field), message);
        }

        EditContext!.NotifyValidationStateChanged();
    }
}