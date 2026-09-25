namespace Clinic_Management_System.DTOs.Models.Appointments
{
    public class AppointmentScheduleDTO
    {
        public int AppointmentID { get; set; }
        public int DoctorID { get; set; }
        public int PatientID { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public byte Status { get; set; }
    }
}
