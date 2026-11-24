using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Domain.Exceptions;

namespace ClaimsPortal.Claims.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(
        Guid claimId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(originalFileName);
        var relative = Path.Combine(claimId.ToString("N"), $"{Guid.NewGuid():N}{extension}");
        var fullPath = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
        return relative.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storedFileName, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storedFileName));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("The document path is not valid.");
        }

        if (!File.Exists(fullPath))
        {
            throw new DomainValidationException("The stored document file is missing.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }
}
