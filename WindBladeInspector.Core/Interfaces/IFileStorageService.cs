namespace WindBladeInspector.Core.Interfaces;

/// <summary>
/// Interface for file storage operations.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file and returns the relative web URL.
    /// </summary>
    /// <param name="stream">The file stream to save.</param>
    /// <param name="fileName">Original file name (used for extension).</param>
    /// <returns>Relative URL path to the saved file.</returns>
    Task<string> SaveFileAsync(Stream stream, string fileName);
}
