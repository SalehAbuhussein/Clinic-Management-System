using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
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

        public static DataTable FindAllDoctors(int pageNumber = 1, int pageSize = 10)
        {
            return clsDoctorDA.FindAll(pageNumber, pageSize);
        }

        public static clsDoctor? Find(int? id)
        {
            DataRow? row = clsDoctorDA.Find(id);

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

        public static clsDoctor? FindByUserID(int? userID)
        {
            DataRow? row = clsDoctorDA.FindByUserID(userID);

            return row == null ? null : _MapDoctor(row);
        }

        public static bool DeleteDoctor(int? id)
        {
            return clsDoctorDA.Delete(id);
        }

        public static bool IsDoctorExist(int? id)
        {
            clsDoctor? doctor = Find(id);
            return doctor != null && doctor.UserInfo.Active;
        }

        public static bool IsDoctorOfVisit(int? visitId, int? userId)
        {
            return clsDoctorDA.IsDoctorOfVisit(visitId, userId);
        }

        private bool _CreateDoctor()
        {
            (int? doctorId, int? personId, int? userId) = clsDoctorDA.Create(
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

        private bool _UpdateDoctor()
        {
            return clsDoctorDA.Update(
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

        public bool Save()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_CreateDoctor())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }
                    return false;
                case enMode.Update:
                    return _UpdateDoctor();
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
