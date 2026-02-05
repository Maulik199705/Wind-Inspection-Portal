using WindBladeInspector.Core.Interfaces;

namespace WindBladeInspector.Infrastructure;

/// <summary>
/// Local file storage implementation that saves files to wwwroot/blade-images.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(string webRootPath)
    {
        _basePath = Path.Combine(webRootPath, "blade-images");
        
        // Ensure directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> SaveFileAsync(Stream stream, string fileName)
    {
        // Generate unique filename using GUID
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_basePath, uniqueName);

        // Save file
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await stream.CopyToAsync(fileStream);
        }

        // Return relative web URL
        return $"/blade-images/{uniqueName}";
    }
}
