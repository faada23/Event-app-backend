using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Options;

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;

    public FileStorageService(IOptions<FileStorageOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options), "FileStorageOptions cannot be null.");
        if (string.IsNullOrEmpty(_options.BasePath))
        {
            throw new ArgumentException("BasePath in FileStorageOptions cannot be null or empty.", nameof(options));
        }
    }

    public async Task<string?> SaveFileAsync(IFormFile file, string subDirectory)
    {   

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty or null.", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid()}{extension}";
        
        var relativeDirectoryPath = subDirectory.TrimStart('/');
        var relativeFilePath = Path.Combine(relativeDirectoryPath, fileName).Replace("\\", "/");

        var absoluteDirectoryPath = Path.Combine(_options.BasePath, relativeDirectoryPath);
        var absoluteFilePath = Path.Combine(absoluteDirectoryPath, fileName);

        if (!Directory.Exists(absoluteDirectoryPath))
        {
            Directory.CreateDirectory(absoluteDirectoryPath);
        }

        await using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
    
        return "/" + relativeFilePath;
    }

    public bool DeleteFile(string relativePath)
    {   
       
        if (string.IsNullOrEmpty(relativePath))
        {
            return true;
        }

        var absolutePath = Path.Combine(_options.BasePath, relativePath.TrimStart('/'));

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return true;
    }
}