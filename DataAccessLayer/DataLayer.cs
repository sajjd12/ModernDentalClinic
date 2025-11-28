using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace DataAccessLayer
{
    public static class DataLayer
    {
        // يفضل وضع الاتصال في ملف App.config أو secrets.json
        private static readonly string ConnectionString =
            "Server=.;Database=DentalClinicDB;Trusted_Connection=True;";

        // =========================
        //   الدوال المساعدة العامة
        // =========================

        private static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        private static SqlCommand CreateCommand(SqlConnection con, string query, Dictionary<string, object> parameters)
        {
            SqlCommand cmd = new SqlCommand(query, con);
            if (parameters != null)
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }
            return cmd;
        }

        // =========================
        //        المرضى
        // =========================

        public static bool GetPatientInfoByID(
            int patientID,
            ref string fullName,
            ref string address,
            ref string phone,
            ref short age,
            ref short gender,
            ref DateTime visitDate,
            ref string medicalHistory,
            ref string canalLength,
            ref string notes)
        {
            bool found = false;
            const string query = "SELECT * FROM Patients WHERE PatientID = @PatientID";

            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = CreateCommand(con, query, new Dictionary<string, object> { ["@PatientID"] = patientID }))
            {
                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            found = true;
                            fullName = reader["FullName"] as string ?? "";
                            address = reader["Address"] as string ?? "";
                            phone = reader["Phone"] as string ?? "";
                            age = reader["Age"] != DBNull.Value ? Convert.ToInt16(reader["Age"]) : (short)0;
                            gender = reader["Gender"] != DBNull.Value ? Convert.ToInt16(reader["Gender"]) : (short)0;
                            visitDate = reader["VisitDate"] != DBNull.Value ? Convert.ToDateTime(reader["VisitDate"]) : DateTime.Now;
                            medicalHistory = reader["MedicalHistory"] as string ?? "";
                            canalLength = reader["CanalLength"] as string ?? "";
                            notes = reader["Notes"] as string ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError("GetPatientInfoByID", ex);
                }
            }

            return found;
        }

        public static DataTable Search(string column, string keyword)
        {
            DataTable dt = new DataTable();
            string query = $"SELECT  Patients.PatientID AS التسلسل,Patients.FullName AS الاسم, Patients.VisitDate AS تاريخ_المراجعة, Patients.Address AS العنوان,Patients.Age AS العمر,CASE WHEN Patients.Gender = 0 THEN 'انثى' ELSE 'ذكر' END AS الجنس, Patients.Phone AS رقم_الهاتف FROM Patients WHERE {column} LIKE @keyword";

            using (SqlConnection con = GetConnection()) 
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
            }

            return dt;
        }
        public static int AddNewPatient(
            string medicalHistory,
            string canalLength,
            string notes,
            string fullName,
            short gender,
            DateTime visitDate,
            string address,
            string phoneNumber,
            short age)
        {
            const string query = @"
                INSERT INTO Patients
                    (FullName, Age, Gender, Phone, MedicalHistory, VisitDate, Notes, CanalLength, Address)
                VALUES
                    (@FullName, @Age, @Gender, @Phone, @MedicalHistory, @VisitDate, @Notes, @CanalLength, @Address);
                SELECT SCOPE_IDENTITY();";

            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = CreateCommand(con, query, new Dictionary<string, object>
            {
                ["@FullName"] = fullName,
                ["@Age"] = age,
                ["@Gender"] = gender,
                ["@Phone"] = phoneNumber,
                ["@MedicalHistory"] = medicalHistory,
                ["@VisitDate"] = visitDate,
                ["@Notes"] = notes,
                ["@CanalLength"] = canalLength,
                ["@Address"] = address
            }))
            {
                try
                {
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
                catch (Exception ex)
                {
                    LogError("AddNewPatient", ex);
                    return -1;
                }
            }
        }

        public static bool UpdatePatient(
            int patientID,
            string fullName,
            short age,
            string medicalHistory,
            string notes,
            string canalLength,
            short gender,
            DateTime visitDate,
            string address,
            string phone)
        {
            const string query = @"
                UPDATE Patients
                SET FullName = @FullName,
                    Age = @Age,
                    Gender = @Gender,
                    Phone = @Phone,
                    MedicalHistory = @MedicalHistory,
                    VisitDate = @VisitDate,
                    Notes = @Notes,
                    CanalLength = @CanalLength,
                    Address = @Address
                WHERE PatientID = @PatientID";

            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = CreateCommand(con, query, new Dictionary<string, object>
            {
                ["@PatientID"] = patientID,
                ["@FullName"] = fullName,
                ["@Age"] = age,
                ["@Gender"] = gender,
                ["@Phone"] = phone,
                ["@MedicalHistory"] = medicalHistory,
                ["@VisitDate"] = visitDate,
                ["@Notes"] = notes,
                ["@CanalLength"] = canalLength,
                ["@Address"] = address
            }))
            {
                try
                {
                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (Exception ex)
                {
                    LogError("UpdatePatient", ex);
                    return false;
                }
            }
        }

        public static bool DeletePatient(int patientID)
        {
            const string query = @"
                DELETE FROM Payments WHERE PatientID = @PatientID;
                DELETE FROM Appointments WHERE PatientID = @PatientID;
                DELETE FROM Images WHERE PatientID = @PatientID;
                DELETE FROM Patients WHERE PatientID = @PatientID;";

            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = CreateCommand(con, query, new Dictionary<string, object> { ["@PatientID"] = patientID }))
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    cmd.Transaction = tran;
                    try
                    {
                        int rows = cmd.ExecuteNonQuery();
                        tran.Commit();
                        return rows > 0;
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        LogError("DeletePatient", ex);
                        return false;
                    }
                }
            }
        }

        public static DataTable ShowAllPatients(int pageNumber = 1, int pageSize = 50)
        {
            const string query = @"
        WITH OrderedPatients AS (
            SELECT 
                Patients.PatientID AS التسلسل,
                Patients.FullName AS الاسم,
                Patients.VisitDate AS تاريخ_المراجعة,
                Patients.Address AS العنوان,
                Patients.Age AS العمر,
                CASE WHEN Patients.Gender = 0 THEN 'انثى' ELSE 'ذكر' END AS الجنس,
                Patients.Phone AS رقم_الهاتف,
                ROW_NUMBER() OVER (ORDER BY PatientID DESC) AS RowNum
            FROM Patients
        )
        SELECT التسلسل, الاسم, تاريخ_المراجعة, العنوان, العمر, الجنس, رقم_الهاتف
    FROM OrderedPatients
    WHERE RowNum BETWEEN @StartRow AND @EndRow
    ORDER BY RowNum";

            DataTable dt = new DataTable();
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                // حساب الصفوف المطلوبة
                int startRow = ((pageNumber - 1) * pageSize) + 1;
                int endRow = pageNumber * pageSize;

                cmd.Parameters.AddWithValue("@StartRow", startRow);
                cmd.Parameters.AddWithValue("@EndRow", endRow);

                try
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
                catch (Exception ex)
                {
                    LogError("ShowAllPatients", ex);
                }
            }
            return dt;
        }


        public static bool IsPatientExist(int patientID)
        {
            const string query = "SELECT 1 FROM Patients WHERE PatientID = @PatientID";
            using (SqlConnection con = GetConnection())
            using (SqlCommand cmd = CreateCommand(con, query, new Dictionary<string, object> { ["@PatientID"] = patientID }))
            {
                try
                {
                    con.Open();
                    return cmd.ExecuteScalar() != null;
                }
                catch (Exception ex)
                {
                    LogError("IsPatientExist", ex);
                    return false;
                }
            }
        }

        // =========================
        //  تسجيل الأخطاء في ملف
        // =========================

        private static void LogError(string method, Exception ex)
        {
            string logPath = "DataLayer_ErrorLog.txt";
            string message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {method} | {ex.Message}\n";
            System.IO.File.AppendAllText(logPath, message);
        }

        // =========================
        //        المستخدمين
        // =========================
        public static class UsersData
        {
            public static bool AddUser(string username, string passwordHash, string role)
            {
                const string q = "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@Username, @PasswordHash, @Role)";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object>
                {
                    ["@Username"] = username,
                    ["@PasswordHash"] = passwordHash,
                    ["@Role"] = role
                }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("AddUser", ex); return false; }
                }
            }

            public static bool UpdateUser(int id, string username, string passwordHash, string role)
            {
                const string q = "UPDATE Users SET Username=@Username, PasswordHash=@PasswordHash, Role=@Role WHERE UserID=@ID";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object>
                {
                    ["@ID"] = id,
                    ["@Username"] = username,
                    ["@PasswordHash"] = passwordHash,
                    ["@Role"] = role
                }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("UpdateUser", ex); return false; }
                }
            }

            public static bool DeleteUser(int id)
            {
                const string qCount = "SELECT COUNT(*) FROM Users";
                const string qDelete = "DELETE FROM Users WHERE UserID=@ID";

                using (SqlConnection con = GetConnection())
                {
                    try
                    {
                        con.Open();
                        // تحقق من عدد المستخدمين
                        using (SqlCommand cmdCount = new SqlCommand(qCount, con))
                        {
                            int total = (int)cmdCount.ExecuteScalar();
                            if (total <= 1)
                            {
                                // لا يمكن حذف آخر مستخدم
                                return false;
                            }
                        }

                        using (SqlCommand cmdDelete = CreateCommand(con, qDelete, new Dictionary<string, object> { ["@ID"] = id }))
                        {
                            return cmdDelete.ExecuteNonQuery() > 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError("DeleteUser", ex);
                        return false;
                    }
                }
            }


            public static DataTable GetAllUsers()
            {
                const string q = "SELECT UserID, Username, Role FROM Users ORDER BY UserID DESC";
                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand(q, con))
                {
                    try { con.Open(); dt.Load(cmd.ExecuteReader()); } catch (Exception ex) { LogError("GetAllUsers", ex); }
                }
                return dt;
            }

            public static bool ValidateUser(string username, string passwordHash, ref string role)
            {
                const string q = "SELECT Role FROM Users WHERE Username=@Username AND PasswordHash=@PasswordHash";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object>
                {
                    ["@Username"] = username,
                    ["@PasswordHash"] = passwordHash
                }))
                {
                    try
                    {
                        con.Open();
                        object r = cmd.ExecuteScalar();
                        if (r != null) { role = r.ToString(); return true; }
                    }
                    catch (Exception ex) { LogError("ValidateUser", ex); }
                    return false;
                }
            }
        }

        // =========================
        //         المخزون
        // =========================
        public static class InventoryData
        {
            public static bool AddOrIncrementItem(string name, int quantity, int minQuantity, DateTime? expiry, DateTime? purchase)
            {
                const string qGet = "SELECT ItemID, Quantity FROM Inventory WHERE LOWER(LTRIM(RTRIM(ItemName))) = LOWER(LTRIM(RTRIM(@Name)))";
                const string qInsert = @"INSERT INTO Inventory (ItemName, Quantity, MinQuantity, ExpiryDate, DAteOfPurchase)
                             VALUES (@Name, @Qty, @Min, @Exp, @Buy)";
                const string qUpdate = @"UPDATE Inventory
                             SET Quantity = Quantity + @Qty,
                                 MinQuantity = @Min,
                                 ExpiryDate = @Exp,
                                 DAteOfPurchase = @Buy
                             WHERE ItemID = @ID";

                using (SqlConnection con = GetConnection())
                {
                    try
                    {
                        con.Open();

                        int? id = null;
                        int currentQty = 0;

                        // البحث الدقيق بغض النظر عن المسافات أو حالة الأحرف
                        using (SqlCommand cmdGet = CreateCommand(con, qGet, new Dictionary<string, object> { ["@Name"] = name }))
                        using (SqlDataReader r = cmdGet.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                id = Convert.ToInt32(r["ItemID"]);
                                currentQty = Convert.ToInt32(r["Quantity"]);
                            }
                        }

                        if (id.HasValue)
                        {
                            // تحديث المادة الموجودة
                            using (SqlCommand cmdUpd = CreateCommand(con, qUpdate, new Dictionary<string, object>
                            {
                                ["@ID"] = id.Value,
                                ["@Qty"] = quantity,
                                ["@Min"] = minQuantity,
                                ["@Exp"] = (object)expiry ?? DBNull.Value,
                                ["@Buy"] = (object)purchase ?? DBNull.Value
                            }))
                            {
                                return cmdUpd.ExecuteNonQuery() > 0;
                            }
                        }
                        else
                        {
                            // إضافة مادة جديدة فقط إذا لم تكن موجودة فعلاً
                            using (SqlCommand cmdIns = CreateCommand(con, qInsert, new Dictionary<string, object>
                            {
                                ["@Name"] = name.Trim(),
                                ["@Qty"] = quantity,
                                ["@Min"] = minQuantity,
                                ["@Exp"] = (object)expiry ?? DBNull.Value,
                                ["@Buy"] = (object)purchase ?? DBNull.Value
                            }))
                            {
                                return cmdIns.ExecuteNonQuery() > 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError("AddOrIncrementItem", ex);
                        return false;
                    }
                }
            }





            public static bool UpdateItem(int id, string name, int quantity, int minQuantity, DateTime? expiry, DateTime? purchase)
            {
                const string q = @"UPDATE Inventory 
                           SET ItemName=@Name, Quantity=@Qty, MinQuantity=@Min, ExpiryDate=@Exp, DAteOfPurchase=@Buy
                           WHERE ItemID=@ID";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object>
                {
                    ["@ID"] = id,
                    ["@Name"] = name,
                    ["@Qty"] = quantity,
                    ["@Min"] = minQuantity,
                    ["@Exp"] = expiry,
                    ["@Buy"] = purchase
                }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("UpdateItem", ex); return false; }
                }
            }

            public static bool DeleteItem(int id)
            {
                const string q = "DELETE FROM Inventory WHERE ItemID=@ID";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object> { ["@ID"] = id }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("DeleteItem", ex); return false; }
                }
            }

            public static DataTable GetAllItems()
            {
                const string q = @"SELECT ItemID, ItemName, Quantity, MinQuantity, ExpiryDate, DAteOfPurchase FROM Inventory ORDER BY ItemID DESC";
                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand(q, con))
                {
                    try { con.Open(); dt.Load(cmd.ExecuteReader()); }
                    catch (Exception ex) { LogError("GetAllItems", ex); }
                }
                return dt;
            }
            public static DataTable GetAItemByID(int ItemID)
            {
                const string q = @"SELECT ItemID, ItemName, Quantity, MinQuantity, ExpiryDate, DAteOfPurchase FROM Inventory WHERE ItemID=@ID";
                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object> { ["@ID"] = ItemID }))
                {
                    try { con.Open(); dt.Load(cmd.ExecuteReader()); }
                    catch (Exception ex) { LogError("GetAllItems", ex); }
                }
                return dt;
            }
            public static DataTable GetLowStockItems()
            {
                const string q = "SELECT * FROM Inventory WHERE Quantity < MinQuantity OR Quantity < 3";
                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand(q, con))
                {
                    try { con.Open(); dt.Load(cmd.ExecuteReader()); }
                    catch (Exception ex) { LogError("GetLowStockItems", ex); }
                }
                return dt;
            }
        }


        // =========================
        //         المواعيد
        // =========================
        public static class AppointmentData
        {
            // إضافة موعد جديد
            public static bool AddAppointment(int patientID, DateTime date, string reason, string status, string notes)
            {
                const string q = @"INSERT INTO Appointments (PatientID, AppointmentDate, Reason, Status, Notes)
                           VALUES (@PatientID, @Date, @Reason, @Status, @Notes)";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object>
                {
                    ["@PatientID"] = patientID,
                    ["@Date"] = date,
                    ["@Reason"] = reason,
                    ["@Status"] = status,
                    ["@Notes"] = notes
                }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("AddAppointment", ex); return false; }
                }
            }

            // تعديل موعد
            public static bool UpdateAppointment(int appointmentID, DateTime date, string reason, string status, string notes)
            {
                const string q = @"UPDATE Appointments
                           SET AppointmentDate=@Date, Reason=@Reason, Status=@Status, Notes=@Notes
                           WHERE AppointmentID=@ID";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object>
                {
                    ["@ID"] = appointmentID,
                    ["@Date"] = date,
                    ["@Reason"] = reason,
                    ["@Status"] = status,
                    ["@Notes"] = notes
                }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("UpdateAppointment", ex); return false; }
                }
            }

            // حذف موعد
            public static bool DeleteAppointment(int appointmentID)
            {
                const string q = "DELETE FROM Appointments WHERE AppointmentID=@ID";
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object> { ["@ID"] = appointmentID }))
                {
                    try { con.Open(); return cmd.ExecuteNonQuery() > 0; }
                    catch (Exception ex) { LogError("DeleteAppointment", ex); return false; }
                }
            }

            // جلب جميع المواعيد مع اسم المريض
            public static DataTable GetAllAppointments()
            {
                const string q = @"
            SELECT 
                A.AppointmentID AS رقم_الموعد,
                P.FullName AS اسم_المريض,
                A.AppointmentDate AS التاريخ,
                A.Reason AS السبب,
                A.Status AS الحالة,
                A.Notes AS الملاحظات
            FROM Appointments A
            INNER JOIN Patients P ON A.PatientID = P.PatientID
            ORDER BY A.AppointmentDate DESC";

                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand(q, con))
                {
                    try { con.Open(); dt.Load(cmd.ExecuteReader()); }
                    catch (Exception ex) { LogError("GetAllAppointments", ex); }
                }
                return dt;
            }

            // جلب جميع المواعيد لمريض محدد
            public static DataTable GetAppointmentsByPatientID(int patientID)
            {
                const string q = @"
            SELECT 
                AppointmentID,
                AppointmentDate,
                Reason,
                Status,
                Notes
            FROM Appointments
            WHERE PatientID = @PatientID
            ORDER BY AppointmentDate DESC";

                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object> { ["@PatientID"] = patientID }))
                {
                    try { con.Open(); dt.Load(cmd.ExecuteReader()); }
                    catch (Exception ex) { LogError("GetAppointmentsByPatientID", ex); }
                }
                return dt;
            }
            public static DataRow GetAppointmentByID(int appointmentID)
            {
                const string q = @"
        SELECT 
            A.AppointmentID,
            A.PatientID,
            P.FullName AS PatientName,
            A.AppointmentDate,
            A.Reason,
            A.Status,
            A.Notes
        FROM Appointments A
        INNER JOIN Patients P ON A.PatientID = P.PatientID
        WHERE A.AppointmentID = @ID";

                DataTable dt = new DataTable();
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = CreateCommand(con, q, new Dictionary<string, object> { ["@ID"] = appointmentID }))
                {
                    try
                    {
                        con.Open();
                        dt.Load(cmd.ExecuteReader());
                    }
                    catch (Exception ex) { LogError("GetAppointmentByID", ex); }
                }

                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }

        }
    }
}


