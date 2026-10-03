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
    public class clsVisit
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int? VisitID { get; set; }
        public int AppointmentID { get; set; }
        public clsAppointment? AppointmentInfo { get; set; }
        public DateTimeOffset CheckinTime { get; set; }
        public DateTimeOffset? CheckoutTime { get; set; }
        public string? Notes { get; set; }
        public bool IsLocked { get; set; }
        private enMode _Mode = enMode.Add;

        public clsVisit()
        {
            _Mode = enMode.Add;
        }

        private clsVisit(
            int? visitID,
            int appointmentID,
            DateTimeOffset checkinTime,
            DateTimeOffset? checkoutTime,
            string? notes,
            bool isLocked
        )
        {
            _Mode = enMode.Update;

            VisitID = visitID;
            AppointmentID = appointmentID;
            CheckinTime = checkinTime;
            CheckoutTime = checkoutTime;
            Notes = notes;
            IsLocked = isLocked;
        }

        public record VisitItem(int Id, int AppointmentID, string? PatientName, string? DoctorName, DateTimeOffset AppointmentStartAt, DateTimeOffset CheckinTime, DateTimeOffset? CheckoutTime, string? Notes);

        public static async Task<PagedResult<VisitItem>> FindAllAsync(int? doctorId = null, DateOnly? day = null, int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsVisitDA.FindAllAsync(doctorId, day, pageNumber, pageSize);

            List<VisitItem> visits = table.AsEnumerable().Select(row => new VisitItem(
                row.Field<int>("VisitID"),
                row.Field<int>("AppointmentID"),
                row.Field<string>("PatientName"),
                row.Field<string>("DoctorName"),
                row.Field<DateTimeOffset>("StartAt"),
                row.Field<DateTimeOffset>("CheckinTime"),
                row.Field<DateTimeOffset?>("CheckoutTime"),
                row.Field<string>("Notes")
            )).ToList();

            int total = table.Rows.Count == 0 ? 0 : table.Rows[0].Field<int>("TotalCount");

            return new PagedResult<VisitItem>(visits, pageNumber, pageSize, total);
        }

        public static async Task<clsVisit?> FindAsync(int? id)
        {
            DataRow? row = await clsVisitDA.FindAsync(id);

            return row == null ? null : MapVisit(row);
        }

        public static async Task<clsVisit?> FindByAppointmentIDAsync(int? id)
        {
            DataRow? row = await clsVisitDA.FindByAppointmentIDAsync(id);

            return row == null ? null : MapVisit(row);
        }

        internal static clsVisit MapVisit(DataRow row)
        {
            return new clsVisit(
                row.Field<int>("VisitID"),
                row.Field<int>("AppointmentID"),
                row.Field<DateTimeOffset>("CheckinTime"),
                row.Field<DateTimeOffset?>("CheckoutTime"),
                row.Field<string>("Notes"),
                row.Field<bool>("IsLocked")
            );
        }

        public static Task<bool> IsExistByAppointmentIDAsync(int id)
        {
            return clsVisitDA.IsExistByAppointmentIDAsync(id);
        }

        public static Task<bool> HasCheckedoutAsync(int? id)
        {
            return clsVisitDA.HasCheckedoutAsync(id);
        }

        public static Task<bool> DeleteAsync(int? id)
        {
            return clsVisitDA.DeleteAsync(id);
        }

        private async Task<bool> _CreateVisitAsync()
        {
            VisitID = await clsVisitDA.CreateAsync(AppointmentID, CheckinTime, CheckoutTime, Notes);
            return VisitID.HasValue;
        }

        private Task<bool> _UpdateVisitAsync()
        {
            return clsVisitDA.UpdateAsync(VisitID, AppointmentID, CheckinTime, CheckoutTime, Notes, IsLocked);
        }

        public async Task<bool> SaveAsync()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (await _CreateVisitAsync())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return await _UpdateVisitAsync();
            }

            return false;
        }

        public bool IsValid()
        {
            if (_Mode == enMode.Update)
            {
                bool isCheckoutExist = CheckoutTime != null;
                bool isValidCheckout = isCheckoutExist ? (CheckoutTime > CheckinTime) : true;

                return VisitID > 0 && AppointmentID > 0 && isValidCheckout;
            }

            return AppointmentID > 0;
        }
    }
}
