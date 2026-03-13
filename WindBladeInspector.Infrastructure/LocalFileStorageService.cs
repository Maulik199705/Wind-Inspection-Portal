using WindBladeInspector.Core.Interfaces;

namespace WindBladeInspector.Infrastructure;

/// <summary>
/// Saves uploaded blade images to a persistent directory outside wwwroot.
/// Images are served via the /blade-images static files middleware.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageDir;

    /// <param name="storageDir">Absolute path to write files (e.g. App_Data/blade-images).</param>
    /// <param name="webRootPath">Kept for interface compatibility; not used for writing.</param>
    public LocalFileStorageService(string storageDir, string webRootPath = "")
    {
        _storageDir = storageDir;
        Directory.CreateDirectory(_storageDir);
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_storageDir, uniqueName);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await stream.CopyToAsync(fileStream);

        // URL path served by UseStaticFiles middleware
        return $"/blade-images/{uniqueName}";
    }
}