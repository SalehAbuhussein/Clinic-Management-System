using Clinic_Business.Common;
using Clinic_DataAccess;
using System.Data;

namespace Clinic_Business.Entities
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
        public DateOnly DateOfBirth { get; set; }
        public int Age
        {
            get
            {
                return DateOnly.FromDateTime(DateTime.Today).Year - DateOfBirth.Year;
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
            DateOnly dateOfBirth,
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

        public static async Task<PagedResult<clsPerson>> FindAllAsync(string? fullname = null, string? nationalNo = null, string? phone = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = await clsPersonDA.FindAllAsync(fullname, nationalNo, phone, pageNumber, pageSize);

            List<clsPerson> people = dt.AsEnumerable().Select(MapPerson).ToList();

            int total = 0;

            if (dt.Rows.Count > 0)
                total = dt.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsPersonDA.GetTotalCount(fullname, nationalNo, phone);

            return new PagedResult<clsPerson>(people, pageNumber, pageSize, total);
        }

        public static async Task<clsPerson?> FindAsync(int? id)
        {
            DataRow? row = await clsPersonDA.FindAsync(id);

            return row is null ? null : MapPerson(row);
        }

        public virtual Task<bool> UpdateAsync()
        {
            return clsPersonDA.UpdateAsync(
                PersonID,
                Firstname,
                Midname,
                Thirdname,
                Lastname,
                NationalNo,
                Phone,
                Gender,
                DateOfBirth,
                ProfileImage);
        }

        public static Task<bool> IsUsedAsync(int id)
        {
            return clsPersonDA.IsUsedAsync(id);
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
                DateOnly.FromDateTime(row.Field<DateTime>("DateOfBirth")),
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

        public static bool IsValidDateOfBirth(DateOnly dateOfBirth)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            if (dateOfBirth > today)
                return false;

            int age = today.Year - dateOfBirth.Year;

            if (age > 100 || age < 0)
                return false;

            return true;
        }
    }
}
