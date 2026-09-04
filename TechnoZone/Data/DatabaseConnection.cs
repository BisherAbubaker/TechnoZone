using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using TechnoZone.Models;

namespace TechnoZone.Data
{
    /// <summary>
    /// All SQL Server access for the site. Every query uses parameters,
    /// so user input can never be treated as SQL.
    /// </summary>
    public class DatabaseConnection
    {
        private readonly string _connectionString;

        public DatabaseConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? @"Server=(localdb)\mssqllocaldb;Database=TechnoZoneDB;Integrated Security=true;Encrypt=false;";
        }

        // =====================================================================
        //  SCHEMA
        // =====================================================================

        /// <summary>
        /// Creates the tables if they are missing. The full script with
        /// stored procedures lives in Database/TechnoZone_Database.sql.
        /// </summary>
        public void InitializeDatabase()
        {
            const string script = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                BEGIN
                    CREATE TABLE dbo.Users (
                        Id           INT IDENTITY(1,1) PRIMARY KEY,
                        Username     NVARCHAR(50)  NOT NULL UNIQUE,
                        Email        NVARCHAR(100) NOT NULL UNIQUE,
                        PasswordHash NVARCHAR(256) NOT NULL,
                        FirstName    NVARCHAR(100) NULL,
                        LastName     NVARCHAR(100) NULL,
                        CreatedAt    DATETIME NOT NULL DEFAULT (GETUTCDATE()),
                        LastLogin    DATETIME NULL,
                        IsActive     BIT NOT NULL DEFAULT (1)
                    );
                    CREATE INDEX IX_Users_Username ON dbo.Users (Username);
                    CREATE INDEX IX_Users_Email    ON dbo.Users (Email);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoginAttempts')
                BEGIN
                    CREATE TABLE dbo.LoginAttempts (
                        Id          INT IDENTITY(1,1) PRIMARY KEY,
                        Username    NVARCHAR(50) NOT NULL,
                        IsSuccess   BIT NOT NULL,
                        IpAddress   NVARCHAR(45) NULL,
                        AttemptedAt DATETIME NOT NULL DEFAULT (GETUTCDATE())
                    );
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NewsletterSubscribers')
                BEGIN
                    CREATE TABLE dbo.NewsletterSubscribers (
                        Id           INT IDENTITY(1,1) PRIMARY KEY,
                        Email        NVARCHAR(100) NOT NULL UNIQUE,
                        SubscribedAt DATETIME NOT NULL DEFAULT (GETUTCDATE()),
                        IsActive     BIT NOT NULL DEFAULT (1)
                    );
                END
            ";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = new SqlCommand(script, connection);
            command.ExecuteNonQuery();
        }

        // =====================================================================
        //  ACCOUNTS
        // =====================================================================

        /// <summary>Creates an account. Returns false when the username or email is taken.</summary>
        public bool RegisterUser(string username, string email, string password, string firstName, string lastName)
        {
            try
            {
                if (UserExists(username, email))
                {
                    return false;
                }

                const string query = @"
                    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, CreatedAt, IsActive)
                    VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, @CreatedAt, 1);";

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Email", email);
                command.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                command.Parameters.AddWithValue("@FirstName", firstName ?? string.Empty);
                command.Parameters.AddWithValue("@LastName", lastName ?? string.Empty);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

                command.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Returns the user when the password matches, otherwise null.</summary>
        public User? AuthenticateUser(string username, string password)
        {
            try
            {
                const string query = @"
                    SELECT Id, Username, Email, PasswordHash, FirstName, LastName, CreatedAt, LastLogin, IsActive
                    FROM   dbo.Users
                    WHERE  Username = @Username AND IsActive = 1;";

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);

                using var reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    return null;
                }

                var storedHash = reader["PasswordHash"].ToString() ?? string.Empty;

                if (!VerifyPassword(password, storedHash))
                {
                    return null;
                }

                var user = new User
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    FirstName = reader["FirstName"] == DBNull.Value ? string.Empty : reader["FirstName"].ToString()!,
                    LastName = reader["LastName"] == DBNull.Value ? string.Empty : reader["LastName"].ToString()!,
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    LastLogin = reader["LastLogin"] == DBNull.Value ? null : (DateTime)reader["LastLogin"],
                    IsActive = (bool)reader["IsActive"]
                };

                reader.Close();
                UpdateLastLogin(user.Id);

                return user;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Used by the live check on the register page.</summary>
        public bool IsUsernameTaken(string username)
        {
            return ScalarCount("SELECT COUNT(*) FROM dbo.Users WHERE Username = @Value", username) > 0;
        }

        /// <summary>Used by the live check on the register page.</summary>
        public bool IsEmailTaken(string email)
        {
            return ScalarCount("SELECT COUNT(*) FROM dbo.Users WHERE Email = @Value", email) > 0;
        }

        private bool UserExists(string username, string email)
        {
            const string query = "SELECT COUNT(*) FROM dbo.Users WHERE Username = @Username OR Email = @Email";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Email", email);

            return (int)command.ExecuteScalar() > 0;
        }

        private int ScalarCount(string query, string value)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Value", value);

                return (int)command.ExecuteScalar();
            }
            catch
            {
                return 0;
            }
        }

        private void UpdateLastLogin(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(
                    "UPDATE dbo.Users SET LastLogin = @LastLogin WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@LastLogin", DateTime.UtcNow);
                command.Parameters.AddWithValue("@Id", userId);

                command.ExecuteNonQuery();
            }
            catch
            {
                // A failed timestamp update should never block a sign-in.
            }
        }

        // =====================================================================
        //  AUDIT LOG
        // =====================================================================

        public void LogLoginAttempt(string username, bool isSuccess, string? ipAddress)
        {
            try
            {
                const string query = @"
                    INSERT INTO dbo.LoginAttempts (Username, IsSuccess, IpAddress, AttemptedAt)
                    VALUES (@Username, @IsSuccess, @IpAddress, @AttemptedAt);";

                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@IsSuccess", isSuccess);
                command.Parameters.AddWithValue("@IpAddress", (object?)ipAddress ?? DBNull.Value);
                command.Parameters.AddWithValue("@AttemptedAt", DateTime.UtcNow);

                command.ExecuteNonQuery();
            }
            catch
            {
                // Auditing is best effort.
            }
        }

        // =====================================================================
        //  NEWSLETTER
        // =====================================================================

        /// <summary>Returns true when the address was newly added, false when it was already on the list.</summary>
        public bool SubscribeNewsletter(string email)
        {
            const string query = @"
                IF EXISTS (SELECT 1 FROM dbo.NewsletterSubscribers WHERE Email = @Email)
                BEGIN
                    UPDATE dbo.NewsletterSubscribers SET IsActive = 1 WHERE Email = @Email;
                    SELECT 0;
                END
                ELSE
                BEGIN
                    INSERT INTO dbo.NewsletterSubscribers (Email) VALUES (@Email);
                    SELECT 1;
                END";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);

            return Convert.ToInt32(command.ExecuteScalar()) == 1;
        }

        // =====================================================================
        //  PASSWORD HASHING
        //  SHA-256 encoded as Base64. This matches the sample hashes in
        //  Database/TechnoZone_Database.sql. A production site would use a
        //  slow, salted hash such as PBKDF2 or bcrypt instead.
        // =====================================================================

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return string.Equals(HashPassword(password), hash, StringComparison.Ordinal);
        }
    }
}
