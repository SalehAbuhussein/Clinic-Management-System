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

        public static async Task<PagedResult<clsMedicine>> FindAllAsync(string? name = null, int pageNumber = 1, int pageSize = 10)
        {
            (List<clsMedicineDA.MedicineRow> rows, int total) = await clsMedicineDA.FindAllAsync(name, pageNumber, pageSize);

            return new PagedResult<clsMedicine>(rows.Select(row => MapMedicine(row)).ToList(), pageNumber, pageSize, total);
        }

        public static async Task<clsMedicine?> FindAsync(int id)
        {
            clsMedicineDA.MedicineRow? row = await clsMedicineDA.FindAsync(id);

            return row == null ? null : MapMedicine(row);
        }

        /// <summary>
        /// Builds a medicine from a joined row, for a query that loads one alongside
        /// something else. The medicine's own lookups go through the record overload.
        /// </summary>
        internal static clsMedicine MapMedicine(DataRow row)
        {
            return new clsMedicine(
                row.Field<int>("MedicineID"),
                row.Field<int>("RxCUI"),
                row.Field<string>("Genericname") ?? string.Empty,
                row.Field<string>("Brandname") ?? string.Empty,
                row.Field<string>("Form") ?? string.Empty,
                row.Field<string>("Strength") ?? string.Empty
            );
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

        public static Task<bool> AreValidMedicinesAsync(List<string> medicineIds)
        {
            return clsMedicineDA.AreValidMedicinesAsync(medicineIds);
        }

        private async Task<bool> _CreateMedicineAsync()
        {
            MedicineID = await clsMedicineDA.CreateAsync(RxCUI, Genericname, Brandname, Form, Strength);
            return MedicineID.HasValue;
        }

        private Task<bool> _UpdateMedicineAsync()
        {
            return clsMedicineDA.UpdateAsync(MedicineID, RxCUI, Genericname, Brandname, Form, Strength);
        }

        public async Task<bool> SaveAsync()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (await _CreateMedicineAsync())
                    {
                        _Mode = enMode.Update;
                        return true;
                    }

                    return false;
                case enMode.Update:
                    return await _UpdateMedicineAsync();
            }

            return false;
        }

        public bool IsValid()
        {
            return RxCUI > 0 && !string.IsNullOrEmpty(Genericname) && !string.IsNullOrEmpty(Form) && !string.IsNullOrEmpty(Strength);
        }
    }
}
