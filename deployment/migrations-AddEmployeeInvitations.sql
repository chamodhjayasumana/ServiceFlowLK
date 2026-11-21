BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163055_AddEmployeeInvitations'
)
BEGIN
    CREATE TABLE [EmployeeInvitations] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Email] nvarchar(256) NOT NULL,
        [RoleName] nvarchar(64) NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [InvitedByUserId] uniqueidentifier NOT NULL,
        [ExpiresAtUtc] datetimeoffset NOT NULL,
        [AcceptedAtUtc] datetimeoffset NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAtUtc] datetimeoffset NOT NULL,
        [UpdatedAtUtc] datetimeoffset NULL,
        CONSTRAINT [PK_EmployeeInvitations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeInvitations_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163055_AddEmployeeInvitations'
)
BEGIN
    CREATE INDEX [IX_EmployeeInvitations_TenantId_Email_Status] ON [EmployeeInvitations] ([TenantId], [Email], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163055_AddEmployeeInvitations'
)
BEGIN
    CREATE UNIQUE INDEX [IX_EmployeeInvitations_TokenHash] ON [EmployeeInvitations] ([TokenHash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261121163055_AddEmployeeInvitations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261121163055_AddEmployeeInvitations', N'8.0.11');
END;
GO

COMMIT;
GO

