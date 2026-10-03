using Clinic_Business.Entities;
using Clinic_Business.Interfaces;

namespace Clinic_Business.Repositories
{
    public class StatsRepository : IStatsRepository
    {
        public Task<clsStats.ClinicStats> FindAsync(DateTimeOffset todayFrom, DateTimeOffset from, int days) => clsStats.FindAsync(todayFrom, from, days);
    }
}
