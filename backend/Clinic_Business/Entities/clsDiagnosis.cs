using Clinic_DataAccess;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business.Entities
{
    public class clsDiagnosis
    {
        public enum enMode
        {
            Add,
            Update
        }

        public int? DiagnosesID { get; set; }
        public int? VisitID { get; set; }
        public clsVisit? VisitInfo { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        private enMode _Mode = enMode.Add;

        public clsDiagnosis()
        {
            _Mode = enMode.Add;
        }

        private clsDiagnosis(int diagnosesID, int? visitID, clsVisit visitInfo, string diagnosis, string? notes, DateTime createdAt, DateTime updatedAt)
        {
            _Mode = enMode.Update;
            DiagnosesID = diagnosesID;
            VisitID = visitID;
            VisitInfo = visitInfo;
            Diagnosis = diagnosis;
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static async Task<clsDiagnosis?> FindAsync(int id)
        {
            DataRow? row = await clsDiagnosisDA.FindAsync(id);

            return row == null ? null : MapDiagnosis(row);
        }

        public static async Task<clsDiagnosis?> FindByVisitIDAsync(int? visitID)
        {
            DataRow? row = await clsDiagnosisDA.FindByVisitIDAsync(visitID);

            return row == null ? null : MapDiagnosis(row);
        }

        /// <summary>
        /// Builds a diagnosis and its visit from one joined row. Both lookups select the
        /// same columns, so one mapper serves them and neither can drift.
        ///
        /// The visit's own columns keep their names so clsVisit.MapVisit can read the
        /// same row; the diagnosis's Notes is the one that gets aliased, because both
        /// tables have a column by that name.
        /// </summary>
        internal static clsDiagnosis MapDiagnosis(DataRow row)
        {
            return new clsDiagnosis(
                row.Field<int>("DiagnosesID"),
                row.Field<int>("VisitID"),
                clsVisit.MapVisit(row),
                row.Field<string>("Diagnosis") ?? string.Empty,
                row.Field<string>("DiagnosisNotes"),
                row.Field<DateTime>("CreatedAt"),
                row.Field<DateTime>("UpdatedAt")
            );
        }

        public static Task<bool> IsExistByVisitIDAsync(int visitID)
        {
            return clsDiagnosisDA.IsExistByVisitIDAsync(visitID);
        }

        public static Task<bool> DeleteAsync(int id)
        {
            return clsDiagnosisDA.DeleteAsync(id);
        }

        private async Task<bool> _CreateAsync()
        {
            DiagnosesID = await clsDiagnosisDA.CreateAsync(VisitID, Diagnosis, Notes);
            return DiagnosesID.HasValue;
        }

        private Task<bool> _UpdateAsync()
        {
            return clsDiagnosisDA.UpdateAsync(DiagnosesID, Diagnosis, Notes);
        }

        public async Task<bool> SaveAsync()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (await _CreateAsync())
                    {
                        _Mode = enMode.Update;
                        return true;
                    } else
                    {
                        return false;
                    }
                case enMode.Update:
                    return await _UpdateAsync();
            }

            return false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Diagnosis);
        }
    }
}
