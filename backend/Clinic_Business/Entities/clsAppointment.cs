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
        public int? VisitID { get; set; }
        public clsVisit? VisitInfo { get; set; }
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public byte Status { get; set; }
        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case 1: return "Attended";
                    case 2: return "Not attended";
                    case 3: return "Cancelled";
                    default: return "Scheduled";
                }
            }
        }
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
            DateTimeOffset startAt, 
            DateTimeOffset endAt,
            byte status, 
            string? notes, 
            DateTime createdAt, 
            DateTime updatedAt,
            int createdBy,
            int? visitId
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
            VisitID = visitId;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CreatedBy = createdBy;
        }

        public static Task<int> GetTotalCountAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null) => clsAppointmentDA.GetTotalCount(doctorId, patientId, from, to);

        public static async Task<PagedResult<clsAppointment>> FindAllAsync(
            int? doctorId,
            int? patientId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10)
        {
            DataTable table = await clsAppointmentDA.FindAllAsync(doctorId, patientId, from, to, pageNumber, pageSize);

            List<clsAppointment> appointments = table.AsEnumerable().Select(MapAppointment).ToList();
            int total = 0;

            if (table.Rows.Count > 0)
                total = table.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsAppointmentDA.GetTotalCount(doctorId, patientId, from, to);

            return new PagedResult<clsAppointment>(appointments, pageNumber, pageSize, total);
        }

        public record DoctorSchedule(int AppointmentID, int DoctorID, int PatientID, DateTimeOffset StartAt, DateTimeOffset EndAt, byte Status);

        public static async Task<PagedResult<DoctorSchedule>> FindDoctorSchedulesAsync(int doctorId, DateTimeOffset from, DateTimeOffset to, int pageNumber, int pageSize)
        {
            DataTable table = await clsAppointmentDA.FindDoctorSchedules(doctorId, from, to, pageNumber, pageSize);

            List<DoctorSchedule> schedules = table.AsEnumerable().Select(row => new DoctorSchedule(
                row.Field<int>("AppointmentID"),
                row.Field<int>("DoctorID"),
                row.Field<int>("PatientID"),
                row.Field<DateTimeOffset>("StartAt"),
                row.Field<DateTimeOffset>("EndAt"),
                row.Field<byte>("Status")
            )).ToList();

            int total = 0;

            if (table.Rows.Count > 0)
                total = table.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsAppointmentDA.GetDoctorSchedulesTotalCount(doctorId, from, to);

            return new PagedResult<DoctorSchedule>(schedules, pageNumber, pageSize, total);
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
                row.Field<DateTimeOffset>("StartAt"),
                row.Field<DateTimeOffset>("EndAt"),
                row.Field<byte>("Status"),
                row.Field<string>("Notes"),
                row.Field<DateTime>("CreatedAt"),
                row.Field<DateTime>("UpdatedAt"),
                row.Field<int>("CreatedBy"),
                row.Field<int?>("VisitID")
            );

            appointment.PatientInfo = MapPatientInfo(row);
            appointment.DoctorInfo = MapDoctorInfo(row);

            return appointment;
        }

        internal static clsPatient MapPatientInfo(DataRow row)
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

        internal static clsDoctor MapDoctorInfo(DataRow row)
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

        public static Task<bool> HasDoctorExistingAppointmentAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null)
        {
            return clsAppointmentDA.HasDoctorExistingAppointmentAsync(doctorId, startAt, endAt, excludeAppointmentID);
        }

        public static Task<bool> HasPatientExistingAppointmentAsync(int patientId, DateTimeOffset startAt, DateTimeOffset endAt, int? excludeAppointmentID = null)
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
            bool isValidStart = _Mode == enMode.Update || StartAt >= DateTimeOffset.UtcNow;

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
