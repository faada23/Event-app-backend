using Microsoft.AspNetCore.Http;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subDirectory, CancellationToken cancellationToken);
    bool DeleteFile(string relativePath);
}