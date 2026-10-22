using Clinic_Business.Common;
using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Entities
{
    public class clsUser
    {
        public int? UserID { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Active { get; set; }
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public DateTime? RefreshTokenRevokedAt { get; set; }
        public int? PersonID { get; set; }
        public clsPerson PersonInfo { get; set; } = new clsPerson();

        public clsUser()
        {
            PersonInfo = new clsPerson();
        }

        private clsUser(
            int? id,
            string passwordHash,
            string role,
            string email,
            bool active,
            string? refreshTokenHash,
            DateTime? refreshTokenExpiresAt,
            DateTime? refreshTokenRevokedAt,
            int? personID
        )
        {

            UserID = id;
            PasswordHash = passwordHash;
            Role = role;
            Email = email;
            Active = active;
            RefreshTokenHash = refreshTokenHash;
            RefreshTokenExpiresAt = refreshTokenExpiresAt;
            RefreshTokenRevokedAt = refreshTokenRevokedAt;
            PersonID = personID;

            // PersonInfo comes from MapUser's joined row, not a second query here.
            // An empty one keeps the non-null invariant for users built by hand.
            PersonInfo = new clsPerson();
        }

        public static async Task<PagedResult<UserItem>> FindUsersAsync(string? fullname = null, string? email = null, bool? active = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsUserDA.FindUsersAsync(fullname, email, active, pageNumber, pageSize);

            List<UserItem> users = table.AsEnumerable().Select(row => new UserItem(
                row.Field<int>("UserID"),
                row.Field<string>("Fullname") ?? string.Empty,
                row.Field<string>("Email") ?? string.Empty,
                row.Field<string>("Role") ?? string.Empty,
                row.Field<bool>("Active")
            )).ToList();

            int total = 0;

            if (table.Rows.Count > 0)
                total = table.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsUserDA.GetTotalCount(fullname, email, active);

            return new PagedResult<UserItem>(users, pageNumber, pageSize, total);
        }

        public static async Task<clsUser?> FindUserAsync(int? id)
        {
            DataRow? row = await clsUserDA.Find(id);

            return row == null ? null : MapUser(row);
        }

        public static async Task<clsUser?> FindByEmailAsync(string email)
        {
            DataRow? row = await clsUserDA.FindByEmailAsync(email);

            return row == null ? null : MapUser(row);
        }

        public static async Task<clsUser?> FindByPersonIDAsync(int? personID)
        {
            DataRow? row = await clsUserDA.FindByPersonIDAsync(personID);

            return row == null ? null : MapUser(row);
        }

        /// <summary>
        /// Builds the user and its person from the single joined row. Users and People are
        /// one entity across two tables, so they load together. Internal because clsDoctor
        /// maps the same columns out of its own three-table join.
        /// </summary>
        internal static clsUser MapUser(DataRow row)
        {
            clsPerson person = clsPerson.MapPerson(row);

            return new clsUser(
                row.Field<int>("UserID"),
                row.Field<string>("PasswordHash") ?? string.Empty,
                row.Field<string>("Role") ?? string.Empty,
                row.Field<string>("Email") ?? string.Empty,
                row.Field<bool>("Active"),
                row.Field<string>("RefreshTokenHash"),
                row.Field<DateTime?>("RefreshTokenExpiresAt"),
                row.Field<DateTime?>("RefreshTokenRevokedAt"),
                row.Field<int>("PersonID")
            )
            { PersonInfo = person };
        }



        public static Task<bool> DeleteUser(int? id)
        {
            return clsUserDA.Delete(id);
        }

        public static Task<bool> DeactivateUser(int? id)
        {
            return clsUserDA.Deactivate(id);
        }

        public Task<bool> Deactivate()
        {
            return DeactivateUser(UserID);
        }

        public static Task<bool> ActivateUser(int? id)
        {
            return clsUserDA.Activate(id);
        }

        public Task<bool> Activate()
        {
            return ActivateUser(UserID);
        }

        private async Task<bool> _CreateUser()
        {
            UserID = await clsUserDA.Create(
                PersonInfo.Firstname,
                PersonInfo.Midname,
                PersonInfo.Thirdname,
                PersonInfo.Lastname,
                PersonInfo.NationalNo,
                PersonInfo.Phone,
                PersonInfo.Gender,
                PersonInfo.DateOfBirth,
                PersonInfo.ProfileImage,
                PasswordHash,
                Email,
                RefreshTokenHash,
                RefreshTokenExpiresAt,
                RefreshTokenRevokedAt
            );
            return UserID.HasValue;
        }

        private Task<bool> _UpdateUser()
        {
            return clsUserDA.Update(UserID, PasswordHash, Role, Email, Active, PersonID, RefreshTokenHash, RefreshTokenExpiresAt, RefreshTokenRevokedAt);
        }

        public async Task<bool> Create()
        {
            if (!_HasCredintals() || PersonInfo?.IsValid() != true)
                return false;

            if (UserID.HasValue)
                return false;

            return await _CreateUser();
        }

        public async Task<bool> Update()
        {
            if (!_HasCredintals() || !(PersonID > 0))
                return false;

            return await _UpdateUser();
        }

        private bool _HasCredintals() => !string.IsNullOrEmpty(PasswordHash) && !string.IsNullOrEmpty(Email);

        public record UserItem(int Id, string Fullname, string Email, string Role, bool Active);
    }
}
