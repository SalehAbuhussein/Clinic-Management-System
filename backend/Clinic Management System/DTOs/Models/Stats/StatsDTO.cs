namespace Clinic_Management_System.DTOs.Models.Stats
{
    public class StatsDTO
    {
        public int InClinic { get; set; }
        public int AppointmentsToday { get; set; }
        public int AttendedToday { get; set; }
        public int NoShowToday { get; set; }
        public int Patients { get; set; }
        public List<int> AppointmentsByDay { get; set; } = new();
    }
}
