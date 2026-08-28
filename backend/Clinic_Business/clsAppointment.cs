using Clinic_DataAccess;
using System.Data;
using System.Linq;

namespace Clinic_Business
{
    public class clsAppointment
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int AppointmentID { get; set; }
        public int DoctorID { get; set; }
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
            DoctorInfo = clsDoctor.Find(doctorId);
            PatientID = patientID;
            PatientInfo = clsPatient.FindPatient(patientID);
            StartAt = startAt;
            EndAt = endAt;
            Status = status;
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CreatedBy = createdBy;
        }

        public static DataTable FindAll(
            int? doctorId,
            int? patientId,
            DateTime? date,
            int pageNumber = 1,
            int pageSize = 10)
        {
            return clsAppointmentDA.FindAll(doctorId, patientId, date, pageNumber, pageSize);
        }

        public static clsAppointment? Find(int id)
        {
            int doctorID = int.MinValue;
            int patientID = int.MinValue;
            DateTime startAt = DateTime.MinValue;
            DateTime endAt = DateTime.MinValue;
            byte status = byte.MinValue;
            string? notes = null;
            DateTime createdAt = DateTime.MinValue;
            DateTime updatedAt = DateTime.MinValue;
            int createdBy = int.MinValue;

            if (clsAppointmentDA.Find(id, ref doctorID, ref patientID, ref startAt, ref endAt, ref status, ref notes, ref createdAt, ref updatedAt, ref createdBy))
            {
                return new clsAppointment(id, doctorID, patientID, startAt, endAt, status, notes, createdAt, updatedAt, createdBy);
            }

            return null;
        } 

        public static bool IsExistByID(int? id)
        {
            return clsAppointmentDA.IsExistByID(id);
        }

        public static bool Delete(int id)
        {
            return clsAppointmentDA.Delete(id);
        }

        public static bool HasDoctorExistingAppointment(int doctorId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
        {
            return clsAppointmentDA.HasDoctorExistingAppointment(doctorId, startAt, endAt, excludeAppointmentID);
        }

        public static bool HasPatientExistingAppointment(int patientId, DateTime startAt, DateTime endAt, int? excludeAppointmentID = null)
        {
            return clsAppointmentDA.HasPatientExistingAppointment(patientId, startAt, endAt, excludeAppointmentID);
        }

        public static bool IsAppointmentScheduled(int? id)
        {
            return clsAppointmentDA.IsAppointmentScheduled(id);
        }

        public static bool IsAppointmentAttended(int? id)
        {
            return clsAppointmentDA.IsAppointmentAttended(id);
        }

        public static bool IsAppointmentCancelled(int? id)
        {
            return clsAppointmentDA.IsAppointmentCancelled(id);
        }

        public static bool AttendAppointment(int? id)
        {
            return clsAppointmentDA.AttendAppointment(id);
        }

        public bool AttendAppointment()
        {
            return AttendAppointment(AppointmentID);
        }

        public static bool UnattendAppointment(int? id)
        {
            return clsAppointmentDA.UnattendAppointment(id);
        }

        public bool UnattendAppointment()
        {
            return UnattendAppointment(AppointmentID);
        }

        public static bool CancelAppointment(int? id)
        {
            return clsAppointmentDA.CancelAppointment(id);
        }

        public bool CancelAppointment()
        {
            return CancelAppointment(AppointmentID);
        }

        private bool _CreateAppointment()
        {
            int? newAppointmentID = clsAppointmentDA.Create(DoctorID, PatientID, StartAt, EndAt, Status, Notes, CreatedBy);

            if (!newAppointmentID.HasValue)
                return false;

            AppointmentID = newAppointmentID.Value;
            return true;
        }

        private bool _UpdateAppointment()
        {
            return clsAppointmentDA.Update(AppointmentID, DoctorID, PatientID, StartAt, EndAt, Status, Notes);
        }

        public bool Save()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_CreateAppointment())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return _UpdateAppointment();
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
