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
    public class clsPrescriptionItem
    {
        public int PrescriptionItemID { get; set; }
        public int VisitID { get; set; }
        public int MedicineID { get; set; }
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public clsMedicine? MedicineInfo { get; set; }

        public clsPrescriptionItem()
        {
        }

        private clsPrescriptionItem(int id, int visitID, int medicineID, string dosage, string frequency, string duration, clsMedicine medicineInfo)
        {
            PrescriptionItemID = id;
            VisitID = visitID;
            MedicineID = medicineID;
            Dosage = dosage;
            Frequency = frequency;
            Duration = duration;
            MedicineInfo = medicineInfo;
        }

        /// <summary>
        /// Builds an item and its medicine from one joined row. MedicineID is selected
        /// once, from PrescriptionItems, because the join makes both sides equal.
        /// </summary>
        private static clsPrescriptionItem MapPrescriptionItem(DataRow row)
        {
            return new clsPrescriptionItem(
                row.Field<int>("PrescriptionItemID"),
                row.Field<int>("VisitID"),
                row.Field<int>("MedicineID"),
                row.Field<string>("Dosage") ?? string.Empty,
                row.Field<string>("Frequency") ?? string.Empty,
                row.Field<string>("Duration") ?? string.Empty,
                clsMedicine.MapMedicine(row));
        }

        /// <summary>
        /// One line of a prescription, flattened with the medicine it names.
        /// A list has no use for the rest of a clsMedicine.
        /// </summary>
        public record PrescriptionItem(
            int Id,
            int VisitID,
            int MedicineID,
            string Dosage,
            string Frequency,
            string Duration,
            string Genericname,
            string? Brandname,
            string Form,
            string Strength);

        /// <summary>
        /// Every line of a visit's prescription, in the same envelope the other
        /// lists use. It is always one page: the update replaces the whole set,
        /// so handing back a partial read would make a save delete the rest.
        /// </summary>
        public static async Task<PagedResult<PrescriptionItem>> FindAllByVisitIDAsync(int visitID)
        {
            DataTable dt = await clsPrescriptionItemDA.FindAllByVisitIDAsync(visitID);

            List<PrescriptionItem> items = dt.AsEnumerable().Select(row => new PrescriptionItem(
                row.Field<int>("PrescriptionItemID"),
                row.Field<int>("VisitID"),
                row.Field<int>("MedicineID"),
                row.Field<string>("Dosage") ?? string.Empty,
                row.Field<string>("Frequency") ?? string.Empty,
                row.Field<string>("Duration") ?? string.Empty,
                row.Field<string>("Genericname") ?? string.Empty,
                row.Field<string>("Brandname"),
                row.Field<string>("Form") ?? string.Empty,
                row.Field<string>("Strength") ?? string.Empty
            )).ToList();

            return new PagedResult<PrescriptionItem>(items, 1, items.Count, items.Count);
        }

        public static Task<bool> HasPrescriptionByVisitIDAsync(int? id)
        {
            return clsPrescriptionItemDA.HasPrescriptionByVisitIDAsync(id);
        }

        public static Task<bool> CreatePrescriptionItemsAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
        {
            return clsPrescriptionItemDA.CreatePrescriptionItemsAsync(items);
        }

        public static async Task<clsPrescriptionItem?> FindAsync(int id)
        {
            DataRow? row = await clsPrescriptionItemDA.FindAsync(id);

            return row is null ? null : MapPrescriptionItem(row);
        }

        public static Task<int> UpdatePrescriptionItemsAsync(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
        {
            return clsPrescriptionItemDA.UpdatePrescriptionItemsAsync(items);
        }
    }
}
