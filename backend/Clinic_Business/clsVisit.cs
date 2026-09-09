using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
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
        public DateTime CheckinTime { get; set; }
        public DateTime? CheckoutTime { get; set; }
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
            DateTime checkinTime,
            DateTime? checkoutTime,
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

        public static async Task<List<clsVisit>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            DataTable table = await clsVisitDA.FindAllAsync(pageNumber, pageSize);

            return table.AsEnumerable().Select(MapVisit).ToList();
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
                row.Field<DateTime>("CheckinTime"),
                row.Field<DateTime?>("CheckoutTime"),
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
