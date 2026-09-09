using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Entities
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

        public static async Task<List<clsMedicine>> FindAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            List<clsMedicineDA.MedicineRow> rows = await clsMedicineDA.FindAllAsync(pageNumber, pageSize);

            return rows.Select(MapMedicine).ToList();
        }

        public static async Task<clsMedicine?> FindAsync(int id)
        {
            clsMedicineDA.MedicineRow? row = await clsMedicineDA.FindAsync(id);

            return row == null ? null : MapMedicine(row);
        }

        internal static clsMedicine MapMedicine(clsMedicineDA.MedicineRow row)
        {
            return new clsMedicine(
                row.MedicineID,
                row.RxCUI,
                row.Genericname,
                row.Brandname ?? string.Empty,
                row.Form,
                row.Strength
            );
        }

        public static Task<bool> DeleteAsync(int id)
        {
            return clsMedicineDA.DeleteAsync(id);
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
