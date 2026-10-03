using Clinic_Business.Entities;

namespace Clinic_Business.Interfaces
{
    public interface IStatsRepository
    {
        Task<clsStats.ClinicStats> FindAsync(DateTimeOffset todayFrom, DateTimeOffset from, int days);
    }
}
