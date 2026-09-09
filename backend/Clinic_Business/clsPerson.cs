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

        public static async Task<List<clsPerson>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = await clsPersonDA.FindAllAsync(pageNumber, pageSize);

            return dt.AsEnumerable().Select(MapPerson).ToList();
        }

        public static async Task<clsPerson?> FindAsync(int? id)
        {
            DataRow? row = await clsPersonDA.FindAsync(id);

            return row is null ? null : MapPerson(row);
        }

        public static Task<bool> DeleteAsync(int? id)
        {
            return clsPersonDA.DeleteAsync(id);
        }

        /// <summary>
        /// Maps the People columns out of any joined row. Shared by every loader that
        /// brings a person along with it, so a new column is added in one place.
        /// </summary>
        internal static clsPerson MapPerson(DataRow row)
        {
            return new clsPerson(
                row.Field<int>("PersonID"),
                row.Field<string>("Firstname") ?? string.Empty,
                row.Field<string>("Midname") ?? string.Empty,
                row.Field<string>("Thirdname"),
                row.Field<string>("Lastname") ?? string.Empty,
                row.Field<string>("NationalNo") ?? string.Empty,
                row.Field<string>("Phone") ?? string.Empty,
                row.Field<bool>("Gender"),
                row.Field<DateTime>("DateOfBirth"),
                row.Field<string>("ProfileImage"));
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
