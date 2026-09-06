using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
{
    public class clsUser
    {
        public enum enMode
        {
            Add,
            Update
        }

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
        private enMode _Mode = enMode.Add;

        public clsUser()
        {
            _Mode = enMode.Add;
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
            _Mode = enMode.Update;

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

        public static DataTable FindUsers(int pageNumber = 1, int pageSize = 10)
        {
            return clsUserDA.FindUsers(pageNumber, pageSize);
        }

        public static clsUser? FindUser(int? id)
        {
            DataRow? row = clsUserDA.Find(id);

            return row == null ? null : MapUser(row);
        }

        public static clsUser? FindByEmail(string email)
        {
            DataRow? row = clsUserDA.FindByEmail(email);

            return row == null ? null : MapUser(row);
        }

        public static clsUser? FindByPersonID(int? personID)
        {
            DataRow? row = clsUserDA.FindByPersonID(personID);

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



        public static bool DeleteUser(int? id)
        {
            return clsUserDA.Delete(id);
        }

        public static bool DeactivateUser(int? id)
        {
            return clsUserDA.Deactivate(id);
        }

        public bool Deactivate()
        {
            return DeactivateUser(UserID);
        }

        public static bool ActivateUser(int? id)
        {
            return clsUserDA.Activate(id);
        }

        public bool Activate()
        {
            return ActivateUser(UserID);
        }

        private bool _CreateUser()
        {
            UserID = clsUserDA.Create(
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

        private bool _UpdateUser()
        {
            return clsUserDA.Update(UserID, PasswordHash, Role, Email, Active, PersonID, RefreshTokenHash, RefreshTokenExpiresAt, RefreshTokenRevokedAt);
        }

        public bool Save()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_CreateUser())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return _UpdateUser();
            }

            return false;
        }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(PasswordHash) || string.IsNullOrEmpty(Email))
                return false;

            if (_Mode == enMode.Update)
                return PersonID > 0;

            return PersonInfo?.IsValid() == true;
        }
    }
}
