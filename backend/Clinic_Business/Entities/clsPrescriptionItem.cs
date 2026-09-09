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

        public clsPrescriptionItem()
        {
        }

        private clsPrescriptionItem(int id, int visitID, int medicineID, string dosage, string frequency, string duration)
        {

            PrescriptionItemID = id;
            VisitID = visitID;
            MedicineID = medicineID;
            Dosage = dosage;
            Frequency = frequency;
            Duration = duration;
        }

        private static clsPrescriptionItem MapPrescriptionItem(DataRow row)
        {
            return new clsPrescriptionItem(
                (int)row["PrescriptionItemID"],
                (int)row["VisitID"],
                (int)row["MedicineID"],
                (string)row["Dosage"],
                (string)row["Frequency"],
                (string)row["Duration"]);
        }

        public static async Task<List<clsPrescriptionItem>> FindAllByVisitIDAsync(int visitID, int pageNumber, int pageSize)
        {
            DataTable dt = await clsPrescriptionItemDA.FindAllByVisitIDAsync(visitID, pageNumber, pageSize);

            return dt.AsEnumerable().Select(MapPrescriptionItem).ToList();
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
