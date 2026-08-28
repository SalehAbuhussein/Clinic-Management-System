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
        public enum enMode
        {
            Add,
            Update
        }

        public int PrescriptionItemID { get; set; }
        public int VisitID { get; set; }
        public int MedicineID { get; set; }
        public string Dosage { get; set; }
        public string Frequency { get; set; }
        public string Duration { get; set; }
        private enMode _Mode = enMode.Add;

        public clsPrescriptionItem()
        {
            _Mode = enMode.Add;
        }

        private clsPrescriptionItem(int id, int visitID, int medicineID, string dosage, string frequency, string duration)
        {
            _Mode = enMode.Update;

            PrescriptionItemID = id;
            VisitID = visitID;
            MedicineID = medicineID;
            Dosage = dosage;
            Frequency = frequency;
            Duration = duration;
        }

        public static DataTable FindAllByVisitID(int visitID, int pageNumber = 1, int pageSize = 10)
        {
            return clsPrescriptionItemDA.FindAllByVisitID(visitID, pageNumber, pageSize);
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
    }
}
