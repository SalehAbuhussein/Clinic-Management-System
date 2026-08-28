using Clinic_DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_Business
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
        public clsVisit VisitInfo { get; set; }
        public string Diagnosis { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        private enMode _Mode = enMode.Add;

        public clsDiagnosis()
        {
            _Mode = enMode.Add;
            VisitInfo = new clsVisit();
        }

        private clsDiagnosis(int diagnosesID, int? visitID, string diagnosis, string? notes, DateTime createdAt, DateTime updatedAt)
        {
            _Mode = enMode.Update;
            DiagnosesID = diagnosesID;
            VisitID = visitID;
            Diagnosis = diagnosis;
            VisitInfo = clsVisit.Find(visitID);
            Notes = notes;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        public static clsDiagnosis? Find(int id)
        {
            int visitID = int.MinValue;
            string diagnosis = string.Empty;
            string? notes = null;
            DateTime createdAt = DateTime.MinValue;
            DateTime updatedAt = DateTime.MinValue;

            if (clsDiagnosisDA.Find(id, ref visitID, ref diagnosis, ref notes, ref createdAt, ref updatedAt))
                return new clsDiagnosis(id, visitID, diagnosis, notes, createdAt, updatedAt);

            return null;
        }

        public static clsDiagnosis? FindByVisitID(int? visitID)
        {
            int diagnosisID = int.MinValue;
            string diagnosis = string.Empty;
            string? notes = null;
            DateTime createdAt = DateTime.MinValue;
            DateTime updatedAt = DateTime.MinValue;

            if (clsDiagnosisDA.FindByVisitID(visitID, ref diagnosisID, ref diagnosis, ref notes, ref createdAt, ref updatedAt))
                return new clsDiagnosis(diagnosisID, visitID, diagnosis, notes, createdAt, updatedAt);

            return null;
        }

        public static bool IsExistByVisitID(int visitID)
        {
            return clsDiagnosisDA.IsExistByVisitID(visitID);
        }

        public static bool Delete(int id)
        {
            return clsDiagnosisDA.Delete(id);
        }

        private bool _Create()
        {
            DiagnosesID = clsDiagnosisDA.Create(VisitID, Diagnosis, Notes);
            return DiagnosesID.HasValue;
        }

        private bool _Update()
        {
            return clsDiagnosisDA.Update(DiagnosesID, Diagnosis, Notes);
        }

        public bool Save()
        {
            if (!IsValid())
                return false;

            switch (_Mode)
            {
                case enMode.Add:
                    if (_Create())
                    {
                        _Mode = enMode.Update;
                        return true;
                    } else
                    {
                        return false;
                    }
                case enMode.Update:
                    return _Update();
            }

            return false;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Diagnosis);
        }
    }
}
