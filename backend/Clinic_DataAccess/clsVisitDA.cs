using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clinic_DataAccess
{
    public class clsVisitDA
    {
        public static DataTable FindAll(int pageNumber = 1, int pageSize = 10)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT *
                FROM Visits
                ORDER BY VisitID
                OFFSET (@PageNumber - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            dt.Load(reader);
                        }
                    }
                }
            }

            return dt;
        }

        public static bool Find(
            int? id,
            ref int appointmentID,
            ref DateTime checkinTime,
            ref DateTime? checkoutTime,
            ref string? notes,
            ref bool isLocked
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [VisitID]
                    ,[AppointmentID]
                    ,[CheckinTime]
                    ,[CheckoutTime]
                    ,[Notes]
                    ,[isLocked]
                FROM [dbo].[Visits]
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;
                            appointmentID = (int)reader["AppointmentID"];
                            checkinTime = (DateTime)reader["CheckinTime"];
                            checkoutTime = reader["CheckoutTime"] == DBNull.Value ? null : (DateTime)reader["CheckoutTime"];
                            notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString();
                            isLocked = (bool)reader["IsLocked"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool FindByAppointmentID(
            int? id,
            ref int visitID,
            ref DateTime checkinTime,
            ref DateTime? checkoutTime,
            ref string? notes,
            ref bool isLocked
        )
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT [VisitID]
                    ,[AppointmentID]
                    ,[CheckinTime]
                    ,[CheckoutTime]
                    ,[Notes]
                    ,[IsLocked]
                FROM [dbo].[Visits]
                WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isFound = true;
                            visitID = (int)reader["VisitID"];
                            checkinTime = (DateTime)reader["CheckinTime"];
                            checkoutTime = reader["CheckoutTime"] == DBNull.Value ? null : (DateTime)reader["CheckoutTime"];
                            notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString();
                            isLocked = (bool)reader["IsLocked"];
                        }
                    }
                }
            }

            return isFound;
        }

        public static bool IsExistByAppointmentID(int? id)
        {
            if (!id.HasValue)
                return false;

            bool isFound = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1 FROM [dbo].[Visits] WHERE AppointmentID = @AppointmentID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        isFound = reader.HasRows;
                    }
                }
            }

            return isFound;
        }

        public static bool HasCheckedout(int? id)
        {
            if (!id.HasValue)
                return false;

            bool hasCheckedout = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"SELECT true=1 FROM Visits WHERE VisitID = @VisitID AND IsLocked = 1 AND CheckoutTime IS NOT NULL";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        hasCheckedout = reader.HasRows;
                    }
                }
            }

            return hasCheckedout;
        }

        public static int? Create(
            int appointmentID,
            DateTime checkinTime,
            DateTime? checkoutTime,
            string? notes
        )
        {
            int? id = null;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Appointments]
	            SET [Status] = 1
                WHERE AppointmentID = @AppointmentID;
                INSERT INTO [dbo].[Visits]
                    ([AppointmentID]
                    ,[CheckinTime]
                    ,[CheckoutTime]
                    ,[Notes])
                VALUES
                    (@AppointmentID
                    ,@CheckinTime
                    ,@CheckoutTime
                    ,@Notes); UPDATE [dbo].[Appointments]
                    SET [Status] = 1
                WHERE AppointmentID = @AppointmentID; SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@AppointmentID", appointmentID);
                    command.Parameters.AddWithValue("@CheckinTime", checkinTime);
                    command.Parameters.AddWithValue("@CheckoutTime", checkoutTime == null ? DBNull.Value : checkoutTime);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);

                    object result = command.ExecuteScalar();

                    if (result != null && int.TryParse(result.ToString(), out int insertedId))
                    {
                        id = insertedId;
                    }
                }
            }

            return id;
        }

        public static bool Update(
            int? id,
            int appointmentID,
            DateTime checkinTime,
            DateTime? checkoutTime,
            string? notes,
            bool isLocked
        )
        {
            if (!id.HasValue)
                return false;

            int rowsAffected = 0;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                string query = @"UPDATE [dbo].[Visits]
                SET [AppointmentID] = @AppointmentID
                    ,[CheckinTime] = @CheckinTime
                    ,[CheckoutTime] = @CheckoutTime
                    ,[Notes] = @Notes
                    ,[IsLocked] = @IsLocked
                WHERE VisitID = @VisitID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@VisitID", id);
                    command.Parameters.AddWithValue("@AppointmentID", appointmentID);
                    command.Parameters.AddWithValue("@CheckinTime", checkinTime);
                    command.Parameters.AddWithValue("@CheckoutTime", checkoutTime == null ? DBNull.Value : checkoutTime);
                    command.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? DBNull.Value : notes);
                    command.Parameters.AddWithValue("@IsLocked", isLocked);

                    rowsAffected = command.ExecuteNonQuery();
                }
            }

            return rowsAffected > 0;
        }

        public static bool Delete(int? id)
        {
            if (!id.HasValue)
                return false;

            bool hasDeleted = false;

            using (SqlConnection connection = new SqlConnection(clsDataAccessSettings.ConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("DeleteVisit", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@VisitID", id);

                    SqlParameter returnValue = new SqlParameter();
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add(returnValue);

                    command.ExecuteNonQuery();

                    int result = (int)returnValue.Value;

                    hasDeleted = result == 1;
                }
            }

            return hasDeleted;
        }
    }
}
