namespace Clinic_Management_System.DTOs.Models.WorkingDays
{
    public class WorkingDayDTO
    {
        public int Id { get; set; }
        public string WorkingDay { get; set; } = string.Empty;
        public TimeSpan? StartAt { get; set; }
        public TimeSpan? EndAt { get; set; }
        public bool IsWorkingDay { get; set; }
    }
}
