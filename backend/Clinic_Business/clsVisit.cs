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

        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            return clsVisitDA.FindAll(pageNumber, pageSize);
        }

        public static clsVisit? Find(int? id)
        {
            int appointmentID = int.MinValue;
            DateTime checkinTime = DateTime.MinValue;
            DateTime? checkoutTime = null;
            string? notes = null;
            bool isLocked = false;

            if (clsVisitDA.Find(id, ref appointmentID, ref checkinTime, ref checkoutTime, ref notes, ref isLocked))
            {
                return new clsVisit(id, appointmentID, checkinTime, checkoutTime, notes, isLocked);
            }

            return null;
        }

        public static clsVisit? FindByAppointmentID(int id)
        {
            int visitID = int.MinValue;
            DateTime checkinTime = DateTime.MinValue;
            DateTime? checkoutTime = null;
            string? notes = null;
            bool isLocked = false;

            if (clsVisitDA.FindByAppointmentID(id, ref visitID, ref checkinTime, ref checkoutTime, ref notes, ref isLocked))
            {
                return new clsVisit(visitID, id, checkinTime, checkoutTime, notes, isLocked);
            }

            return null;
        }

        public static bool IsExistByAppointmentID(int id)
        {
            return clsVisitDA.IsExistByAppointmentID(id);
        }

        public static bool HasCheckedout(int? id)
        {
            return clsVisitDA.HasCheckedout(id);
        }

        public static bool Delete(int? id)
        {
            return clsVisitDA.Delete(id);
        }

        private bool _CreateVisit()
        {
            VisitID = clsVisitDA.Create(AppointmentID, CheckinTime, CheckoutTime, Notes);
            return VisitID.HasValue;
        }

        private bool _UpdateVisit()
        {
            return clsVisitDA.Update(VisitID, AppointmentID, CheckinTime, CheckoutTime, Notes, IsLocked);
        }

        public bool Save()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_CreateVisit())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return _UpdateVisit();
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
