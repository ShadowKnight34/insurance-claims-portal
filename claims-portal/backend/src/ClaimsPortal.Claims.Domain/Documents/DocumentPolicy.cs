using ClaimsPortal.Claims.Domain.Exceptions;

namespace ClaimsPortal.Claims.Domain.Documents;

public static class DocumentPolicy
{
    public const long MaxBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };

    public static void EnsureValid(string fileName, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainValidationException("A file name is required.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainValidationException("The uploaded file is empty.");
        }

        if (sizeBytes > MaxBytes)
        {
            throw new DomainValidationException("Documents must be 10 MB or smaller.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new DomainValidationException("Allowed document types are PDF, JPG, PNG, DOC and DOCX.");
        }
    }
}
