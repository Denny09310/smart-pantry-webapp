using Bit.Butil;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Household member state: list, current selection (persisted in
/// localStorage), and management. Attribution only — never access control.
/// </summary>
internal sealed class MemberService(ServerApi api, LocalStorage storage, ILogger<MemberService> log)
{
    private const string StorageKey = "smart-pantry.member-id";

    private readonly List<HouseholdMemberDto> _members = [];
    private bool _initialized;
    private bool _creating;

    public IReadOnlyList<HouseholdMemberDto> Members => _members;

    public IEnumerable<HouseholdMemberDto> ActiveMembers => _members.Where(m => !m.IsArchived);

    public string? CurrentMemberId { get; private set; }

    public HouseholdMemberDto? CurrentMember =>
        _members.FirstOrDefault(m => m.Id == CurrentMemberId && !m.IsArchived);

    public bool LoadFailed { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            using var response = await api.Members.GetAsync();

            if (!response.IsSuccessfulWithContent)
            {
                LoadFailed = true;
                return;
            }

            _members.AddRange(response.Content);

            string? stored = null;
            try
            {
                stored = await storage.GetItem(StorageKey);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Member selection restore failed.");
            }

            CurrentMemberId = _members.Any(m => m.Id == stored && !m.IsArchived)
                ? stored
                : ActiveMembers.FirstOrDefault()?.Id;

            _initialized = true;
        }
        catch (Exception ex)
        {
            LoadFailed = true;
            log.LogWarning(ex, "Member initialization failed.");
        }

        Changed?.Invoke();
    }

    public async Task SelectMemberAsync(string id)
    {
        if (!_members.Any(m => m.Id == id && !m.IsArchived))
            return;

        CurrentMemberId = id;
        await PersistAsync();
        Changed?.Invoke();
    }

    public async Task<HouseholdMemberDto?> CreateMemberAsync(string name, string? color)
    {
        if (_creating)
            return null;

        _creating = true;

        try
        {
            HouseholdMemberDto? created = null;

            try
            {
                using var response = await api.Members.CreateAsync(new CreateHouseholdMemberRequest(name, color));
                if (response.IsSuccessfulWithContent)
                    created = response.Content;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Member creation failed.");
            }

            if (created is null)
                return null;

            _members.Add(created);
            CurrentMemberId = created.Id;
            await PersistAsync();
            Changed?.Invoke();

            return created;
        }
        finally
        {
            _creating = false;
        }
    }

    public async Task<HouseholdMemberDto?> UpdateMemberAsync(string id, string name, string? color, bool archived)
    {
        HouseholdMemberDto? updated = null;

        try
        {
            using var response = await api.Members.UpdateAsync(id, new UpdateHouseholdMemberRequest(name, color, archived));
            if (response.IsSuccessfulWithContent)
                updated = response.Content;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Member update failed.");
        }

        if (updated is null)
            return null;

        var index = _members.FindIndex(m => m.Id == id);
        if (index >= 0)
            _members[index] = updated;

        if (CurrentMemberId == id && (archived || !_members.Any(m => m.Id == id && !m.IsArchived)))
        {
            CurrentMemberId = ActiveMembers.FirstOrDefault()?.Id;
            await PersistAsync();
        }

        Changed?.Invoke();
        return updated;
    }

    private async Task PersistAsync()
    {
        try
        {
            if (CurrentMemberId is null)
                await storage.RemoveItem(StorageKey);
            else
                await storage.SetItem(StorageKey, CurrentMemberId);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Member selection persistence failed.");
        }
    }
}