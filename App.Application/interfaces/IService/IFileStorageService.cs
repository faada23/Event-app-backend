using Microsoft.AspNetCore.Http;

public interface IFileStorageService
{
    Task<Result<string>> SaveFileAsync(IFormFile file, string subDirectory);
    Result<bool> DeleteFile(string relativePath);
}