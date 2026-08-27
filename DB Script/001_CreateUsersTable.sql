IF DB_ID(N'ConfidraDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [ConfidraDb]');
END;
GO

USE [ConfidraDb];
GO

IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Users]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Users] PRIMARY KEY,
        [FullName] NVARCHAR(150) NOT NULL,
        [Email] NVARCHAR(320) NOT NULL,
        [Phone] NVARCHAR(30) NOT NULL,
        [PasswordHash] NVARCHAR(500) NOT NULL,
        [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Users_CreatedUtc] DEFAULT (SYSUTCDATETIME()),
        [PasswordResetOtpHash] NVARCHAR(64) NULL,
        [PasswordResetOtpExpiresUtc] DATETIME2(7) NULL
    );
END;
GO

IF COL_LENGTH(N'[dbo].[Users]', N'PasswordResetOtpHash') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PasswordResetOtpHash] NVARCHAR(64) NULL;
END;
GO

IF COL_LENGTH(N'[dbo].[Users]', N'PasswordResetOtpExpiresUtc') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users] ADD [PasswordResetOtpExpiresUtc] DATETIME2(7) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Email' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [dbo].[Users]([Email]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Phone' AND object_id = OBJECT_ID(N'[dbo].[Users]'))
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Phone] ON [dbo].[Users]([Phone]);
END;
GO