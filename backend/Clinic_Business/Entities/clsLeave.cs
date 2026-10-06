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

        public int? LeaveID { get; set; }
        public int? UserID { get; set; }
        public DateTimeOffset StartAt { get; set;  }
        public DateTimeOffset EndAt { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
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
            bool isApproved
        )
        {
            _Mode = enMode.Update;

            LeaveID = id;
            UserID = userId;
            StartAt = startAt;
            EndAt = endAt;
            Reason = reason;
            IsApproved = isApproved;
            // Will be filled from the mapper
            UserInfo = new clsUser();
        }

        public record LeaveItem(int Id, DateTimeOffset StartAt, DateTimeOffset EndAt, string Reason, bool IsApproved);

        public static async Task<PagedResult<LeaveItem>> FindAllLeavesAsync(
            DateTimeOffset? from, 
            DateTimeOffset? to,
            int pageNumber = 1,
            int pageSize = 10
        ) 
        {
            DataTable table = await clsLeaveDA.FindAllAsync(from, to, pageNumber, pageSize);

            List<LeaveItem> leaves = table.AsEnumerable().Select(row => new LeaveItem(
                row.Field<int>("LeaveID"),
                row.Field<DateTimeOffset>("StartAt"),
                row.Field<DateTimeOffset>("EndAt"),
                row.Field<string>("Reason") ?? string.Empty,
                row.Field<bool>("Status")
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

        private async Task<bool> _CreateAsync()
        {
            LeaveID = await clsLeaveDA.CreateAsync(UserID, StartAt, EndAt, Reason);
            return LeaveID.HasValue;
        }

        private async Task<bool> _UpdateAsync()
        {
            return await clsLeaveDA.UpdateAsync(LeaveID, Reason);
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
                row.Field<bool>("Status")
            );

            leave.UserInfo = clsUser.MapUser(row);

            return leave;
        }
    }
}
