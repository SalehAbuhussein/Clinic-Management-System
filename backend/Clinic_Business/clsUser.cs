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

            PersonInfo = clsPerson.Find(PersonID) ?? new clsPerson();
        }

        public static DataTable FindUsers(int pageNumber = 1, int pageSize = 10)
        {
            return clsUserDA.FindUsers(pageNumber, pageSize);
        }

        public static clsUser? FindUser(int? id)
        {
            string passwordHash = string.Empty;
            string role = string.Empty;
            string email = string.Empty;
            bool active = false;
            int personID = int.MinValue;
            string? refreshTokenHash = null;
            DateTime? refreshTokenExpiresAt = null;
            DateTime? refreshTokenRevokedAt = null;

            if (clsUserDA.Find(
                id, 
                ref passwordHash, 
                ref role, 
                ref email, 
                ref active, 
                ref personID,
                ref refreshTokenHash,
                ref refreshTokenExpiresAt,
                ref refreshTokenRevokedAt
                )
            )
            {
                return new clsUser(
                    id,
                    passwordHash,
                    role,
                    email,
                    active,
                    refreshTokenHash,
                    refreshTokenExpiresAt,
                    refreshTokenRevokedAt,
                    personID
                );
            }

            return null;
        }

        public static clsUser? FindByEmail(string email)
        {
            string passwordHash = string.Empty;
            string role = string.Empty;
            bool active = false;
            int personID = int.MinValue;
            int userID = int.MinValue;
            string? refreshTokenHash = null;
            DateTime? refreshTokenExpiresAt = null;
            DateTime? refreshTokenRevokedAt = null;

            if (clsUserDA.FindByEmail(
                email, 
                ref passwordHash, 
                ref role, 
                ref userID, 
                ref active, 
                ref personID,
                ref refreshTokenHash,
                ref refreshTokenExpiresAt,
                ref refreshTokenRevokedAt
                )
            )
            {
                clsPerson? person = clsPerson.Find(personID);

                if (person != null)
                {
                    return new clsUser(
                        userID,
                        passwordHash,
                        role,
                        email,
                        active,
                        refreshTokenHash,
                        refreshTokenExpiresAt,
                        refreshTokenRevokedAt,
                        personID
                    );
                }
            }

            return null;
        }

        public static clsUser? FindByPersonID(int? personID)
        {
            string passwordHash = string.Empty;
            string role = string.Empty;
            bool active = false;
            string email = string.Empty;
            int userID = int.MinValue;
            string? refreshTokenHash = null;
            DateTime? refreshTokenExpiresAt = null;
            DateTime? refreshTokenRevokedAt = null;

            if (clsUserDA.FindByPersonID(
                personID,
                ref passwordHash,
                ref role,
                ref userID,
                ref active,
                ref email,
                ref refreshTokenHash,
                ref refreshTokenExpiresAt,
                ref refreshTokenRevokedAt
                )
            )
            {
                return new clsUser(
                    userID,
                    passwordHash,
                    role,
                    email,
                    active,
                    refreshTokenHash,
                    refreshTokenExpiresAt,
                    refreshTokenRevokedAt,
                    personID
                );
            }

            return null;
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
