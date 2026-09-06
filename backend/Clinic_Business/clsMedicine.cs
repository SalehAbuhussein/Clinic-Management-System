using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
{
    public class clsMedicine
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int? MedicineID { get; set; }
        public int RxCUI { get; set; }
        public string Genericname { get; set; } = string.Empty;
        public string? Brandname { get; set; }
        public string Form { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        private enMode _Mode = enMode.Add;

        public clsMedicine()
        {
            _Mode = enMode.Add;
        }

        private clsMedicine(int id, int rxCUI, string genericname, string brandname, string form, string strength)
        {
            _Mode = enMode.Update;
            MedicineID = id;
            Genericname = genericname;
            Brandname = brandname;
            Form = form;
            Strength = strength;
            RxCUI = rxCUI;
        }

        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            return clsMedicineDA.FindAll(pageNumber, pageSize);
        }

        public static clsMedicine? Find(int id)
        {
            int rxCUI = int.MinValue;
            string genericname = string.Empty;
            string brandname = string.Empty;
            string form = string.Empty;
            string strength = string.Empty;

            if (clsMedicineDA.Find(id, ref rxCUI, ref genericname, ref brandname, ref form, ref strength))
                return new clsMedicine(id, rxCUI, genericname, brandname, form, strength);

            return null;
        }

        public static bool Delete(int id)
        {
            return clsMedicineDA.Delete(id);
        }

        public static bool IsValidMedicines(List<string> medicineIds)
        {
            return clsMedicineDA.IsValidMedicines(medicineIds);
        }

        private bool _CreateMedicine()
        {
            MedicineID = clsMedicineDA.Create(RxCUI, Genericname, Brandname, Form, Strength);
            return MedicineID.HasValue;
        }

        private bool _UpdateMedicine()
        {
            return clsMedicineDA.Update(MedicineID, RxCUI, Genericname, Brandname, Form, Strength);
        }

        public bool Save()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_CreateMedicine())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return _UpdateMedicine();
            }

            return false;
        }

        public bool IsValid()
        {
            return RxCUI > 0 && !string.IsNullOrEmpty(Genericname) && !string.IsNullOrEmpty(Form) && !string.IsNullOrEmpty(Strength);
        }
    }
}
