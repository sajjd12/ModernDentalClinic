# Bug Report

## 1. Critical Security Vulnerability: SQL Injection
**Location:** `DataAccessLayer/DataLayer.cs`, Method: `Search`

**Description:**
The `Search` method constructs a SQL query using string interpolation with the `column` parameter directly:
```csharp
string query = $"SELECT ... FROM Patients WHERE {column} LIKE @keyword";
```
This allows an attacker to inject arbitrary SQL logic if they can control the `column` parameter. For example, passing `FullName; DROP TABLE Patients; --` as the `column` argument would execute the malicious command.

**Recommendation:**
Validate the `column` parameter against a whitelist of allowed column names before constructing the query. Do not allow arbitrary strings to be used as column identifiers.

## 2. Security Vulnerability: Insecure Encryption (Zero IV)
**Location:** `BusinessLogicLayer/BusinessLayer.cs`, Class: `clsHashing`

**Description:**
The `Encrypt` and `Decrypt` methods in `clsHashing` explicitly set the Initialization Vector (IV) to an array of zeros:
```csharp
aesAlg.IV = new byte[aesAlg.BlockSize / 8];
```
Using a static or zero IV with CBC mode (default for Aes) makes the encryption deterministic for the first block and leaks information about patterns in the plaintext. It renders the encryption susceptible to certain attacks.

**Recommendation:**
Generate a random IV for each encryption operation. The IV should be stored alongside the ciphertext (e.g., prepended to it) so it can be retrieved for decryption.

## 3. Configuration Issue: Hardcoded Connection String
**Location:** `DataAccessLayer/DataLayer.cs`, Field: `ConnectionString`

**Description:**
The database connection string is hardcoded in the source code:
```csharp
private static readonly string ConnectionString = "Server=.;Database=DentalClinicDB;Trusted_Connection=True;";
```
This makes it difficult to change the database configuration without recompiling the application. It also poses a security risk if sensitive credentials are added to the connection string in the future.

**Recommendation:**
Move the connection string to a configuration file (e.g., `App.config` or `appsettings.json`) and read it using `ConfigurationManager` or `IConfiguration`.

## 4. Logic Error: Patient Save State Management
**Location:** `BusinessLogicLayer/BusinessLayer.cs`, Class: `clsPatients`, Method: `Save`

**Description:**
The `Save` method changes the object's mode to `Update` *before* attempting to add the new patient:
```csharp
case enMode.AddNew:
{
    this.Mode = enMode.Update;
    return _addNewPatient();
}
```
If `_addNewPatient()` fails (returns `false`), the object remains in `Update` mode but with an invalid state (likely `PatientID` is -1). Subsequent calls to `Save()` will attempt to update a non-existent patient.

**Recommendation:**
Only update the `Mode` to `Update` after a successful insertion.

## 5. Potential Concurrency Issue
**Location:** `DataAccessLayer/DataLayer.cs`, Class: `InventoryData`, Method: `AddOrIncrementItem`

**Description:**
The `AddOrIncrementItem` method performs a "Check-then-Act" sequence (Select then Insert/Update) without a transaction or locking mechanism. In a concurrent environment, two requests could both see the item as non-existent and both attempt to insert it, leading to a race condition (e.g., duplicate items or primary key violation).

**Recommendation:**
Wrap the logic in a transaction with appropriate isolation level, or use a `MERGE` statement (if using SQL Server) to perform the operation atomically.

## 6. Error Handling
**Location:** `DataAccessLayer/DataLayer.cs`

**Description:**
Exceptions are caught and logged to a text file, but the methods return `false` or `-1` without propagating the error information. This swallows the actual exception, making it hard for the calling layer to report meaningful errors to the user or diagnose the issue.

**Recommendation:**
Consider rethrowing the exception or returning a result object that contains error details.
