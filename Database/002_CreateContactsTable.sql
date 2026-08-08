USE [ContactManagerDb];
GO

IF OBJECT_ID(N'dbo.Contacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contacts
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Contacts PRIMARY KEY,

        [Name] NVARCHAR(200) NOT NULL,

        DateOfBirth DATE NOT NULL,

        Married BIT NOT NULL,

        Phone NVARCHAR(50) NOT NULL,

        Salary DECIMAL(18,2) NOT NULL,

        CONSTRAINT CK_Contacts_Name_NotBlank
            CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),

        CONSTRAINT CK_Contacts_Phone_NotBlank
            CHECK (LEN(LTRIM(RTRIM(Phone))) > 0),

        CONSTRAINT CK_Contacts_Salary_NonNegative
            CHECK (Salary >= 0)
    );
END
GO