using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TPromptHelper.Core.Interfaces;
using TPromptHelper.Core.Models;

namespace TPromptHelper.Desktop.ViewModels;

public partial class ModelSettingsViewModel(IModelProfileRepository repo, IEncryptionService encryption) : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ModelProfile> _profiles = [];
    [ObservableProperty] private ModelProfile? _editingProfile;
    [ObservableProperty] private string _rawApiKey = string.Empty;

    public string[] Providers { get; } = ["openai", "anthropic", "qwen", "kimi", "ollama", "custom"];
    public string[] Currencies { get; } = ["¥", "$"];

    public async Task LoadAsync()
    {
        var profiles = await repo.GetAllAsync();
        Profiles = new ObservableCollection<ModelProfile>(profiles);
    }

    [RelayCommand]
    private void NewProfile()
    {
        EditingProfile = new ModelProfile { Provider = "openai", ModelName = "gpt-4o" };
        RawApiKey = string.Empty;
    }

    [RelayCommand]
    private void EditProfile(ModelProfile profile)
    {
        EditingProfile = profile;
        RawApiKey = string.IsNullOrEmpty(profile.EncryptedApiKey) ? string.Empty
            : encryption.Decrypt(profile.EncryptedApiKey);
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (EditingProfile is null) return;
        if (!string.IsNullOrWhiteSpace(RawApiKey))
            EditingProfile.EncryptedApiKey = encryption.Encrypt(RawApiKey);

        await repo.SaveAsync(EditingProfile);
        if (!Profiles.Contains(EditingProfile))
            Profiles.Add(EditingProfile);
        EditingProfile = null;
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(ModelProfile profile)
    {
        Profiles.Remove(profile);
        await repo.DeleteAsync(profile.Id);
    }
}
