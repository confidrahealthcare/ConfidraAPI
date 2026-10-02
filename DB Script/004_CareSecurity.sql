-- Review against a staging SQL Server backup first. Never auto-run on application start.
-- Additive migration: no existing patient or payment rows are deleted.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.Users','U') IS NULL THROW 51000, 'Apply the existing baseline schema first.', 1;
IF COL_LENGTH('dbo.Users','Role') IS NULL ALTER TABLE dbo.Users ADD Role nvarchar(30) NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Patient';
IF COL_LENGTH('dbo.Users','ProfessionalVerified') IS NULL ALTER TABLE dbo.Users ADD ProfessionalVerified bit NOT NULL CONSTRAINT DF_Users_Verified DEFAULT 0;
IF COL_LENGTH('dbo.Users','FailedLogins') IS NULL ALTER TABLE dbo.Users ADD FailedLogins int NOT NULL CONSTRAINT DF_Users_Failed DEFAULT 0;
IF COL_LENGTH('dbo.Users','LockedUntilUtc') IS NULL ALTER TABLE dbo.Users ADD LockedUntilUtc datetime2 NULL;
IF COL_LENGTH('dbo.Users','ResetAttempts') IS NULL ALTER TABLE dbo.Users ADD ResetAttempts int NOT NULL CONSTRAINT DF_Users_ResetAttempts DEFAULT 0;
IF OBJECT_ID('dbo.CareConsents','U') IS NULL BEGIN
 CREATE TABLE dbo.CareConsents(Id int IDENTITY PRIMARY KEY,UserId int NOT NULL REFERENCES dbo.Users(Id),Purpose nvarchar(30) NOT NULL,Version nvarchar(30) NOT NULL,GrantedUtc datetime2 NOT NULL,WithdrawnUtc datetime2 NULL);
 CREATE UNIQUE INDEX IX_CareConsents_UserId_Purpose ON dbo.CareConsents(UserId,Purpose) WHERE WithdrawnUtc IS NULL;
END;
IF OBJECT_ID('dbo.PatientIntakes','U') IS NULL CREATE TABLE dbo.PatientIntakes(UserId int PRIMARY KEY REFERENCES dbo.Users(Id),City nvarchar(100) NOT NULL,DiabetesType nvarchar(40) NOT NULL,Duration nvarchar(100) NOT NULL,Medicines nvarchar(2000) NOT NULL,OtherConditions nvarchar(2000) NOT NULL,UpdatedUtc datetime2 NOT NULL);
IF OBJECT_ID('dbo.DailyLogs','U') IS NULL BEGIN
 CREATE TABLE dbo.DailyLogs(Id int IDENTITY PRIMARY KEY,UserId int NOT NULL REFERENCES dbo.Users(Id),Date date NOT NULL,FastingGlucose decimal(7,2) NULL,Systolic int NULL,Diastolic int NULL,WeightKg decimal(6,2) NULL,Energy int NULL,UpdatedUtc datetime2 NOT NULL);
 CREATE UNIQUE INDEX IX_DailyLogs_UserId_Date ON dbo.DailyLogs(UserId,Date);
END;
IF OBJECT_ID('dbo.CareAssignments','U') IS NULL BEGIN
 CREATE TABLE dbo.CareAssignments(Id int IDENTITY PRIMARY KEY,PatientId int NOT NULL REFERENCES dbo.Users(Id),StaffId int NOT NULL REFERENCES dbo.Users(Id));
 CREATE UNIQUE INDEX IX_CareAssignments_PatientId_StaffId ON dbo.CareAssignments(PatientId,StaffId);
END;
IF OBJECT_ID('dbo.ClinicalReviews','U') IS NULL CREATE TABLE dbo.ClinicalReviews(Id int IDENTITY PRIMARY KEY,PatientId int NOT NULL REFERENCES dbo.Users(Id),PhysicianId int NOT NULL REFERENCES dbo.Users(Id),Day int NOT NULL,Fbs decimal(7,2) NULL,Ppbs decimal(7,2) NULL,HbA1c decimal(5,2) NULL,Interpretation nvarchar(4000) NOT NULL,RecordedUtc datetime2 NOT NULL);
IF OBJECT_ID('dbo.Referrals','U') IS NULL CREATE TABLE dbo.Referrals(Id int IDENTITY PRIMARY KEY,ReferrerId int NOT NULL REFERENCES dbo.Users(Id),PatientName nvarchar(150) NOT NULL,Phone nvarchar(30) NOT NULL,ConsentConfirmedUtc datetime2 NOT NULL,Status nvarchar(30) NOT NULL,CreatedUtc datetime2 NOT NULL);
IF OBJECT_ID('dbo.AuditEvents','U') IS NULL CREATE TABLE dbo.AuditEvents(Id bigint IDENTITY PRIMARY KEY,ActorId int NOT NULL,SubjectId int NULL,Action nvarchar(60) NOT NULL,CreatedUtc datetime2 NOT NULL);
IF OBJECT_ID('dbo.PaymentRecords','U') IS NULL BEGIN
 CREATE TABLE dbo.PaymentRecords(OrderId nvarchar(80) PRIMARY KEY,UserId int NOT NULL REFERENCES dbo.Users(Id),ProgrammeId nvarchar(40) NOT NULL,AmountPaise int NOT NULL,PaymentId nvarchar(80) NULL,Status nvarchar(25) NOT NULL,CreatedUtc datetime2 NOT NULL);
 CREATE UNIQUE INDEX IX_PaymentRecords_PaymentId ON dbo.PaymentRecords(PaymentId) WHERE PaymentId IS NOT NULL;
END;
IF OBJECT_ID('dbo.ProviderEvents','U') IS NULL CREATE TABLE dbo.ProviderEvents(Id nvarchar(150) PRIMARY KEY,ProcessedUtc datetime2 NOT NULL);
COMMIT;
-- Roll back application binaries if needed; retain additive tables and audit data.
-- Do not revert to the unauthenticated API baseline on a publicly reachable host.
