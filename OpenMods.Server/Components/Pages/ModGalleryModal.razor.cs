using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OpenMods.Shared.Models;
using OpenMods.Shared.Services;

namespace OpenMods.Server.Components.Pages
{
    public partial class ModGalleryModal
    {
        [Parameter] public bool Show { get; set; }
        [Parameter] public EventCallback OnClose { get; set; }
        [Parameter] public Mod? Mod { get; set; }
        [Parameter] public EventCallback OnSaved { get; set; }

        [Inject] public GitHubService GitHubService { get; set; } = default!;
        [Inject] public ModService ModService { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        private List<GitHubContent> _repoImages = new();
        private List<string> _selectedUrls = new();
        private bool _loading;
        private bool _saving;

        protected override async Task OnParametersSetAsync()
        {
            if (Show && Mod != null && !(_repoImages?.Any() ?? false))
            {
                _selectedUrls = Mod.GalleryImageUrls?.ToList() ?? new();
                await LoadRepoImages();
            }
        }

        private async Task LoadRepoImages()
        {
            if (Mod == null) return;
            _loading = true;
            
            try 
            {
                if (string.IsNullOrEmpty(Mod.GitHubRepoUrl))
                {
                    Console.WriteLine("Warning: Mod GitHubRepoUrl is null or empty.");
                    return;
                }
                var uri = new Uri(Mod.GitHubRepoUrl);
                var fullName = uri.AbsolutePath.TrimStart('/').TrimEnd('/');
                
                var content = await GitHubService.GetRepositoryContent(fullName, ".openmods/img");
                _repoImages = content
                    .Where(c => c.Type == "file" && (c.Name.EndsWith(".png") || c.Name.EndsWith(".jpg") || c.Name.EndsWith(".jpeg") || c.Name.EndsWith(".webp")))
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading gallery images: {ex.Message}");
            }
            finally
            {
                _loading = false;
            }
        }

        private void ToggleImage(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            
            if (_selectedUrls.Contains(url))
                _selectedUrls.Remove(url);
            else
                _selectedUrls.Add(url);
        }

        private void MoveLeft(string url)
        {
            var index = _selectedUrls.IndexOf(url);
            if (index > 0)
            {
                _selectedUrls.RemoveAt(index);
                _selectedUrls.Insert(index - 1, url);
            }
        }

        private void MoveRight(string url)
        {
            var index = _selectedUrls.IndexOf(url);
            if (index < _selectedUrls.Count - 1)
            {
                _selectedUrls.RemoveAt(index);
                _selectedUrls.Insert(index + 1, url);
            }
        }

        private async Task SaveGallery()
        {
            if (Mod == null) return;
            _saving = true;
            
            var success = await ModService.UpdateModGallery(Mod.Id, _selectedUrls);
            if (success)
            {
                await OnSaved.InvokeAsync();
                await Close();
            }
            
            _saving = false;
        }

        private async Task SetThumbnail(string? url)
        {
            if (Mod == null || string.IsNullOrEmpty(url)) return;
            
            var success = await ModService.UpdateModThumbnail(Mod.Id, url);
            if (success)
            {
                Mod.ImageUrl = url;
                StateHasChanged();
                await OnSaved.InvokeAsync();
            }
        }

        private async Task Close()
        {
            await OnClose.InvokeAsync();
        }
    }
}
