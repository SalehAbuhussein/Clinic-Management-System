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
            // a doctor with no user row is broken data: keep the invariant and let IsValid reject it

            UserInfo = clsUser.FindUser(userId) ?? new clsUser();
        }

        public static DataTable FindAllDoctors(int pageNumber = 1, int pageSize = 10)
        {
            return clsDoctorDA.FindAll(pageNumber, pageSize);
        }

        public static clsDoctor? Find(int? id)
        {
            string specialization = string.Empty;
            string medicalSchool = string.Empty;
            ushort graduationYear = ushort.MinValue;
            DateTime createdAt = DateTime.MinValue;
            DateTime updatedAt = DateTime.MinValue;
            int createdBy = int.MinValue;
            int userId = int.MinValue;


            if (clsDoctorDA.Find(
                id, 
                ref userId, 
                ref specialization, 
                ref medicalSchool, 
                ref graduationYear, 
                ref createdAt, 
                ref updatedAt, 
                ref createdBy)
            )
            {
                return new clsDoctor(id, userId, specialization, medicalSchool, graduationYear, createdAt, updatedAt, createdBy);
            }

            return null;
        }

        public static clsDoctor? FindByUserID(int userID)
        {
            string specialization = string.Empty;
            string medicalSchool = string.Empty;
            ushort graduationYear = ushort.MinValue;
            DateTime createdAt = DateTime.MinValue;
            DateTime updatedAt = DateTime.MinValue;
            int createdBy = int.MinValue;
            int doctorID = int.MinValue;

            if (clsDoctorDA.FindByUserID(
                userID,
                ref doctorID,
                ref specialization,
                ref medicalSchool,
                ref graduationYear,
                ref createdAt,
                ref updatedAt,
                ref createdBy)
            )
            {
                return new clsDoctor(doctorID, userID, specialization, medicalSchool, graduationYear, createdAt, updatedAt, createdBy);
            }

            return null;
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
