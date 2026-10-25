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
        public int? VisitID { get; set; }
        public int AppointmentID { get; set; }
        public clsAppointment? AppointmentInfo { get; set; }
        public DateTimeOffset CheckinTime { get; set; }
        public DateTimeOffset? CheckoutTime { get; set; }
        public string? Notes { get; set; }
        public bool IsLocked { get; set; }
        public int? PatientID { get; set; }
        public clsPatient? PatientInfo { get; set; }
        public int? DoctorID { get; set; }
        public clsDoctor? DoctorInfo { get; set; }
        public DateTimeOffset? AppointmentStartAt { get; set; }

        public clsVisit()
        {
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
            VisitID = visitID;
            AppointmentID = appointmentID;
            CheckinTime = checkinTime;
            CheckoutTime = checkoutTime;
            Notes = notes;
            IsLocked = isLocked;
        }

        public record VisitItem(int Id, int AppointmentID, string? PatientName, string? DoctorName, DateTimeOffset AppointmentStartAt, DateTimeOffset CheckinTime, DateTimeOffset? CheckoutTime, string? Notes);

        public static Task<int> GetTotalCountAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default) => clsVisitDA.GetTotalCount(doctorId, patientId, from, to, cancellationToken);

        public static async Task<PagedResult<VisitItem>> FindAllAsync(int? doctorId = null, int? patientId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            DataTable table = await clsVisitDA.FindAllAsync(doctorId, patientId, from, to, pageNumber, pageSize, cancellationToken);

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

            int total = 0;

            if (table.Rows.Count > 0)
                total = table.Rows[0].Field<int>("TotalCount");
            else if (pageNumber > 1)
                total = await clsVisitDA.GetTotalCount(doctorId, patientId, from, to, cancellationToken);

            return new PagedResult<VisitItem>(visits, pageNumber, pageSize, total);
        }

        public static async Task<clsVisit?> FindAsync(int? id)
        {
            DataRow? row = await clsVisitDA.FindAsync(id);

            return row == null ? null : _MapVisitWithPeople(row);
        }

        public static async Task<clsVisit?> FindByAppointmentIDAsync(int? id)
        {
            DataRow? row = await clsVisitDA.FindByAppointmentIDAsync(id);

            return row == null ? null : _MapVisitWithPeople(row);
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

        internal static clsVisit _MapVisitWithPeople(DataRow row)
        {
            clsVisit visit = MapVisit(row);

            visit.PatientID = row.Field<int>("PatientID");
            visit.PatientInfo = clsAppointment.MapPatientInfo(row);
            visit.DoctorID = row.Field<int>("DoctorID");
            visit.DoctorInfo = clsAppointment.MapDoctorInfo(row);
            visit.AppointmentStartAt = row.Field<DateTimeOffset?>("StartAt");

            return visit;
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

        /// <summary>
        /// Opens a visit for an appointment.
        /// </summary>
        /// <returns>Whether it was written.</returns>
        public Task<bool> CreateAsync()
        {
            if (AppointmentID <= 0)
                return Task.FromResult(false);

            return _CreateVisitAsync();
        }

        /// <summary>
        /// Saves a visit that already exists. A checkout, when there is one,
        /// has to come after the check-in.
        /// </summary>
        /// <returns>Whether it was written.</returns>
        public Task<bool> UpdateAsync()
        {
            bool isValidCheckout = CheckoutTime == null || CheckoutTime > CheckinTime;

            if (!(VisitID > 0 && AppointmentID > 0 && isValidCheckout))
                return Task.FromResult(false);

            return _UpdateVisitAsync();
        }
    }
}
