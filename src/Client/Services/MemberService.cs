using Bit.Butil;

using Shared.Models;

namespace Client.Services;

/// <summary>
/// Household member state: list, current selection (persisted in
/// localStorage), and CRUD. Attribution only — never access control.
/// </summary>
internal sealed class MemberService(ServerApi api, LocalStorage storage, ILogger<MemberService> log)
{
    private const string StorageKey = "smart-pantry.member-id";

    private readonly List<MemberDto> _members = [];
    private bool _initialized;

    public IReadOnlyList<MemberDto> Members => _members;

    public string? CurrentMemberId { get; private set; }

    public MemberDto? CurrentMember => _members.FirstOrDefault(m => m.Id == CurrentMemberId);

    public bool IsReady => _initialized;

    public bool LoadFailed { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
            return;

        try
        {
            using var response = await api.Members.GetAsync();

            if (response.IsSuccessfulWithContent)
                _members.AddRange(response.Content);

            var stored = await storage.GetItem(StorageKey);
            CurrentMemberId = _members.Any(m => m.Id == stored)
                ? stored
                : _members.FirstOrDefault()?.Id;

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
        if (!_members.Any(m => m.Id == id))
            return;

        CurrentMemberId = id;
        await PersistAsync();
        Changed?.Invoke();
    }

    private bool _creating;

    public async Task<MemberDto?> CreateMemberAsync(string name, string? color)
    {
        if (_creating)
            return null;

        _creating = true;

        try
        {
            return await CreateMemberInnerAsync(name, color);
        }
        finally
        {
            _creating = false;
        }
    }

    private async Task<MemberDto?> CreateMemberInnerAsync(string name, string? color)
    {
        MemberDto? created = null;

        try
        {
            using var response = await api.Members.CreateAsync(new CreateMemberRequest(name, color));

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

    public async Task DeleteMemberAsync(string id)
    {
        try
        {
            using var response = await api.Members.DeleteAsync(id);

            if (!response.IsSuccessStatusCode)
                return;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Member deletion failed.");
            return;
        }

        _members.RemoveAll(m => m.Id == id);

        if (CurrentMemberId == id)
        {
            CurrentMemberId = _members.FirstOrDefault()?.Id;
            await PersistAsync();
        }

        Changed?.Invoke();
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
