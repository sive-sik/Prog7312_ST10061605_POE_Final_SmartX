namespace SmartX.Api.Services;

public class AttachmentStore
{
    private readonly IWebHostEnvironment _environment;

    public AttachmentStore(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(Guid sensorId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("The uploaded file is empty.");

        const long maxFileSize = 10 * 1024 * 1024;

        if (file.Length > maxFileSize)
            throw new ArgumentException("File size cannot exceed 10 MB.");

        string[] allowedExtensions =
        {
            ".txt", ".log", ".json", ".csv",
            ".jpg", ".jpeg", ".png", ".pdf"
        };

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Unsupported file type.");

        string uploadRoot = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "Uploads",
            sensorId.ToString());

        Directory.CreateDirectory(uploadRoot);

        string safeFileName = $"{Guid.NewGuid()}{extension}";
        string fullPath = Path.Combine(uploadRoot, safeFileName);

        await using FileStream stream = new(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await file.CopyToAsync(stream);

        return safeFileName;
    }
}