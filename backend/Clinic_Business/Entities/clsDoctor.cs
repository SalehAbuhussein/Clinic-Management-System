using Clinic_Business.Common;
using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Entities
{
    public class clsDoctor
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int? DoctorID { get; set; }
        public string Specialization { get; set; } = string.Empty;
        public string MedicalSchool { get; set; } = string.Empty;
        public ushort GraduationYear { get; set; }
        public int? UserID { get; set; }
        public clsUser UserInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        private enMode _Mode = enMode.Add;

        public clsDoctor()
        {
            _Mode = enMode.Add;
            UserInfo = new clsUser();
        }

        private clsDoctor(
            int? id,
            int userId,
            string specialization,
            string medicalSchool,
            ushort graduationYear,
            DateTime createdAt,
            DateTime updatedAt,
            int createdBy
        )
        {
            _Mode = enMode.Update;

            DoctorID = id;
            Specialization = specialization;
            MedicalSchool = medicalSchool;
            GraduationYear = graduationYear;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CreatedBy = createdBy;
            UserID = userId;
            // UserInfo is supplied by _MapDoctor from the joined row, not loaded here.
            // An empty one keeps the non-null invariant for callers that build a doctor by hand.
            UserInfo = new clsUser();
        }

        public record DoctorItem(
            int Id,
            string Fullname,
            string Firstname,
            string Midname,
            string? Thirdname,
            string Lastname,
            string NationalNo,
            string Phone,
            bool Gender,
            DateOnly DateOfBirth,
            string Specialization,
            string MedicalSchool,
            ushort GraduationYear,
            int PersonID,
            bool Active);

        public static async Task<PagedResult<DoctorItem>> FindAllDoctorsAsync(string? fullname = null, string? specialization = null, bool? active = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsDoctorDA.FindAllAsync(fullname, specialization, active, pageNumber, pageSize);

            List<DoctorItem> doctors = table.AsEnumerable().Select(row => new DoctorItem(
                row.Field<int>("DoctorID"),
                row.Field<string>("Fullname") ?? string.Empty,
                row.Field<string>("Firstname") ?? string.Empty,
                row.Field<string>("Midname") ?? string.Empty,
                row.Field<string>("Thirdname"),
                row.Field<string>("Lastname") ?? string.Empty,
                row.Field<string>("NationalNo") ?? string.Empty,
                row.Field<string>("Phone") ?? string.Empty,
                row.Field<bool>("Gender"),
                DateOnly.FromDateTime(row.Field<DateTime>("DateOfBirth")),
                row.Field<string>("Specialization") ?? string.Empty,
                row.Field<string>("MedicalSchool") ?? string.Empty,
                Convert.ToUInt16(row.Field<short>("GraduationYear")),
                row.Field<int>("PersonID"),
                row.Field<bool>("Active")
            )).ToList();

            int total = 0;

            if (table.Rows.Count > 0)
                total = table.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsDoctorDA.GetTotalCount(fullname, specialization, active);

            return new PagedResult<DoctorItem>(doctors, pageNumber, pageSize, total);
        }

        public static async Task<clsDoctor?> FindAsync(int? id)
        {
            DataRow? row = await clsDoctorDA.FindAsync(id);

            return row == null ? null : _MapDoctor(row);
        }

        /// <summary>
        /// Builds the doctor, its user and its person from the single joined row.
        /// Doctors, Users and People are one entity across three tables, so they load
        /// together; anything the doctor merely points at is loaded on request.
        /// </summary>
        private static clsDoctor _MapDoctor(DataRow row)
        {
            clsPerson person = clsPerson.MapPerson(row);

            clsUser user = new clsUser
            {
                UserID = row.Field<int>("UserID"),
                PersonID = row.Field<int>("PersonID"),
                Email = row.Field<string>("Email") ?? string.Empty,
                PasswordHash = row.Field<string>("PasswordHash") ?? string.Empty,
                Role = row.Field<string>("Role") ?? string.Empty,
                Active = row.Field<bool>("Active"),
                PersonInfo = person
            };

            return new clsDoctor(
                row.Field<int>("DoctorID"),
                row.Field<int>("UserID"),
                row.Field<string>("Specialization") ?? string.Empty,
                row.Field<string>("MedicalSchool") ?? string.Empty,
                Convert.ToUInt16(row.Field<short>("GraduationYear")),
                row.Field<DateTime>("CreatedAt"),
                row.Field<DateTime>("UpdatedAt"),
                row.Field<int>("CreatedBy")
            )
            { UserInfo = user };
        }

        public static async Task<clsDoctor?> FindByUserIDAsync(int? userID)
        {
            DataRow? row = await clsDoctorDA.FindByUserIDAsync(userID);

            return row == null ? null : _MapDoctor(row);
        }

        public static Task<bool> DeleteDoctorAsync(int? id)
        {
            return clsDoctorDA.DeleteAsync(id);
        }

        /// <summary>Exists *and* the linked user is active.</summary>
        public static async Task<bool> IsDoctorExistAsync(int? id)
        {
            clsDoctor? doctor = await FindAsync(id);
            return doctor != null && doctor.UserInfo.Active;
        }

        public static Task<bool> IsDoctorOfPatientAsync(int? patientId, int? userId)
        {
            return clsDoctorDA.IsDoctorOfPatientAsync(patientId, userId);
        }

        public static Task<bool> IsDoctorOfVisitAsync(int? visitId, int? userId)
        {
            return clsDoctorDA.IsDoctorOfVisitAsync(visitId, userId);
        }

        private async Task<bool> _CreateDoctorAsync()
        {
            (int? doctorId, int? personId, int? userId) = await clsDoctorDA.CreateAsync(
                Specialization,
                MedicalSchool,
                GraduationYear,
                CreatedBy,
                UserInfo.PersonInfo.Firstname,
                UserInfo.PersonInfo.Midname,
                UserInfo.PersonInfo.Thirdname,
                UserInfo.PersonInfo.Lastname,
                UserInfo.PersonInfo.NationalNo,
                UserInfo.PersonInfo.Phone,
                UserInfo.PersonInfo.Gender,
                UserInfo.PersonInfo.DateOfBirth,
                UserInfo.PersonInfo.ProfileImage,
                UserInfo.PasswordHash,
                UserInfo.Email
            );

            DoctorID = doctorId;
            UserID = userId;
            UserInfo.PersonID = personId;

            return doctorId.HasValue && personId.HasValue && userId.HasValue;
        }

        private Task<bool> _UpdateDoctorAsync()
        {
            return clsDoctorDA.UpdateAsync(
                DoctorID,
                UserInfo.PersonID,
                Specialization,
                MedicalSchool,
                GraduationYear,
                UserInfo.PersonInfo.Firstname,
                UserInfo.PersonInfo.Midname,
                UserInfo.PersonInfo.Thirdname,
                UserInfo.PersonInfo.Lastname,
                UserInfo.PersonInfo.Phone,
                UserInfo.PersonInfo.Gender,
                UserInfo.PersonInfo.DateOfBirth,
                UserInfo.PersonInfo.ProfileImage
            );
        }

        public async Task<bool> SaveAsync()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (await _CreateDoctorAsync())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }
                    return false;
                case enMode.Update:
                    return await _UpdateDoctorAsync();
            }

            return false;
        }

        public bool IsValid()
        {
            bool isValidPerson = UserInfo?.PersonInfo?.IsValid() == true;
            bool isValidDoctor = (!string.IsNullOrEmpty(Specialization) && !string.IsNullOrEmpty(MedicalSchool) && GraduationYear > 0);

            if (_Mode == enMode.Update)
                return isValidDoctor && isValidPerson && UserID > 0 &&  UserInfo!.PersonID > 0;

            return isValidDoctor && isValidPerson;
        }
    }
}
