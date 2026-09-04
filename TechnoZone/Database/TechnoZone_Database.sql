/* ============================================================================
   TechnoZone - Database Setup Script
   Target: Microsoft SQL Server (run in SQL Server Management Studio)

   HOW TO RUN
   1. Open SQL Server Management Studio (SSMS) and connect to your server.
   2. Open a New Query window.
   3. Paste this whole script and press Execute (F5).
   4. Everything below is safe to re-run: it will not duplicate data.

   Test accounts created by this script:
     username: testuser   password: Test@123
     username: johndoe    password: John@123
     username: admin      password: Admin@123
   ============================================================================ */


/* ----------------------------------------------------------------------------
   1. CREATE THE DATABASE
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'TechnoZoneDB')
BEGIN
    CREATE DATABASE TechnoZoneDB;
    PRINT 'Database TechnoZoneDB created.';
END
ELSE
BEGIN
    PRINT 'Database TechnoZoneDB already exists.';
END
GO

USE TechnoZoneDB;
GO


/* ----------------------------------------------------------------------------
   2. USERS TABLE
   Holds every registered account. Passwords are never stored as plain text -
   the application stores a SHA-256 hash encoded as Base64 (44 characters).
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'Users')
BEGIN
    CREATE TABLE dbo.Users
    (
        Id            INT             IDENTITY(1,1) NOT NULL,
        Username      NVARCHAR(50)    NOT NULL,
        Email         NVARCHAR(100)   NOT NULL,
        PasswordHash  NVARCHAR(256)   NOT NULL,
        FirstName     NVARCHAR(100)   NULL,
        LastName      NVARCHAR(100)   NULL,
        CreatedAt     DATETIME        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE()),
        LastLogin     DATETIME        NULL,
        IsActive      BIT             NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT (1),

        CONSTRAINT PK_Users          PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email    UNIQUE (Email),
        CONSTRAINT CK_Users_Username CHECK (LEN(Username) >= 3),
        CONSTRAINT CK_Users_Email    CHECK (Email LIKE '%_@_%._%')
    );

    PRINT 'Table Users created.';
END
ELSE
BEGIN
    PRINT 'Table Users already exists.';
END
GO

/* Indexes for the columns the login screen searches on */
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Users_Username' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE NONCLUSTERED INDEX IX_Users_Username ON dbo.Users (Username) INCLUDE (PasswordHash, IsActive);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Users_Email' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE NONCLUSTERED INDEX IX_Users_Email ON dbo.Users (Email);
GO


/* ----------------------------------------------------------------------------
   3. LOGIN ATTEMPTS TABLE
   An audit trail of every sign-in attempt, successful or not. Useful for the
   report section of the project and for showing failed-login handling.
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'LoginAttempts')
BEGIN
    CREATE TABLE dbo.LoginAttempts
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        Username    NVARCHAR(50)   NOT NULL,
        IsSuccess   BIT            NOT NULL,
        IpAddress   NVARCHAR(45)   NULL,
        AttemptedAt DATETIME       NOT NULL CONSTRAINT DF_LoginAttempts_AttemptedAt DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_LoginAttempts PRIMARY KEY CLUSTERED (Id)
    );

    CREATE NONCLUSTERED INDEX IX_LoginAttempts_Username ON dbo.LoginAttempts (Username, AttemptedAt DESC);

    PRINT 'Table LoginAttempts created.';
END
GO


/* ----------------------------------------------------------------------------
   4. NEWSLETTER SUBSCRIBERS TABLE
   Backs the "Join the TechnoZone Dispatch" form on the home page.
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'NewsletterSubscribers')
BEGIN
    CREATE TABLE dbo.NewsletterSubscribers
    (
        Id           INT           IDENTITY(1,1) NOT NULL,
        Email        NVARCHAR(100) NOT NULL,
        SubscribedAt DATETIME      NOT NULL CONSTRAINT DF_Newsletter_SubscribedAt DEFAULT (GETUTCDATE()),
        IsActive     BIT           NOT NULL CONSTRAINT DF_Newsletter_IsActive     DEFAULT (1),

        CONSTRAINT PK_NewsletterSubscribers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Newsletter_Email      UNIQUE (Email)
    );

    PRINT 'Table NewsletterSubscribers created.';
END
GO


/* ----------------------------------------------------------------------------
   5. STORED PROCEDURES
   The C# code can call these instead of writing inline SQL. Each one is
   dropped and recreated so the script stays re-runnable.
   ---------------------------------------------------------------------------- */

/* 5.1 Register a new user.
   Returns 1 in @Result when the account was created,
           0 when the username or email is already taken. */
IF OBJECT_ID(N'dbo.sp_RegisterUser', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegisterUser;
GO

CREATE PROCEDURE dbo.sp_RegisterUser
    @Username     NVARCHAR(50),
    @Email        NVARCHAR(100),
    @PasswordHash NVARCHAR(256),
    @FirstName    NVARCHAR(100) = NULL,
    @LastName     NVARCHAR(100) = NULL,
    @Result       INT OUTPUT,
    @NewUserId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username OR Email = @Email)
    BEGIN
        SET @Result    = 0;
        SET @NewUserId = 0;
        RETURN;
    END

    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName, CreatedAt, IsActive)
    VALUES (@Username, @Email, @PasswordHash, @FirstName, @LastName, GETUTCDATE(), 1);

    SET @NewUserId = SCOPE_IDENTITY();
    SET @Result    = 1;
END
GO


/* 5.2 Fetch a user by username so the app can compare password hashes.
   Returns no rows when the username does not exist or the account is disabled. */
IF OBJECT_ID(N'dbo.sp_GetUserByUsername', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserByUsername;
GO

CREATE PROCEDURE dbo.sp_GetUserByUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Username, Email, PasswordHash, FirstName, LastName, CreatedAt, LastLogin, IsActive
    FROM   dbo.Users
    WHERE  Username = @Username
      AND  IsActive = 1;
END
GO


/* 5.3 Check whether a username is already taken (used by the live JavaScript check). */
IF OBJECT_ID(N'dbo.sp_IsUsernameTaken', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_IsUsernameTaken;
GO

CREATE PROCEDURE dbo.sp_IsUsernameTaken
    @Username NVARCHAR(50),
    @Taken    BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Taken = CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username) THEN 1 ELSE 0 END;
END
GO


/* 5.4 Check whether an email is already registered. */
IF OBJECT_ID(N'dbo.sp_IsEmailTaken', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_IsEmailTaken;
GO

CREATE PROCEDURE dbo.sp_IsEmailTaken
    @Email NVARCHAR(100),
    @Taken BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Taken = CASE WHEN EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @Email) THEN 1 ELSE 0 END;
END
GO


/* 5.5 Stamp the time of a successful sign-in. */
IF OBJECT_ID(N'dbo.sp_UpdateLastLogin', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateLastLogin;
GO

CREATE PROCEDURE dbo.sp_UpdateLastLogin
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users SET LastLogin = GETUTCDATE() WHERE Id = @UserId;
END
GO


/* 5.6 Record a sign-in attempt in the audit table. */
IF OBJECT_ID(N'dbo.sp_LogLoginAttempt', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_LogLoginAttempt;
GO

CREATE PROCEDURE dbo.sp_LogLoginAttempt
    @Username  NVARCHAR(50),
    @IsSuccess BIT,
    @IpAddress NVARCHAR(45) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.LoginAttempts (Username, IsSuccess, IpAddress, AttemptedAt)
    VALUES (@Username, @IsSuccess, @IpAddress, GETUTCDATE());
END
GO


/* 5.7 Add a newsletter subscriber, or reactivate one who unsubscribed. */
IF OBJECT_ID(N'dbo.sp_SubscribeNewsletter', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SubscribeNewsletter;
GO

CREATE PROCEDURE dbo.sp_SubscribeNewsletter
    @Email  NVARCHAR(100),
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.NewsletterSubscribers WHERE Email = @Email)
    BEGIN
        UPDATE dbo.NewsletterSubscribers SET IsActive = 1 WHERE Email = @Email;
        SET @Result = 0;   -- already on the list
        RETURN;
    END

    INSERT INTO dbo.NewsletterSubscribers (Email) VALUES (@Email);
    SET @Result = 1;       -- newly added
END
GO


/* ----------------------------------------------------------------------------
   6. VIEW: user accounts without the password hash
   Safe to open in SSMS when demonstrating the project.
   ---------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.vw_UserAccounts', N'V') IS NOT NULL
    DROP VIEW dbo.vw_UserAccounts;
GO

CREATE VIEW dbo.vw_UserAccounts
AS
    SELECT  Id,
            Username,
            Email,
            FirstName + ' ' + LastName AS FullName,
            CreatedAt,
            LastLogin,
            CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Disabled' END AS Status
    FROM    dbo.Users;
GO


/* ----------------------------------------------------------------------------
   7. SAMPLE DATA
   The hashes below are real SHA-256 + Base64 values, so these accounts can
   actually sign in. Delete this section if you want an empty database.
   ---------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'testuser')
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName)
    VALUES (N'testuser', N'test@technozone.com', N'h3bxCOJHqx4rMjBCwEnCZkB8gfutQb3h6N/Bu2b9Jn4=', N'Test', N'User');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'johndoe')
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName)
    VALUES (N'johndoe', N'john@technozone.com', N'D03WxnvIyCeisYG8dj+auWFm2PUIQP4a4LvA53Rk2iw=', N'John', N'Doe');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FirstName, LastName)
    VALUES (N'admin', N'admin@technozone.com', N'6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', N'Site', N'Admin');
GO


/* ----------------------------------------------------------------------------
   8. VERIFY
   ---------------------------------------------------------------------------- */
PRINT '--- TechnoZoneDB setup finished ---';

SELECT * FROM dbo.vw_UserAccounts;
SELECT TOP 20 * FROM dbo.LoginAttempts ORDER BY AttemptedAt DESC;
SELECT * FROM dbo.NewsletterSubscribers;
GO
