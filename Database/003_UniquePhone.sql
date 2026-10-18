USE [ContactManagerDb];
GO

-- Existing databases must resolve duplicate phones before enabling the constraint.
IF EXISTS (SELECT Phone FROM dbo.Contacts GROUP BY Phone HAVING COUNT(*) > 1)
    THROW 51000, 'Duplicate phone values exist. Deduplicate dbo.Contacts before running this migration.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Contacts_Phone' AND object_id = OBJECT_ID(N'dbo.Contacts'))
    CREATE UNIQUE INDEX UX_Contacts_Phone ON dbo.Contacts(Phone);
GO
