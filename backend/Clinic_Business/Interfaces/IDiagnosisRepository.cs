namespace Clinic_Business.Interfaces
{
    public interface IDiagnosisRepository
    {
        Task<clsDiagnosis?> FindByIdAsync(int id);
        Task<clsDiagnosis?> FindByVisitIdAsync(int? visitId);
        Task<bool> ExistsByVisitIdAsync(int visitId);
        Task<bool> DeleteAsync(int id);
    }
}
