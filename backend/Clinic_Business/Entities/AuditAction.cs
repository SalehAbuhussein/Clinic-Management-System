using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Entities
{
    public static class AuditAction
    {
        public const string LoginSucceeded = "LoginSucceeded";
        public const string LoginFailed = "LoginFailed";
        public const string RefreshRejected = "RefreshRejected";
        public const string UserCreated = "UserCreated";
        public const string UserRoleChanged = "UserRoleChanged";
        public const string UserDeactivated = "UserDeactivated";
        public const string UserActivated = "UserActivated";
        public const string PasswordChanged = "PasswordChanged";
        public const string PasswordReset = "PasswordReset";
        public const string AccessDenied = "AccessDenied";
    }
}
