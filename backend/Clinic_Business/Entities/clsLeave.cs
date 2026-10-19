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
    public class clsLeave
    {
        public enum enMode
        {
            Add,
            Update
        }
        public enum enStatus
        {
            Pending,
            Approved,
            Rejected
        }

        public int? LeaveID { get; set; }
        public int? UserID { get; set; }
        public DateTimeOffset StartAt { get; set;  }
        public DateTimeOffset EndAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public byte Status { get; set; }
        public bool IsPending { get { return Status == 0; } }
        public bool IsApproved { get { return Status == 1; } }
        public bool IsRejected { get { return Status == 2; } }
        public clsUser UserInfo { get; set; }
        private enMode _Mode;

        public clsLeave()
        {
            _Mode = enMode.Add;
            UserInfo = new clsUser();
        }

        private clsLeave(
            int? id,
            int? userId,
            DateTimeOffset startAt,
            DateTimeOffset endAt,
            string reason,
            byte status
        )
        {
            _Mode = enMode.Update;

            LeaveID = id;
            UserID = userId;
            StartAt = startAt;
            EndAt = endAt;
            Reason = reason;
            Status = status;
            // Will be filled from the mapper
            UserInfo = new clsUser();
        }

        public record LeaveItem(int Id, string Fullname, DateTimeOffset StartAt, DateTimeOffset EndAt, string Reason, byte Status)
        {
            public string StatusText => Status switch
            {
                1 => "Approved",
                2 => "Rejected",
                _ => "Pending",
            };
        }

        public static async Task<PagedResult<LeaveItem>> FindAllLeavesAsync(
            int? userId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10
        )
        {
            DataTable table = await clsLeaveDA.FindAllAsync(userId, from, to, pageNumber, pageSize);

            List<LeaveItem> leaves = table.AsEnumerable().Select(row => new LeaveItem(
                row.Field<int>("LeaveID"),
                row.Field<string>("Fullname") ?? string.Empty,
                row.Field<DateTimeOffset>("StartAt"),
                row.Field<DateTimeOffset>("EndAt"),
                row.Field<string>("Reason") ?? string.Empty,
                row.Field<byte>("Status")
            )).ToList();

            int total = table.Rows.Count == 0 ? 0 : table.Rows[0].Field<int>("TotalCount");

            return new PagedResult<LeaveItem>(leaves, pageNumber, pageSize, total);
        }

        public static async Task<clsLeave?> FindAsync(int id)
        {
            if (id < 1)
                return null;

            DataRow? row = await clsLeaveDA.FindAsync(id);

            return row == null ? null : MapLeave(row);
        }

        public static async Task<bool> HasDoctorLeaveAsync(int? doctorId, DateTimeOffset startAt, DateTimeOffset endAt)
        {
            return await clsLeaveDA.HasDoctorLeaveAsync(doctorId, startAt, endAt);
        }

        public static async Task<bool> HasUserLeaveAsync(int? userId, DateTimeOffset startAt, DateTimeOffset endAt)
        {
            return await clsLeaveDA.HasUserLeaveAsync(userId, startAt, endAt);
        }

        private async Task<bool> _CreateAsync()
        {
            LeaveID = await clsLeaveDA.CreateAsync(UserID, StartAt, EndAt, Reason);
            return LeaveID.HasValue;
        }

        private async Task<bool> _UpdateAsync()
        {
            return await clsLeaveDA.UpdateAsync(LeaveID, Reason, Status);
        }

        public async Task<bool> SaveAsync()
        {
            switch (_Mode)
            {
                case enMode.Add:
                    if (await _CreateAsync())
                    {
                        return true;
                    }
                    return false;
                case enMode.Update:
                    return await _UpdateAsync();
            }

            return false;
        }

        internal static clsLeave MapLeave(DataRow row)
        {
            clsLeave leave = new clsLeave(
                row.Field<int>("LeaveID"),
                row.Field<int>("UserID"),
                row.Field<DateTimeOffset>("StartAt"),
                row.Field<DateTimeOffset>("EndAt"),
                row.Field<string>("Reason") ?? string.Empty,
                row.Field<byte>("Status")
            );

            leave.UserInfo = clsUser.MapUser(row);

            return leave;
        }
    }
}
