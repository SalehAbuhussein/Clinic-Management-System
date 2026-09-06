using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
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

        public static DataTable FindAllByVisitID(int visitID, int pageNumber, int pageSize)
        {
            return clsPrescriptionItemDA.FindAllByVisitID(visitID, pageNumber, pageSize);
        }

        public static bool HasPrescriptionByVisitID(int? id)
        {
            return clsPrescriptionItemDA.HasPrescriptionByVisitID(id);
        }

        public static bool CreatePrescriptionItems(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
        {
            return clsPrescriptionItemDA.CreatePrescriptionItems(items);
        }

        public static clsPrescriptionItem? Find(int id)
        {
            int visitID = int.MinValue;
            int medicineID = int.MinValue;
            string dosage = string.Empty;
            string frequency = string.Empty;
            string duration = string.Empty;

            if (clsPrescriptionItemDA.Find(id, ref visitID, ref medicineID, ref dosage, ref frequency, ref duration))
                return new clsPrescriptionItem(id, visitID, medicineID, dosage, frequency, duration);

            return null;
        }

        public static int UpdatePrescriptionItems(List<(int visitID, int medicineID, string dosage, string frequency, string duration)> items)
        {
            return clsPrescriptionItemDA.UpdatePrescriptionItems(items);
        }
    }
}
