IF DB_ID(N'ConfidraDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [ConfidraDb]');
END;
GO

USE [ConfidraDb];
GO

IF OBJECT_ID(N'[dbo].[Appointments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Appointments]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Appointments] PRIMARY KEY,
        [UserId] INT NULL,
        [PlanName] NVARCHAR(100) NOT NULL,
        [PaymentId] NVARCHAR(100) NOT NULL,
        [AppointmentDate] DATETIME2(7) NOT NULL,
        [AppointmentTime] NVARCHAR(20) NOT NULL,
        [DoctorName] NVARCHAR(150) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_Appointments_Status] DEFAULT (N'Confirmed'),
        [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_Appointments_CreatedUtc] DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX [IX_Appointments_AppointmentDate]
        ON [dbo].[Appointments]([AppointmentDate]);
END;
GO

IF COL_LENGTH(N'[dbo].[Appointments]', N'AppointmentTime') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [AppointmentTime] NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH(N'[dbo].[Appointments]', N'DoctorName') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [DoctorName] NVARCHAR(150) NULL;
END;
GO

IF COL_LENGTH(N'[dbo].[Appointments]', N'Status') IS NULL
BEGIN
    ALTER TABLE [dbo].[Appointments] ADD [Status] NVARCHAR(20) NULL;
END;
GO

UPDATE [dbo].[Appointments]
SET [Status] = ISNULL([Status], N'Confirmed');
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = N'Status' AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[Appointments] ALTER COLUMN [Status] NVARCHAR(20) NOT NULL;
END;
GO

UPDATE [dbo].[Appointments]
SET [AppointmentTime] = ISNULL([AppointmentTime], N'Not specified'),
    [DoctorName] = ISNULL([DoctorName], N'Assigned doctor');
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = N'AppointmentTime' AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[Appointments] ALTER COLUMN [AppointmentTime] NVARCHAR(20) NOT NULL;
END;
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Appointments]') AND name = N'DoctorName' AND is_nullable = 1)
BEGIN
    ALTER TABLE [dbo].[Appointments] ALTER COLUMN [DoctorName] NVARCHAR(150) NOT NULL;
END;
GO

IF OBJECT_ID(N'[dbo].[Enrollments]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Enrollments]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Enrollments] PRIMARY KEY,
        [UserId] INT NULL,
        [PlanName] NVARCHAR(100) NOT NULL,
        [PaymentId] NVARCHAR(100) NOT NULL CONSTRAINT [UQ_Enrollments_PaymentId] UNIQUE,
        [EnrolledUtc] DATETIME2(7) NOT NULL,
        [ExpiresUtc] DATETIME2(7) NOT NULL
    );
END;
GO