namespace Clinic_Management_System.Services
{
    /// <summary>
    /// Keeps the profile pictures on disk under wwwroot, and hands back the
    /// file name the Person row stores.
    ///
    /// The client's own file name is never used. A caller could send
    /// "..\..\appsettings.json" or a name that collides with someone else's
    /// picture, so every file is renamed to a fresh GUID and the extension
    /// comes from the bytes rather than from what the client claimed.
    ///
    /// Deleting never throws. The row is the record of what exists; a file
    /// that is already gone, locked, or renamed by hand is a log line, not a
    /// failed request.
    /// </summary>
    public class ProfileImageStore : IProfileImageStore
    {
        private const string Folder = "uploads/profiles";

        public const long MaxBytes = 500 * 1024; // 500KB

        private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileImageStore> _logger;

        public ProfileImageStore(IWebHostEnvironment environment, ILogger<ProfileImageStore> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string?> SaveAsync(IFormFile image, CancellationToken cancellationToken)
        {
            if (image.Length > MaxBytes)
                return null;

            byte[] header = new byte[12];

            await using (Stream reading = image.OpenReadStream())
            {
                if (await reading.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken) < header.Length)
                    return null;
            }

            string? extension = _ExtensionOf(header);

            if (extension is null)
                return null;

            string fileName = $"{Guid.NewGuid():N}{extension}";

            Directory.CreateDirectory(FolderPath());

            await using FileStream writing = File.Create(Path.Combine(FolderPath(), fileName));
            await using Stream source = image.OpenReadStream();

            await source.CopyToAsync(writing, cancellationToken);

            return fileName;
        }

        public void Delete(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
                return;

            try
            {
                File.Delete(Path.Combine(FolderPath(), fileName));
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not delete the profile picture {FileName}", fileName);
            }
        }

        private string FolderPath() => Path.Combine(_environment.WebRootPath, Folder);

        private static string? _ExtensionOf(ReadOnlySpan<byte> header)
        {
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return ".jpg";

            if (header[..8].SequenceEqual(PngHeader))
                return ".png";

            if (header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
                return ".webp";

            return null;
        }
    }
}
