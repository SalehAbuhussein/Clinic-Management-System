using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
{
    public class clsPatient : clsPerson
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int? PatientID { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public bool Active { get; set; }
        private enMode _Mode = enMode.Add;

        public clsPatient()
        {
            _Mode = enMode.Add;
        }

        private clsPatient(
            int? id, 
            string? emergencyContactNumber, 
            string? emergencyContactName,
            DateTime createdAt,
            DateTime updatedAt,
            int createdBy,
            bool active,
            int personID,
            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string phone,
            bool gender,
            DateTime dateOfBirth,
            string? profileImage,
            string nationalNo
        )
        {
            _Mode = enMode.Update;

            PatientID = id;
            EmergencyContactNumber = emergencyContactNumber;
            EmergencyContactName = emergencyContactName;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            CreatedBy = createdBy;
            PersonID = personID;
            Active = active;

            base.PersonID = personID;
            base.Firstname = firstname;
            base.Midname = midname;
            base.Thirdname = thirdname;
            base.Lastname = lastname;
            base.Phone = phone;
            base.Gender = gender;
            base.DateOfBirth = dateOfBirth;
            base.ProfileImage = profileImage;
            base.NationalNo = nationalNo;
        }

        public static DataTable FindPatients(int pageNumber = 1, int pageSize = 10)
        {
            return clsPatientDA.FindAll(pageNumber, pageSize);
        }


        public static clsPatient? FindPatient(int? id)
        {
            DataRow? row = clsPatientDA.Find(id);

            return row == null ? null : _MapPatient(row);
        }

        public static clsPatient? FindPatientByNationalNo(string nationalNo)
        {
            DataRow? row = clsPatientDA.FindByNationalNo(nationalNo);

            return row == null ? null : _MapPatient(row);
        }

        /// <summary>
        /// Builds the patient from the single joined row. clsPatient inherits clsPerson
        /// rather than holding one, so the person columns go into the base properties.
        /// </summary>
        private static clsPatient _MapPatient(DataRow row)
        {
            return new clsPatient(
                row.Field<int>("PatientID"),
                row.Field<string>("EmergencyContactNumber"),
                row.Field<string>("EmergencyContactName"),
                row.Field<DateTime>("CreatedAt"),
                row.Field<DateTime>("UpdatedAt"),
                row.Field<int>("CreatedBy"),
                row.Field<bool>("Active"),
                row.Field<int>("PersonID"),
                row.Field<string>("Firstname") ?? string.Empty,
                row.Field<string>("Midname") ?? string.Empty,
                row.Field<string>("Thirdname"),
                row.Field<string>("Lastname") ?? string.Empty,
                row.Field<string>("Phone") ?? string.Empty,
                row.Field<bool>("Gender"),
                row.Field<DateTime>("DateOfBirth"),
                row.Field<string>("ProfileImage"),
                row.Field<string>("NationalNo") ?? string.Empty
            );
        }
        
        
        public static bool DeletePatient(int? id)
        {
            return clsPatientDA.Delete(id);
        }

        public static bool IsPatientExist(int? id)
        {
            clsPatient? patient = FindPatient(id);

            return patient != null && patient.Active;
        }

        private bool _CreatePatient()
        {
            PatientID = clsPatientDA.Create(
                EmergencyContactNumber, 
                EmergencyContactName, 
                CreatedAt, 
                UpdatedAt,
                CreatedBy,
                Firstname,
                Midname,
                Thirdname,
                Lastname,
                NationalNo,
                Phone,
                Gender,
                DateOfBirth,
                ProfileImage
            );
            return PatientID.HasValue;
        }

        private bool _UpdatePatient()
        {
            return clsPatientDA.Update(
                PatientID, 
                EmergencyContactNumber, 
                EmergencyContactName, 
                PersonID,
                Firstname,
                Midname,
                Thirdname,
                Lastname,
                Phone,
                Gender,
                DateOfBirth,
                ProfileImage
            );
        }

        public bool Save()
        {
            if (!base.IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_CreatePatient())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }
                    return false;
                case enMode.Update:
                    return _UpdatePatient();
            }

            return false;
        }
    }
}
