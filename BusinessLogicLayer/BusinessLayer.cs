using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer
{
    public class BusinessLayer
    {

        public class clsPatients
        {
            public enum enMode { Update, AddNew };
            public string FullName { set; get; }
            public string Address { set; get; }
            public string MedicalHistory { set; get; }
            public string PhoneNumber { set; get; }
            public string Notes { set; get; }
            public short Age { set; get; }
            public DateTime VisitDate { set; get; }
            public short Gender { set; get; }
            public int PatientID { set; get; }
            public string CanalLength { set; get; }
            public enMode Mode { set; get; }

            public clsPatients(string fullName, string address, string medicalHistory, string phoneNumber, string notes, short age, DateTime visitDate, short gender, int patientID, string canalLength)
            {
                FullName = fullName;
                Address = address;
                MedicalHistory = medicalHistory;
                PhoneNumber = phoneNumber;
                Notes = notes;
                Age = age;
                VisitDate = visitDate;
                Gender = gender;
                PatientID = patientID;
                CanalLength = canalLength;
                this.Mode = enMode.Update;
            }

            public clsPatients()
            {
                this.PatientID = -1;
                this.VisitDate = DateTime.MinValue;
                this.Gender = 0;
                this.Age = 0;
                this.Notes = "";
                this.FullName = "";
                this.MedicalHistory = "";
                this.CanalLength = "";
                this.Address = "";
                this.PhoneNumber = "";
                this.Mode = enMode.AddNew;
            }

            public static clsPatients GetPatientByID(int PatientID)
            {
                string fullName = "";
                string address = "";
                string medicalHistory = "";
                string phoneNumber = "";
                string notes = "";
                short age = 0;
                DateTime visitDate = DateTime.Now;
                short gender = 1;
                int patientID = 0;
                string canalLength = "";
                if (DataAccessLayer.DataLayer.GetPatientInfoByID(PatientID, ref fullName, ref address, ref phoneNumber, ref age, ref gender, ref visitDate, ref medicalHistory, ref canalLength, ref notes))
                    return new clsPatients(fullName, address, medicalHistory, phoneNumber, notes, age, visitDate, gender, PatientID, canalLength);
                return null;
            }
            public static DataTable SearchPatients(string column, string keyword)
            {
                return DataAccessLayer.DataLayer.Search(column, keyword);
            }

            private bool _addNewPatient()
            {
                this.PatientID = DataAccessLayer.DataLayer.AddNewPatient(this.MedicalHistory, this.CanalLength, this.Notes, this.FullName, this.Gender, this.VisitDate, this.Address, this.PhoneNumber, this.Age);
                this.Mode = enMode.Update;
                return this.PatientID != -1;
            }

            private bool _UpdatePatient()
            {
                return DataAccessLayer.DataLayer.UpdatePatient(this.PatientID, this.FullName, this.Age, this.MedicalHistory, this.Notes, this.CanalLength, this.Gender, this.VisitDate, this.Address, this.PhoneNumber);
            }
            public bool Save()
            {
                switch (Mode)
                {
                    case enMode.AddNew:
                        {
                            this.Mode = enMode.Update;
                            return _addNewPatient();
                        }
                    case enMode.Update:
                        {
                            return _UpdatePatient();
                        }
                }
                return false;
            }

        }

        public static bool DeletePatient(int PatientID)
        {
            return DataAccessLayer.DataLayer.DeletePatient(PatientID);
        }
        public static bool IsPatientExist(int PatientID)
        {
            return DataAccessLayer.DataLayer.IsPatientExist(PatientID);
        }
        public static DataTable GetAllPatients(int PageNum , int PageSize)
        {
            return DataAccessLayer.DataLayer.ShowAllPatients(PageNum,PageSize);
        }

        public class clsHashing
        {
            public string HashedPassword { get; set; }
            public static string HashOutput(string password)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] Hashbyte = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                    return BitConverter.ToString(Hashbyte).Replace("-", "").ToLower();
                }
            }
            public static string Encrypt(string plainText, string key)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    // Set the key and IV for AES encryption
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = new byte[aesAlg.BlockSize / 8];


                    // Create an encryptor
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);


                    // Encrypt the data
                    using (var msEncrypt = new System.IO.MemoryStream())
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        using (var swEncrypt = new System.IO.StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }


                        // Return the encrypted data as a Base64-encoded string
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }

            public static string Decrypt(string cipherText, string key)
            {
                using (Aes aesAlg = Aes.Create())
                {
                    // Set the key and IV for AES decryption
                    aesAlg.Key = Encoding.UTF8.GetBytes(key);
                    aesAlg.IV = new byte[aesAlg.BlockSize / 8];


                    // Create a decryptor
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);


                    // Decrypt the data
                    using (var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                    {
                        // Read the decrypted data from the StreamReader
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        public class clsUsers
        {
            public int UserID { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string Role { get; set; }

            public static bool Add(string username, string password, string role)
            {
                string hash = clsHashing.HashOutput(password);
                return DataAccessLayer.DataLayer.UsersData.AddUser(username, hash, role);
            }

            public static bool Update(int id, string username, string password, string role)
            {
                string hash = clsHashing.HashOutput(password);
                return DataAccessLayer.DataLayer.UsersData.UpdateUser(id, username, hash, role);
            }

            public static bool Delete(int id)
            {
                return DataAccessLayer.DataLayer.UsersData.DeleteUser(id);
            }

            public static DataTable GetAll()
            {
                return DataAccessLayer.DataLayer.UsersData.GetAllUsers();
            }

            public static bool Login(string username, string password, ref string role)
            {
                string hash = clsHashing.HashOutput(password);
                return DataAccessLayer.DataLayer.UsersData.ValidateUser(username, hash, ref role);
            }
        }

        public class clsInventory
        {
            public int ItemID { get; set; }
            public string ItemName { get; set; }
            public int Quantity { get; set; }
            public int MinQuantity { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public DateTime? DateOfPurchase { get; set; }

            public static bool Add(string name, int qty, int minQty, DateTime? exp, DateTime? buy)
            {
                return DataAccessLayer.DataLayer.InventoryData.AddOrIncrementItem(name, qty, minQty, exp, buy);
            }

            public static bool Update(int id, string name, int qty, int minQty, DateTime? exp, DateTime? buy)
            {
                return DataAccessLayer.DataLayer.InventoryData.UpdateItem(id, name, qty, minQty, exp, buy);
            }

            public static bool Delete(int id)
            {
                return DataAccessLayer.DataLayer.InventoryData.DeleteItem(id);
            }

            public static DataTable GetAll()
            {
                return DataAccessLayer.DataLayer.InventoryData.GetAllItems();
            }

            public static DataTable GetLowStock()
            {
                return DataAccessLayer.DataLayer.InventoryData.GetLowStockItems();
            }
            public static DataTable GetAItemByID(int ItemID)
            {
                return DataAccessLayer.DataLayer.InventoryData.GetAItemByID(ItemID);
            }
        }

        public class clsAppointments
        {
            public int AppointmentID { get; set; }
            public int PatientID { get; set; }
            public DateTime AppointmentDate { get; set; }
            public string Reason { get; set; }
            public string Status { get; set; }
            public string Notes { get; set; }

            public static bool Add(int patientID, DateTime date, string reason, string status, string notes)
            {
                return DataAccessLayer.DataLayer.AppointmentData.AddAppointment(patientID, date, reason, status, notes);
            }

            public static bool Update(int id, DateTime date, string reason, string status, string notes)
            {
                return DataAccessLayer.DataLayer.AppointmentData.UpdateAppointment(id, date, reason, status, notes);
            }

            public static bool Delete(int id)
            {
                return DataAccessLayer.DataLayer.AppointmentData.DeleteAppointment(id);
            }

            public static DataTable GetAll()
            {
                return DataAccessLayer.DataLayer.AppointmentData.GetAllAppointments();
            }

            public static DataTable GetByPatient(int patientID)
            {
                return DataAccessLayer.DataLayer.AppointmentData.GetAppointmentsByPatientID(patientID);
            }
            public static clsAppointments GetByAppt(int AppID)
            {
                DataRow row = DataAccessLayer.DataLayer.AppointmentData.GetAppointmentByID(AppID);
                if (row == null)
                    return null;

                return new clsAppointments
                {
                    AppointmentID = Convert.ToInt32(row["AppointmentID"]),
                    PatientID = Convert.ToInt32(row["PatientID"]),
                    AppointmentDate = Convert.ToDateTime(row["AppointmentDate"]),
                    Reason = row["Reason"]?.ToString(),
                    Status = row["Status"]?.ToString(),
                    Notes = row["Notes"]?.ToString()
                };
            }
        }



    }
}
