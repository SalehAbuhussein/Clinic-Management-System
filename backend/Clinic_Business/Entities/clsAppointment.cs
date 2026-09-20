using Clinic_Business.Common;
using Clinic_DataAccess;
using System.Data;
using System.Linq;

namespace Clinic_Business.Entities
{
    public class clsAppointment
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int AppointmentID { get; set; }
        public int? DoctorID { get; set; }
        public clsDoctor? DoctorInfo { get; set; }
        public int PatientID { get; set; }
        public clsPatient? PatientInfo { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public byte Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public enMode Mode { get { return _Mode; } }
        private enMode _Mode = enMode.Add;

        public clsAppointment()
        {
            _Mode = enMode.Add;
        }

        private clsAppointment(
            int id, 
            int doctorId, 
            int patientID, 
            DateTime startAt, 
            DateTime endAt,
            byte status, 
            string? notes, 
            DateTime createdAt, 
            DateTime updatedAt,
            int createdBy
        )
        {
            _Mode = enMode.Update;

            AppointmentID = id;
            DoctorID = doctorId;
            PatientID = patientID;
            StartAt = startAt;
            EndAt = endAt;
            Status = status;
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CreatedBy = createdBy;
        }

        public static async Task<PagedResult<clsAppointment>> FindAllAsync(
            int? doctorId,
            int? patientId,
            DateOnly? from,
            DateOnly? to,
            int pageNumber = 1,
            int pageSize = 10)
        {
            DataTable table = await clsAppointmentDA.FindAllAsync(doctorId, patientId, from, to, pageNumber, pageSize);

            List<clsAppointment> appointments = table.AsEnumerable().Select(MapAppointment).ToList();
            int total = table.Rows.Count == 0 ? 0 : table.Rows[0].Field<int>("TotalCount");

            return new PagedResult<clsAppointment>(appointments, pageNumber, pageSize, total);
        }

        public record DoctorSchedule(int AppointmentID, DateTime StartAt, DateTime EndAt, byte Status);

        public static async Task<List<DoctorSchedule>> FindDoctorSchedulesAsync(int doctorId, DateTime from, DateTime to)
        {
            DataTable table = await clsAppointmentDA.FindDoctorSchedules(doctorId, from, to);

            return table.AsEnumerable().Select(row => new DoctorSchedule(
                row.Field<int>("AppointmentID"),
                row.Field<DateTime>("StartAt"),
                row.Field<DateTime>("EndAt"),
                row.Field<byte>("Status")
            )).ToList();
        }

        public static async Task<clsAppointment?> FindAsync(int id)
        {
            DataRow? row = await clsAppointmentDA.FindAsync(id);

            return row == null ? null : MapAppointment(row);
        }

        /// <summary>
        /// Builds an appointment from a row. Shared by the single fetch and the list, so the
        /// two can never disagree about a column name.
        /// </summary>
        internal static clsAppointment MapAppointment(DataRow row)
        {
            clsAppointment appointment = new clsAppointment(
                row.Field<int>("AppointmentID"),
                row.Field<int>("DoctorID"),
                row.Field<int>("PatientID"),
                row.Field<DateTime>("StartAt"),
                row.Field<DateTime>("EndAt"),
                row.Field<byte>("Status"),
                row.Field<string>("Notes"),
                row.Field<DateTime>("CreatedAt"),
                row.Field<DateTime>("UpdatedAt"),
                row.Field<int>("CreatedBy")
            );

            appointment.PatientInfo = _MapPatientInfo(row);
            appointment.DoctorInfo = _MapDoctorInfo(row);

            return appointment;
        }

        /// <summary>
        /// Builds the patient carried on the appointment row. Only what a list
        /// row displays is read; the emergency contacts and audit columns stay
        /// at their defaults.
        ///
        /// Deliberately not `clsPatient._MapPatient`: that reads the column
        /// names of its own query, which are not the ones in this joined row.
        /// </summary>
        private static clsPatient _MapPatientInfo(DataRow row)
        {
            return new clsPatient
            {
                PatientID = row.Field<int>("PatientID"),
                PersonID = row.Field<int>("PersonID"),
                Firstname = row.Field<string>("PatientFirstname") ?? string.Empty,
                Midname = row.Field<string>("PatientMidname") ?? string.Empty,
                Thirdname = row.Field<string?>("PatientThirdname"),
                Lastname = row.Field<string>("PatientLastname") ?? string.Empty,
            };
        }

        /// <summary>
        /// Builds the doctor carried on the appointment row. The name lives two
        /// hops away, because a doctor reaches a person through their user.
        /// </summary>
        private static clsDoctor _MapDoctorInfo(DataRow row)
        {
            return new clsDoctor
            {
                DoctorID = row.Field<int>("DoctorID"),
                UserID = row.Field<int>("UserID"),
                UserInfo = new clsUser
                {
                    PersonInfo = new clsPerson
                    {
                        Firstname = row.Field<string>("DoctorFirstname") ?? string.Empty,
                        Midname = row.Field<string>("DoctorMidname") ?? string.Empty,
                        Thirdname = row.Field<string?>("DoctorThirdname"),
                        Lastname = row.Field<string>("DoctorLastname") ?? string.Empty,
                    },
                },
            };
        }

        public static Task<bool> IsExistByIDAsync(int? id)
        {
            return clsAppointmentDA.IsExistByIDAsync(id);
        }

        public static Task<bool> DeleteAsync(int id)
        {
            return clsAppointmentDA.DeleteAsync(id);
        }

        public static Task<bool> HasDoctorExistingAppointmentAsync(int? doctorId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
        {
            return clsAppointmentDA.HasDoctorExistingAppointmentAsync(doctorId, startAt, endAt, excludeAppointmentID);
        }

        public static Task<bool> HasPatientExistingAppointmentAsync(int patientId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
        {
            return clsAppointmentDA.HasPatientExistingAppointmentAsync(patientId, startAt, endAt, excludeAppointmentID);
        }

        public static Task<bool> IsAppointmentScheduledAsync(int? id)
        {
            return clsAppointmentDA.IsAppointmentScheduledAsync(id);
        }

        public static Task<bool> IsAppointmentAttendedAsync(int? id)
        {
            return clsAppointmentDA.IsAppointmentAttendedAsync(id);
        }

        public static Task<bool> IsAppointmentCancelledAsync(int? id)
        {
            return clsAppointmentDA.IsAppointmentCancelledAsync(id);
        }

        public static Task<bool> AttendAppointmentAsync(int? id)
        {
            return clsAppointmentDA.AttendAppointmentAsync(id);
        }

        public Task<bool> AttendAppointmentAsync()
        {
            return AttendAppointmentAsync(AppointmentID);
        }

        public static Task<bool> UnattendAppointmentAsync(int? id)
        {
            return clsAppointmentDA.UnattendAppointmentAsync(id);
        }

        public Task<bool> UnattendAppointmentAsync()
        {
            return UnattendAppointmentAsync(AppointmentID);
        }

        public static Task<bool> CancelAppointmentAsync(int? id)
        {
            return clsAppointmentDA.CancelAppointmentAsync(id);
        }

        public Task<bool> CancelAppointmentAsync()
        {
            return CancelAppointmentAsync(AppointmentID);
        }

        private async Task<bool> _CreateAppointmentAsync()
        {
            int? newAppointmentID = await clsAppointmentDA.CreateAsync(DoctorID, PatientID, StartAt, EndAt, Status, Notes, CreatedBy);

            if (!newAppointmentID.HasValue)
                return false;

            AppointmentID = newAppointmentID.Value;
            return true;
        }

        private Task<bool> _UpdateAppointmentAsync()
        {
            return clsAppointmentDA.UpdateAsync(AppointmentID, DoctorID, PatientID, StartAt, EndAt, Status, Notes);
        }

        public async Task<bool> SaveAsync()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (await _CreateAppointmentAsync())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return await _UpdateAppointmentAsync();
            }

            return false;
        }

        public bool IsValid()
        {
            bool isValidRange = EndAt > StartAt;
            bool isValidStart = _Mode == enMode.Update || StartAt >= DateTime.UtcNow;

            return isValidRange && isValidStart;
        }

        public bool IsScheduled()
        {
            return Status == 0;
        }

        public bool IsAttended()
        {
            return Status == 1;
        }

        public bool IsNotAttended()
        {
            return Status == 2;
        }

        public bool IsCancelled()
        {
            return Status == 3;
        }
    }
}
