USE MrLeeDb;

-- 1. Tabla Clientes
IF OBJECT_ID('dbo.Clientes','U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id                          INT IDENTITY(1,1) PRIMARY KEY,
        Nombre                      NVARCHAR(100)  NOT NULL DEFAULT '',
        Apellido                    NVARCHAR(100)  NOT NULL DEFAULT '',
        Email                       NVARCHAR(200)  NOT NULL,
        PasswordHash                NVARCHAR(500)  NOT NULL DEFAULT '',
        TipoIdentificacion          INT            NOT NULL DEFAULT 1,
        Identificacion              NVARCHAR(30)   NOT NULL DEFAULT '',
        Telefono                    NVARCHAR(20)   NOT NULL DEFAULT '',
        EmailVerificado             BIT            NOT NULL DEFAULT 0,
        TelefonoVerificado          BIT            NOT NULL DEFAULT 0,
        IsActive                    BIT            NOT NULL DEFAULT 1,
        TokenVerificacion           NVARCHAR(300)  NOT NULL DEFAULT '',
        TokenExpiraUtc              DATETIME2      NULL,
        TokenRecuperacion           NVARCHAR(300)  NOT NULL DEFAULT '',
        TokenRecupExpiraUtc         DATETIME2      NULL,
        FailedLoginCount            INT            NOT NULL DEFAULT 0,
        LockoutEndUtc               DATETIME2      NULL,
        DadoDeBaja                  BIT            NOT NULL DEFAULT 0,
        FechaBaja                   DATETIME2      NULL,
        MotivoBaja                  NVARCHAR(300)  NOT NULL DEFAULT '',
        TokenReactivacion           NVARCHAR(300)  NOT NULL DEFAULT '',
        TokenReactivacionExpiraUtc  DATETIME2      NULL,
        NotiEmail                   BIT            NOT NULL DEFAULT 1,
        NotiSms                     BIT            NOT NULL DEFAULT 0,
        NotiWhatsapp                BIT            NOT NULL DEFAULT 0,
        HoraSilencioInicio          TIME           NULL,
        HoraSilencioFin             TIME           NULL,
        CreatedAtUtc                DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc                DATETIME2      NULL
    );
    CREATE UNIQUE INDEX UX_Clientes_Email ON dbo.Clientes (Email);
END;

-- 2. Direcciones del cliente
IF OBJECT_ID('dbo.DireccionesCliente','U') IS NULL
BEGIN
    CREATE TABLE dbo.DireccionesCliente (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        ClienteId    INT           NOT NULL,
        Provincia    NVARCHAR(100) NOT NULL DEFAULT '',
        Canton       NVARCHAR(100) NOT NULL DEFAULT '',
        Distrito     NVARCHAR(100) NOT NULL DEFAULT '',
        Direccion    NVARCHAR(300) NOT NULL DEFAULT '',
        CodPostal    NVARCHAR(10)  NOT NULL DEFAULT '',
        Lat          DECIMAL(10,6) NULL,
        Lng          DECIMAL(10,6) NULL,
        EsPrincipal  BIT           NOT NULL DEFAULT 1,
        CreatedAtUtc DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc DATETIME2     NULL,
        CONSTRAINT FK_DireccionesCliente_Clientes FOREIGN KEY (ClienteId)
            REFERENCES dbo.Clientes(Id) ON DELETE CASCADE
    );
END;

-- 3. Datos de prueba
SET IDENTITY_INSERT dbo.Clientes ON;
INSERT INTO dbo.Clientes
    (Id, Nombre, Apellido, Email, PasswordHash, TipoIdentificacion,
     Identificacion, Telefono, EmailVerificado, IsActive, NotiEmail,
     CreatedAtUtc)
VALUES
-- Contraseña: Test1234! (hash de ejemplo, usar la app para crear cuentas reales)
(1, 'Ana',    'Mora',    'ana.mora@cliente.cr',    'placeholder_hash', 1, '101010101', '8888-1111', 1, 1, 1, GETUTCDATE()),
(2, 'Carlos', 'Vega',    'carlos.vega@cliente.cr', 'placeholder_hash', 1, '202020202', '8888-2222', 1, 1, 1, GETUTCDATE()),
(3, 'Lucía',  'Fallas',  'lucia.f@cliente.cr',     'placeholder_hash', 2, '11223344556', '8888-3333', 0, 1, 1, GETUTCDATE());
SET IDENTITY_INSERT dbo.Clientes OFF;

SET IDENTITY_INSERT dbo.DireccionesCliente ON;
INSERT INTO dbo.DireccionesCliente
    (Id, ClienteId, Provincia, Canton, Distrito, Direccion, EsPrincipal)
VALUES
(1, 1, 'San José',  'Escazú',  'San Rafael', '200m norte del POPS Escazú',  1),
(2, 2, 'Heredia',   'Heredia', 'Mercedes',   'Casa 7B, Urbanización Las Flores', 1),
(3, 3, 'San José',  'Curridabat','Granadilla','Del Mall Oxígeno 300m este', 1);
SET IDENTITY_INSERT dbo.DireccionesCliente OFF;

-- Verificar
SELECT 'Clientes'          AS Tabla, COUNT(*) AS Registros FROM dbo.Clientes
UNION ALL
SELECT 'DireccionesCliente', COUNT(*) FROM dbo.DireccionesCliente;