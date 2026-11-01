namespace Clinic_Management_System.Services
{
    public interface IProfileImageStore
    {
        Task<string?> SaveAsync(IFormFile image, CancellationToken cancellationToken);

        void Delete(string? fileName);
    }
}