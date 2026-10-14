using BlazorBlueprint.Components;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Member sheet results always close the sheet first (dialogs render beneath
/// it) and let the opener confirm and execute here. Handled for every opener
/// so archive and delete work from the avatar and from settings alike.
/// </summary>
internal static class MemberSheetActions
{
    /// <summary>Marker distinguishing a delete request from an archive request.</summary>
    public sealed record DeleteRequest(HouseholdMemberDto Member);

    public static async Task HandleAsync(
        DialogResult result,
        MemberService members,
        DialogService dialogs,
        ToastService toasts,
        ILogger logger,
        LocalizationService loc)
    {
        if (result.Cancelled)
            return;

        if (result.GetData<DeleteRequest>() is { } delete)
            await DeleteAsync(delete.Member, members, dialogs, toasts, logger, loc);
        else if (result.GetData<HouseholdMemberDto>() is { } member)
            await ArchiveAsync(member, members, dialogs, toasts, logger, loc);
    }

    private static async Task ArchiveAsync(
        HouseholdMemberDto member,
        MemberService members,
        DialogService dialogs,
        ToastService toasts,
        ILogger logger,
        LocalizationService loc)
    {
        var confirmed = await dialogs.ConfirmAsync(
            loc["ArchiveMemberConfirm", member.DisplayName],
            loc["ArchiveMemberMessage"],
            new ConfirmDialogOptions { ConfirmText = loc["Archive"], Destructive = true });

        if (!confirmed.Confirmed)
            return;

        try
        {
            var updated = await members.UpdateMemberAsync(member.Id, member.DisplayName, member.Color, true);
            if (updated is null)
            {
                toasts.Error(loc["CouldNotArchive"]);
                return;
            }

            toasts.Success(loc["MemberArchived", member.DisplayName]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Member {MemberId} archive failed", member.Id);
            toasts.Error(loc["CouldNotArchive"]);
        }
    }

    private static async Task DeleteAsync(
        HouseholdMemberDto member,
        MemberService members,
        DialogService dialogs,
        ToastService toasts,
        ILogger logger,
        LocalizationService loc)
    {
        var confirmed = await dialogs.ConfirmAsync(
            loc["DeleteMemberConfirm", member.DisplayName],
            loc["DeleteMemberMessage"],
            new ConfirmDialogOptions { ConfirmText = loc["Delete"], Destructive = true });

        if (!confirmed.Confirmed)
            return;

        try
        {
            if (!await members.DeleteMemberAsync(member.Id))
            {
                toasts.Error(loc["CouldNotDeleteMember"]);
                return;
            }

            toasts.Success(loc["MemberDeleted", member.DisplayName]);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Member {MemberId} delete failed", member.Id);
            toasts.Error(loc["CouldNotDeleteMember"]);
        }
    }
}
