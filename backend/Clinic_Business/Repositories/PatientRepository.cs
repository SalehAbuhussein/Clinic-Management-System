using Clinic_Business.Common;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        public Task<clsPatient?> FindByIdAsync(int? id)
        {
            return clsPatient.FindPatientAsync(id);
        }

        public Task<clsPatient?> FindByNationalNoAsync(string nationalNo)
        {
            return clsPatient.FindPatientByNationalNoAsync(nationalNo);
        }

        public Task<PagedResult<clsPatient.PatientItem>> FindAllAsync(string? fullname = null, string? phone = null, int pageNumber = 1, int pageSize = 10)
        {
            return clsPatient.FindPatientsAsync(fullname, phone, pageNumber, pageSize);
        }

        public Task<bool> DeleteAsync(int? id)
        {
            return clsPatient.DeletePatientAsync(id);
        }

        // Named Exists rather than IsPatientExist: inside IPatientRepository the "Patient"
        // is redundant. Note it means exists AND is active.
        public Task<bool> ExistsAsync(int? id)
        {
            return clsPatient.IsPatientExistAsync(id);
        }
    }
}
