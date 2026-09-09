using Clinic_Business.Interfaces;

namespace Clinic_Business
{
    public class MedicineRepository : IMedicineRepository
    {
        public Task<clsMedicine?> FindByIdAsync(int id)
        {
            return clsMedicine.FindAsync(id);
        }

        public Task<List<clsMedicine>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            return clsMedicine.FindAllAsync(pageNumber, pageSize);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return clsMedicine.DeleteAsync(id);
        }
    }
}
