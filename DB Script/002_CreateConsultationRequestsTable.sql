IF DB_ID(N'ConfidraDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [ConfidraDb]');
END;
GO

USE [ConfidraDb];
GO

IF OBJECT_ID(N'[dbo].[ConsultationRequests]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ConsultationRequests]
    (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ConsultationRequests] PRIMARY KEY,
        [FullName] NVARCHAR(150) NOT NULL,
        [Phone] NVARCHAR(30) NOT NULL,
        [Email] NVARCHAR(320) NOT NULL,
        [CreatedUtc] DATETIME2(7) NOT NULL CONSTRAINT [DF_ConsultationRequests_CreatedUtc] DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX [IX_ConsultationRequests_CreatedUtc]
        ON [dbo].[ConsultationRequests]([CreatedUtc]);
END;
GO