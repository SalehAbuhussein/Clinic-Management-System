using Clinic_DataAccess;
using System.Data;

namespace Clinic_Business
{
    public class clsPerson
    {
        public int? PersonID { get; set; }
        public string Firstname { get; set; } = string.Empty;
        public string Midname { get; set; } = string.Empty;
        public string? Thirdname { get; set; }
        public string Lastname { get; set; } = string.Empty;
        public string NationalNo { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age
        {
            get
            {
                return DateTime.Now.Year - DateOfBirth.Year;
            }
        }
        public string Fullname {
            get
            {
                return string.Join(" ", new[] { Firstname, Midname, Thirdname, Lastname }.Where(item => !string.IsNullOrEmpty(item)));
            } 
        }
        public string? ProfileImage { get; set; }

        public clsPerson()
        {
        }

        private clsPerson(
            int? Id,
            string firstname,
            string midname,
            string? thirdname,
            string lastname,
            string nationalNo,
            string phone,
            bool gender,
            DateTime dateOfBirth,
            string? profileImage
        )
        {
            PersonID = Id;
            Firstname = firstname;
            Midname = midname;
            Thirdname = thirdname;
            Lastname = lastname;
            NationalNo = nationalNo;
            Phone = phone;
            Gender = gender;
            DateOfBirth = dateOfBirth;
            ProfileImage = profileImage;
        }

        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            return clsPersonDA.FindAll(pageNumber, pageSize);
        }

        public static clsPerson? Find(int? id)
        {
            string firstname = string.Empty;
            string midname = string.Empty;
            string? thirdname = null;
            string lastname = string.Empty;
            string phone = string.Empty;
            string nationalNo = string.Empty;
            bool gender = false;
            DateTime dateofBirth = DateTime.MinValue;
            string? profileImage = null;

            if (clsPersonDA.Find(id, ref firstname, ref midname, ref thirdname, ref lastname, ref nationalNo, ref phone, ref gender, ref dateofBirth, ref profileImage))
            {
                return new clsPerson(id, firstname, midname, thirdname, lastname, nationalNo, phone, gender, dateofBirth, profileImage);
            }
            else
            {
                return null;
            }
        }

        public static bool Delete(int? id)
        {
            return clsPersonDA.Delete(id);
        }

        /// <summary>
        /// Maps the People columns out of any joined row. Shared by every loader that
        /// brings a person along with it, so a new column is added in one place.
        /// </summary>
        internal static clsPerson MapPerson(DataRow row)
        {
            return new clsPerson
            {
                PersonID = row.Field<int>("PersonID"),
                Firstname = row.Field<string>("Firstname") ?? string.Empty,
                Midname = row.Field<string>("Midname") ?? string.Empty,
                Thirdname = row.Field<string>("Thirdname"),
                Lastname = row.Field<string>("Lastname") ?? string.Empty,
                NationalNo = row.Field<string>("NationalNo") ?? string.Empty,
                Phone = row.Field<string>("Phone") ?? string.Empty,
                Gender = row.Field<bool>("Gender"),
                DateOfBirth = row.Field<DateTime>("DateOfBirth"),
                ProfileImage = row.Field<string>("ProfileImage")
            };
        }

        public bool IsValid()
        {
            return this != null && PersonValidators.ValidateFields(this);
        }
    }

    public static class PersonValidators
    {
        public static bool ValidateFields(clsPerson person)
        {
            return (
                !string.IsNullOrEmpty(person.Firstname) &&
                !string.IsNullOrEmpty(person.Midname) &&
                !string.IsNullOrEmpty(person.Lastname) &&
                !string.IsNullOrEmpty(person.Phone) &&
                !string.IsNullOrEmpty(person.NationalNo) &&
                IsValidDateOfBirth(person.DateOfBirth)
            );
        }

        public static bool IsValidDateOfBirth(DateTime dateOfBirth)
        {
            if (dateOfBirth.Date > DateTime.Today)
                return false;

            int age = DateTime.Today.Year - dateOfBirth.Year;

            if (age > 100 || age < 0)
                return false;

            return true;
        }
    }
}
