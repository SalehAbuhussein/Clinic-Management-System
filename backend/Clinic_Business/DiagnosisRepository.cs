using Clinic_Business.Interfaces;

namespace Clinic_Business
{
    public class DiagnosisRepository : IDiagnosisRepository
    {
        public Task<clsDiagnosis?> FindByIdAsync(int id) => clsDiagnosis.FindAsync(id);

        public Task<clsDiagnosis?> FindByVisitIdAsync(int? visitId) => clsDiagnosis.FindByVisitIDAsync(visitId);

        public Task<bool> ExistsByVisitIdAsync(int visitId) => clsDiagnosis.IsExistByVisitIDAsync(visitId);

        public Task<bool> DeleteAsync(int id) => clsDiagnosis.DeleteAsync(id);
    }
}
