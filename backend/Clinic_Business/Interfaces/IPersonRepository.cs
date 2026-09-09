namespace Clinic_Business.Interfaces
{
    public interface IPersonRepository
    {
        Task<clsPerson?> FindByIdAsync(int? id);
        Task<List<clsPerson>> FindAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<bool> DeleteAsync(int? id);
    }
}
