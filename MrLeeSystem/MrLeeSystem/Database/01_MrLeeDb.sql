/*
  Mr Lee - Script SQL Server (DB + tablas principales)
  Módulos incluidos: Seguimiento de pedidos, Inventario, Usuarios/Accesos + Bitácora.

  Nota:
  - Este script crea la base de datos y el esquema básico.
  - El proyecto también incluye EF Core (AppDbContext). Si prefiere migraciones:
      1) Configure connection string en appsettings.json
      2) Ejecute: dotnet ef database update
*/

IF DB_ID('MrLeeDb') IS NULL
BEGIN
    CREATE DATABASE MrLeeDb;
END
GO

USE MrLeeDb;
GO

/*  Seguridad: Roles / Permisos / Usuarios  */

IF OBJECT_ID('dbo.Roles','U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE,
        IsActive BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT(1)
    );
END
GO

IF OBJECT_ID('dbo.Permissions','U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Permissions_IsActive DEFAULT(1)
    );
END
GO

IF OBJECT_ID('dbo.RolePermissions','U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions(
        RoleId INT NOT NULL,
        PermissionId INT NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY(RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(Id),
        CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY(PermissionId) REFERENCES dbo.Permissions(Id)
    );
END
GO

IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName NVARCHAR(150) NOT NULL,
        Email NVARCHAR(200) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(500) NOT NULL,
        FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_Failed DEFAULT(0),
        LockoutEndUtc DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
        MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT(0),
        TemporaryPasswordIssuedUtc DATETIME2 NULL,
        RoleId INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Users_Created DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(Id)
    );
END
GO

IF COL_LENGTH('dbo.Users', 'MustChangePassword') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT(0);
END
GO

IF COL_LENGTH('dbo.Users', 'TemporaryPasswordIssuedUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD TemporaryPasswordIssuedUtc DATETIME2 NULL;
END
GO

IF OBJECT_ID('dbo.ActionLogs','U') IS NULL
BEGIN
    CREATE TABLE dbo.ActionLogs(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AtUtc DATETIME2 NOT NULL CONSTRAINT DF_ActionLogs_At DEFAULT(SYSUTCDATETIME()),
        ActorUserId INT NULL,
        ActorEmail NVARCHAR(200) NOT NULL,
        Action NVARCHAR(80) NOT NULL,
        Entity NVARCHAR(80) NOT NULL,
        EntityId NVARCHAR(50) NOT NULL,
        DetailJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ActionLogs_Detail DEFAULT('{}'),
        IpAddress NVARCHAR(60) NOT NULL CONSTRAINT DF_ActionLogs_Ip DEFAULT(''),
        CONSTRAINT FK_ActionLogs_Users FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_ActionLogs_AtUtc ON dbo.ActionLogs(AtUtc DESC);
END
GO

/*  Inventario  */

IF OBJECT_ID('dbo.Products','U') IS NULL
BEGIN
    CREATE TABLE dbo.Products(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Sku NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Unit NVARCHAR(30) NOT NULL CONSTRAINT DF_Products_Unit DEFAULT('unidad'),
        UnitPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_UnitPrice DEFAULT(0),
        CurrentStock DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_CurrentStock DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT(1),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Products_Created DEFAULT(SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID('dbo.StockMovements','U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovements(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProductId INT NOT NULL,
        Type INT NOT NULL, -- 1=Entry, 2=Exit, 3=Adjustment
        Quantity DECIMAL(18,2) NOT NULL,
        Reason NVARCHAR(300) NOT NULL CONSTRAINT DF_StockMovements_Reason DEFAULT(''),
        AtUtc DATETIME2 NOT NULL CONSTRAINT DF_StockMovements_At DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId INT NULL,
        CreatedByEmail NVARCHAR(200) NOT NULL CONSTRAINT DF_StockMovements_Email DEFAULT(''),
        CONSTRAINT FK_StockMovements_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT FK_StockMovements_Users FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_StockMovements_Type CHECK (Type IN (1,2,3))
    );
    CREATE INDEX IX_StockMovements_ProductId_AtUtc ON dbo.StockMovements(ProductId, AtUtc DESC);
END
GO


/*  Ingresos operativos  */

IF OBJECT_ID('dbo.OperatingIncomes','U') IS NULL
BEGIN
    CREATE TABLE dbo.OperatingIncomes(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Number NVARCHAR(30) NOT NULL UNIQUE,
        IncomeDate DATE NOT NULL,
        Type INT NOT NULL, -- 1=Venta, 2=Cobro, 3=Otro
        Category NVARCHAR(150) NOT NULL,
        SourceName NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NOT NULL CONSTRAINT DF_OperatingIncomes_Description DEFAULT(''),
        Reference NVARCHAR(50) NOT NULL CONSTRAINT DF_OperatingIncomes_Reference DEFAULT(''),
        Amount DECIMAL(18,2) NOT NULL,
        IsVoided BIT NOT NULL CONSTRAINT DF_OperatingIncomes_IsVoided DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_OperatingIncomes_Created DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        VoidedAtUtc DATETIME2 NULL,
        CreatedByUserId INT NULL,
        CreatedByEmail NVARCHAR(200) NOT NULL CONSTRAINT DF_OperatingIncomes_CreatedEmail DEFAULT(''),
        UpdatedByUserId INT NULL,
        UpdatedByEmail NVARCHAR(200) NOT NULL CONSTRAINT DF_OperatingIncomes_UpdatedEmail DEFAULT(''),
        CONSTRAINT FK_OperatingIncomes_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_OperatingIncomes_UpdatedBy FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_OperatingIncomes_Type CHECK (Type IN (1,2,3)),
        CONSTRAINT CK_OperatingIncomes_Amount CHECK (Amount > 0)
    );
    CREATE INDEX IX_OperatingIncomes_IncomeDate ON dbo.OperatingIncomes(IncomeDate DESC);
END
GO

IF OBJECT_ID('dbo.OperatingIncomeAttachments','U') IS NULL
BEGIN
    CREATE TABLE dbo.OperatingIncomeAttachments(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OperatingIncomeId BIGINT NOT NULL,
        OriginalFileName NVARCHAR(260) NOT NULL,
        StoredFileName NVARCHAR(300) NOT NULL,
        RelativePath NVARCHAR(400) NOT NULL,
        ContentType NVARCHAR(120) NOT NULL,
        SizeBytes BIGINT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_OperatingIncomeAttachments_Created DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId INT NULL,
        CreatedByEmail NVARCHAR(200) NOT NULL CONSTRAINT DF_OperatingIncomeAttachments_Email DEFAULT(''),
        CONSTRAINT FK_OperatingIncomeAttachments_Income FOREIGN KEY(OperatingIncomeId) REFERENCES dbo.OperatingIncomes(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OperatingIncomeAttachments_User FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_OperatingIncomeAttachments_OperatingIncomeId ON dbo.OperatingIncomeAttachments(OperatingIncomeId);
END
GO

IF OBJECT_ID('dbo.AccountingPeriods','U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountingPeriods(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Year] INT NOT NULL,
        [Month] INT NOT NULL,
        [Status] INT NOT NULL CONSTRAINT DF_AccountingPeriods_Status DEFAULT(1), -- 1=Abierto, 2=Cerrado
        ClosedAtUtc DATETIME2 NULL,
        ClosedByUserId INT NULL,
        ClosedByEmail NVARCHAR(200) NOT NULL CONSTRAINT DF_AccountingPeriods_ClosedByEmail DEFAULT(''),
        Notes NVARCHAR(300) NOT NULL CONSTRAINT DF_AccountingPeriods_Notes DEFAULT(''),
        CONSTRAINT UQ_AccountingPeriods UNIQUE([Year], [Month]),
        CONSTRAINT FK_AccountingPeriods_User FOREIGN KEY(ClosedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_AccountingPeriods_Month CHECK ([Month] BETWEEN 1 AND 12),
        CONSTRAINT CK_AccountingPeriods_Status CHECK ([Status] IN (1,2))
    );
END
GO

/*  Seguimiento de pedidos  */

IF OBJECT_ID('dbo.Orders','U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TrackingNumber NVARCHAR(40) NOT NULL UNIQUE,
        CustomerName NVARCHAR(150) NOT NULL,
        CustomerPhone NVARCHAR(50) NOT NULL,
        DeliveryAddress NVARCHAR(300) NOT NULL,
        Notes NVARCHAR(500) NOT NULL CONSTRAINT DF_Orders_Notes DEFAULT(''),
        Status INT NOT NULL CONSTRAINT DF_Orders_Status DEFAULT(1), -- 1=Recibido
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Orders_Created DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT CK_Orders_Status CHECK (Status IN (1,2,3,4,5))
    );
    CREATE INDEX IX_Orders_CreatedAtUtc ON dbo.Orders(CreatedAtUtc DESC);
END
GO

IF OBJECT_ID('dbo.OrderItems','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderId BIGINT NOT NULL,
        ProductId INT NOT NULL,
        Quantity DECIMAL(18,2) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY(OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY(ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT CK_OrderItems_Qty CHECK (Quantity > 0)
    );
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
END
GO

IF OBJECT_ID('dbo.OrderStatusHistory','U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderStatusHistory(
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderId BIGINT NOT NULL,
        Status INT NOT NULL,
        Comment NVARCHAR(300) NOT NULL CONSTRAINT DF_OrderStatusHistory_Comment DEFAULT(''),
        AtUtc DATETIME2 NOT NULL CONSTRAINT DF_OrderStatusHistory_At DEFAULT(SYSUTCDATETIME()),
        ChangedByUserId INT NULL,
        ChangedByEmail NVARCHAR(200) NOT NULL CONSTRAINT DF_OrderStatusHistory_Email DEFAULT(''),
        CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY(OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OrderStatusHistory_Users FOREIGN KEY(ChangedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_OrderStatusHistory_Status CHECK (Status IN (1,2,3,4,5))
    );
    CREATE INDEX IX_OrderStatusHistory_OrderId_AtUtc ON dbo.OrderStatusHistory(OrderId, AtUtc DESC);
END
GO

/*  Seed mínimo (Roles + Permisos)  */

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Name = 'Administrador')
BEGIN
    INSERT INTO dbo.Roles(Name, IsActive) VALUES
    ('Administrador',1),
    ('Ventas',1),
    ('Bodega',1),
    ('Despacho',1);
END
GO

MERGE dbo.Permissions AS target
USING (VALUES
    ('USR.VIEW','Ver usuarios',1),
    ('USR.MANAGE','Administrar usuarios',1),
    ('USR.AUDIT','Ver bitácora',1),
    ('INV.VIEW','Ver inventario',1),
    ('INV.MANAGE','Administrar productos',1),
    ('INV.MOVEMENTS','Registrar movimientos inventario',1),
    ('ORD.VIEW','Ver pedidos',1),
    ('ORD.MANAGE','Administrar pedidos',1),
    ('ORD.STATUS','Actualizar estado del pedido',1),
    ('ING.VIEW','Ver ingresos operativos',1),
    ('ING.MANAGE','Administrar ingresos operativos',1),
    ('ING.AUDIT','Ver trazabilidad de ingresos',1)
) AS src(Code, Description, IsActive)
ON target.Code = src.Code
WHEN NOT MATCHED THEN
    INSERT(Code, Description, IsActive) VALUES(src.Code, src.Description, src.IsActive);
GO

-- Dar todos los permisos al Administrador
DECLARE @AdminRoleId INT = (SELECT TOP 1 Id FROM dbo.Roles WHERE Name='Administrador');
INSERT INTO dbo.RolePermissions(RoleId, PermissionId)
SELECT @AdminRoleId, p.Id
FROM dbo.Permissions p
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.RolePermissions rp
    WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id
);
GO

-- ============================================================
--  MR LEE SYSTEM – INSERTs para tabla OperatingIncome
--  Ejecutar en SQL Server contra la base de datos de Mr Lee
--  Incluye: Ventas, Cobros y Otros (tipos del enum)
--  CreatedByUserId = 1 (admin@mrlee.local)
-- ============================================================
 
SET IDENTITY_INSERT OperatingIncomes ON;
 
INSERT INTO OperatingIncomes
    (Id, Number, IncomeDate, Type, Category, SourceName, Description, Reference, Amount,
     IsVoided, CreatedAtUtc, UpdatedAtUtc, VoidedAtUtc, CreatedByUserId, CreatedByEmail,
     UpdatedByUserId, UpdatedByEmail)
VALUES
 
-- ── Ventas (Type = 1) ──────────────────────────────────────
(1,  'ING-2026-001', '2026-01-05', 1, 'Ventas',  'Pedido mostrador',      'Venta de pan baguette y croissants al mostrador',     'TRK-20260105-001', 18500.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(2,  'ING-2026-002', '2026-01-08', 1, 'Ventas',  'Pedido en línea',       'Venta de empanadas y galletas por WhatsApp',           'TRK-20260108-002', 12300.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(3,  'ING-2026-003', '2026-01-12', 1, 'Ventas',  'Evento corporativo',    'Catering para reunión empresa TechCorp S.A.',          'EVT-TECHCORP-01',  85000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(4,  'ING-2026-004', '2026-01-15', 1, 'Ventas',  'Pedido mostrador',      'Venta de pan dulce y café',                            'TRK-20260115-003', 9800.00,  0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(5,  'ING-2026-005', '2026-01-20', 1, 'Ventas',  'Cumpleaños cliente',    'Torta personalizada para María González',              'ORD-MGZ-001',      32000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
 
(6,  'ING-2026-006', '2026-02-03', 1, 'Ventas',  'Pedido mostrador',      'Venta de empanadas de queso y pollo',                  'TRK-20260203-001', 14700.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(7,  'ING-2026-007', '2026-02-10', 1, 'Ventas',  'Pedido en línea',       'Pedido surtido para oficina – Juan Mora',              'TRK-20260210-004', 27500.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(8,  'ING-2026-008', '2026-02-14', 1, 'Ventas',  'San Valentín',          'Canasta de postres especiales San Valentín',           'SV-2026-001',      55000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(9,  'ING-2026-009', '2026-02-20', 1, 'Ventas',  'Pedido mostrador',      'Pan integral y semillas – cliente frecuente',          'TRK-20260220-002', 8900.00,  0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(10, 'ING-2026-010', '2026-02-25', 1, 'Ventas',  'Pedido en línea',       'Caja variada de pasteles franceses',                   'TRK-20260225-005', 19200.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
 
(11, 'ING-2026-011', '2026-03-02', 1, 'Ventas',  'Pedido mostrador',      'Venta croissants mantequilla lunes',                   'TRK-20260302-001', 11400.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(12, 'ING-2026-012', '2026-03-10', 1, 'Ventas',  'Evento escolar',        'Meriendas para Colegio Fidelitas – 50 unidades',       'EVT-COL-2026-01',  42000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(13, 'ING-2026-013', '2026-03-15', 1, 'Ventas',  'Pedido en línea',       'Pack desayuno semanal – cliente Byron',                'TRK-20260315-003', 16800.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
 
-- ── Cobros (Type = 2) ─────────────────────────────────────
(14, 'ING-2026-014', '2026-01-18', 2, 'Cobros',  'Cuenta por cobrar',     'Cobro parcial factura enero – Supermercado El Trigo',  'FAC-ETR-2026-01',  60000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(15, 'ING-2026-015', '2026-01-28', 2, 'Cobros',  'Cuenta por cobrar',     'Saldo pendiente catering TechCorp S.A.',               'EVT-TECHCORP-01',  40000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(16, 'ING-2026-016', '2026-02-06', 2, 'Cobros',  'Cuenta por cobrar',     'Cobro factura distribución – Colegio San José',        'FAC-CSJ-2026-02',  33000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(17, 'ING-2026-017', '2026-02-22', 2, 'Cobros',  'Cuenta por cobrar',     'Cobro servicio mensual – cafetería corporativa',       'SERV-CAFE-FEB26',  75000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(18, 'ING-2026-018', '2026-03-05', 2, 'Cobros',  'Cuenta por cobrar',     'Cobro evento escolar Fidelitas',                       'EVT-COL-2026-01',  42000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
 
-- ── Otros (Type = 3) ──────────────────────────────────────
(19, 'ING-2026-019', '2026-01-10', 3, 'Otros',   'Devolución proveedor',  'Nota de crédito harina – Proveedor Molinos S.A.',      'NC-MOL-2026-01',   5500.00,  0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(20, 'ING-2026-020', '2026-02-01', 3, 'Otros',   'Alquiler espacio',      'Alquiler vitrina externa – enero 2026',                'ALQ-VIT-ENE26',    15000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(21, 'ING-2026-021', '2026-03-01', 3, 'Otros',   'Alquiler espacio',      'Alquiler vitrina externa – febrero 2026',              'ALQ-VIT-FEB26',    15000.00, 0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
(22, 'ING-2026-022', '2026-03-08', 3, 'Otros',   'Ajuste contable',       'Diferencial cambiario – depósito USD cliente externo', 'ADJ-USD-MAR26',    3200.00,  0, GETUTCDATE(), NULL, NULL, 1, 'admin@mrlee.local', NULL, ''),
 
-- ── Registro ANULADO (para probar filtro "Incluir anulados") ──
(23, 'ING-2026-023', '2026-02-12', 1, 'Ventas',  'Pedido cancelado',      'Torta cancelada – cliente no recogió',                 'TRK-20260212-X',   28000.00, 1, GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 'admin@mrlee.local', 1, 'admin@mrlee.local');
 
SET IDENTITY_INSERT OperatingIncomes OFF;
 
-- ============================================================
-- Verificar inserción
-- ============================================================
SELECT Id, Number, IncomeDate, Type, SourceName, Amount, IsVoided
FROM   OperatingIncomes
ORDER  BY IncomeDate;