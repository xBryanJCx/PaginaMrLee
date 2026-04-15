
-- ============================================================
--  MR LEE SYSTEM – Migración manual RRHH
--  Ejecutar en SQL Server contra la base de datos de MrLee
--  ORDEN: 1) Puestos 2) Sucursales 3) Empleados 4) Vacaciones 5) Seed
-- ============================================================

USE MrLeeDb; 
-- 1. Catálogo Puestos
IF OBJECT_ID('dbo.Puestos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Puestos (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Nombre      NVARCHAR(100)  NOT NULL,
        Descripcion NVARCHAR(300)  NOT NULL DEFAULT '',
        IsActive    BIT            NOT NULL DEFAULT 1
    );
END;

-- 2. Catálogo Sucursales
IF OBJECT_ID('dbo.Sucursales', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sucursales (
        Id        INT IDENTITY(1,1) PRIMARY KEY,
        Nombre    NVARCHAR(100) NOT NULL,
        Direccion NVARCHAR(200) NOT NULL DEFAULT '',
        IsActive  BIT           NOT NULL DEFAULT 1
    );
END;

-- 3. Empleados
IF OBJECT_ID('dbo.Empleados', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Empleados (
        Id                   INT IDENTITY(1,1) PRIMARY KEY,
        Codigo               NVARCHAR(20)   NOT NULL,
        Nombre               NVARCHAR(100)  NOT NULL,
        Apellido             NVARCHAR(100)  NOT NULL,
        Identificacion       NVARCHAR(30)   NOT NULL,
        Email                NVARCHAR(200)  NOT NULL,
        Telefono             NVARCHAR(20)   NOT NULL DEFAULT '',
        FechaIngreso         DATE           NOT NULL,
        PuestoId             INT            NOT NULL,
        SalarioBase          DECIMAL(18,2)  NOT NULL,
        TipoContrato         INT            NOT NULL DEFAULT 1,  -- 1=INDEFINIDO 2=FIJO 3=SERVICIOS
        Jornada              INT            NOT NULL DEFAULT 1,  -- 1=COMPLETA 2=PARCIAL
        SucursalId           INT            NOT NULL,
        Estado               INT            NOT NULL DEFAULT 1,  -- 1=ACTIVO 2=INACTIVO
        Observaciones        NVARCHAR(500)  NOT NULL DEFAULT '',
        DiasVacacionDisponibles INT         NOT NULL DEFAULT 0,
        FechaSalida          DATE           NULL,
        MotivoCambioEstado   NVARCHAR(300)  NOT NULL DEFAULT '',
        CreatedAtUtc         DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc         DATETIME2      NULL,
        CreatedByUserId      INT            NULL,
        CreatedByEmail       NVARCHAR(200)  NOT NULL DEFAULT '',
        UpdatedByUserId      INT            NULL,
        UpdatedByEmail       NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_Empleados_Puestos    FOREIGN KEY (PuestoId)    REFERENCES dbo.Puestos(Id),
        CONSTRAINT FK_Empleados_Sucursales FOREIGN KEY (SucursalId)  REFERENCES dbo.Sucursales(Id)
    );

    CREATE UNIQUE INDEX UX_Empleados_Identificacion ON dbo.Empleados (Identificacion);
    CREATE UNIQUE INDEX UX_Empleados_Codigo         ON dbo.Empleados (Codigo);
END;

-- 4. Solicitudes de Vacación
IF OBJECT_ID('dbo.SolicitudesVacacion', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SolicitudesVacacion (
        Id               INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId       INT            NOT NULL,
        FechaInicio      DATE           NOT NULL,
        FechaFin         DATE           NOT NULL,
        DiasSolicitados  INT            NOT NULL,
        TipoDia          INT            NOT NULL DEFAULT 1,  -- 1=DIA 2=MEDIO_DIA
        Observaciones    NVARCHAR(500)  NOT NULL DEFAULT '',
        Estado           INT            NOT NULL DEFAULT 1,  -- 1=SOLICITADA 2=APROBADA 3=RECHAZADA 4=CANCELADA
        MotivoRechazo    NVARCHAR(300)  NOT NULL DEFAULT '',
        RevisadoPorUserId INT           NULL,
        RevisadoPorEmail  NVARCHAR(200) NOT NULL DEFAULT '',
        RevisadoAtUtc    DATETIME2      NULL,
        CreatedAtUtc     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        CreatedByUserId  INT            NULL,
        CreatedByEmail   NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_SolicitudesVacacion_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(Id) ON DELETE CASCADE
    );
END;

-- 5. Permisos RRHH en tabla Permissions
INSERT INTO dbo.Permissions (Code, Description, IsActive)
SELECT Code, Description, 1
FROM (VALUES
    ('RRHH.VIEW',       'Ver módulo de empleados'),
    ('RRHH.MANAGE',     'Crear y editar empleados, aprobar vacaciones'),
    ('RRHH.VACACIONES', 'Solicitar y cancelar vacaciones propias')
) AS src(Code, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.Code = src.Code);

-- Asignar todos los permisos RRHH al rol Administrador
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM   dbo.Roles r
CROSS  JOIN dbo.Permissions p
WHERE  r.Name = 'Administrador'
  AND  p.Code IN ('RRHH.VIEW', 'RRHH.MANAGE', 'RRHH.VACACIONES')
  AND  NOT EXISTS (
       SELECT 1 FROM dbo.RolePermissions rp
       WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id);

-- 6. Datos de prueba – Puestos
SET IDENTITY_INSERT dbo.Puestos ON;
INSERT INTO dbo.Puestos (Id, Nombre, Descripcion, IsActive) VALUES
(1, 'Panadero',         'Elaboración de productos de panadería',          1),
(2, 'Pastelero',        'Elaboración de postres y pasteles',              1),
(3, 'Cajero',           'Atención al cliente y manejo de caja',           1),
(4, 'Supervisor',       'Supervisión de personal y operación diaria',     1),
(5, 'Repartidor',       'Distribución y entrega de pedidos',              1),
(6, 'Auxiliar Bodega',  'Control de inventario y almacén',                1);
SET IDENTITY_INSERT dbo.Puestos OFF;

-- 7. Datos de prueba – Sucursales
SET IDENTITY_INSERT dbo.Sucursales ON;
INSERT INTO dbo.Sucursales (Id, Nombre, Direccion, IsActive) VALUES
(1, 'Central',          'Av. Principal 100, San José',   1),
(2, 'Escazú',           'Centro Comercial Plaza, Escazú',1),
(3, 'Heredia',          'Calle 5, Heredia Centro',        1);
SET IDENTITY_INSERT dbo.Sucursales OFF;

-- 8. Datos de prueba – Empleados
SET IDENTITY_INSERT dbo.Empleados ON;
INSERT INTO dbo.Empleados
    (Id, Codigo, Nombre, Apellido, Identificacion, Email, Telefono,
     FechaIngreso, PuestoId, SalarioBase, TipoContrato, Jornada,
     SucursalId, Estado, Observaciones, DiasVacacionDisponibles,
     CreatedByEmail)
VALUES
(1, 'EMP-001', 'Carlos',   'Ramírez',   '1-0101-0101', 'cramires@mrlee.local',  '8888-1111', '2023-01-15', 1, 520000.00, 1, 1, 1, 1, '',  10, 'admin@mrlee.local'),
(2, 'EMP-002', 'Ana',      'González',  '1-0202-0202', 'agonzalez@mrlee.local', '8888-2222', '2023-03-01', 2, 540000.00, 1, 1, 1, 1, '',  10, 'admin@mrlee.local'),
(3, 'EMP-003', 'Luis',     'Mora',      '1-0303-0303', 'lmora@mrlee.local',     '8888-3333', '2023-06-10', 3, 400000.00, 1, 1, 2, 1, '',   8, 'admin@mrlee.local'),
(4, 'EMP-004', 'María',    'Castro',    '1-0404-0404', 'mcastro@mrlee.local',   '8888-4444', '2022-09-01', 4, 700000.00, 1, 1, 1, 1, '',  15, 'admin@mrlee.local'),
(5, 'EMP-005', 'José',     'Vargas',    '1-0505-0505', 'jvargas@mrlee.local',   '8888-5555', '2024-01-08', 5, 380000.00, 2, 1, 3, 1, '',   5, 'admin@mrlee.local'),
(6, 'EMP-006', 'Sofía',    'Jiménez',   '1-0606-0606', 'sjimenez@mrlee.local',  '8888-6666', '2024-04-15', 3, 400000.00, 1, 2, 2, 1, 'Jornada parcial mañanas', 4, 'admin@mrlee.local'),
(7, 'EMP-007', 'Diego',    'Solano',    '1-0707-0707', 'dsolano@mrlee.local',   '8888-7777', '2021-11-20', 1, 510000.00, 1, 1, 1, 2, '',  20, 'admin@mrlee.local'),
(8, 'EMP-008', 'Valentina','López',     '1-0808-0808', 'vlopez@mrlee.local',    '8888-8888', '2022-05-03', 2, 530000.00, 1, 1, 3, 1, '',  12, 'admin@mrlee.local'),
(9, 'EMP-009', 'Andrés',   'Quesada',   '1-0909-0909', 'aqa@mrlee.local',       '8888-9999', '2023-08-22', 6, 390000.00, 3, 1, 1, 1, 'Servicios profesionales', 0, 'admin@mrlee.local'),
(10,'EMP-010', 'Laura',    'Brenes',    '1-1010-1010', 'lbrenes@mrlee.local',   '8888-0000', '2020-02-14', 4, 720000.00, 1, 1, 2, 2, '',  25, 'admin@mrlee.local');
SET IDENTITY_INSERT dbo.Empleados OFF;

-- EMP-010 baja lógica (INACTIVO) para pruebas de filtro
UPDATE dbo.Empleados
SET    Estado = 2, FechaSalida = '2025-12-31', MotivoCambioEstado = 'Renuncia voluntaria'
WHERE  Codigo = 'EMP-010';

-- 9. Solicitudes de vacación de prueba
SET IDENTITY_INSERT dbo.SolicitudesVacacion ON;
INSERT INTO dbo.SolicitudesVacacion
    (Id, EmpleadoId, FechaInicio, FechaFin, DiasSolicitados, TipoDia, Observaciones, Estado, CreatedByEmail)
VALUES
(1, 1, '2026-04-07', '2026-04-11', 5, 1, 'Vacaciones Semana Santa',        1, 'cramires@mrlee.local'),
(2, 2, '2026-05-01', '2026-05-05', 5, 1, 'Vacaciones programadas mayo',     2, 'agonzalez@mrlee.local'),
(3, 3, '2026-03-10', '2026-03-10', 1, 2, 'Medio día diligencias personales',3, 'lmora@mrlee.local'),
(4, 4, '2026-06-01', '2026-06-15',10, 1, 'Vacaciones anuales',              1, 'mcastro@mrlee.local');
SET IDENTITY_INSERT dbo.SolicitudesVacacion OFF;

-- Reflejar días descontados del empleado 2 (solicitud APROBADA de 5 días)
UPDATE dbo.Empleados SET DiasVacacionDisponibles = DiasVacacionDisponibles - 5 WHERE Id = 2;

-- ============================================================
-- Verificar
-- ============================================================
SELECT 'Puestos'             AS Tabla, COUNT(*) AS Registros FROM dbo.Puestos
UNION ALL
SELECT 'Sucursales',          COUNT(*) FROM dbo.Sucursales
UNION ALL
SELECT 'Empleados',           COUNT(*) FROM dbo.Empleados
UNION ALL
SELECT 'SolicitudesVacacion', COUNT(*) FROM dbo.SolicitudesVacacion;


-- ============================================================
--  MR LEE SYSTEM – Migración RRHH Parte 2
--  Ejecutar en SSMS contra la BD de MrLee
--  Requiere que la migración anterior (migracion_rrhh.sql) ya esté aplicada
-- ============================================================

-- 1. Incapacidades
IF OBJECT_ID('dbo.Incapacidades','U') IS NULL
BEGIN
    CREATE TABLE dbo.Incapacidades (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId      INT            NOT NULL,
        FechaInicio     DATE           NOT NULL,
        FechaFin        DATE           NOT NULL,
        Diagnostico     NVARCHAR(500)  NOT NULL DEFAULT '',
        TipoDocumento   INT            NOT NULL DEFAULT 1,  -- 1=ORDEN 2=CERTIFICADO
        DocumentoUrl    NVARCHAR(500)  NOT NULL DEFAULT '',
        CentroMedico    NVARCHAR(200)  NOT NULL DEFAULT '',
        NumeroOrden     NVARCHAR(50)   NOT NULL DEFAULT '',
        Estado          INT            NOT NULL DEFAULT 1,  -- 1=REGISTRADA 2=VIGENTE 3=FINALIZADA
        Observaciones   NVARCHAR(500)  NOT NULL DEFAULT '',
        CreatedAtUtc    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc    DATETIME2      NULL,
        CreatedByUserId INT            NULL,
        CreatedByEmail  NVARCHAR(200)  NOT NULL DEFAULT '',
        UpdatedByUserId INT            NULL,
        UpdatedByEmail  NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_Incapacidades_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(Id) ON DELETE CASCADE
    );
END;

-- 2. Documentos del expediente
IF OBJECT_ID('dbo.DocumentosExpediente','U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentosExpediente (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId      INT            NOT NULL,
        DocumentoTipo   INT            NOT NULL,  -- 1=CONTRATO 2=IDENTIFICACION 3=CONSTANCIA 4=INCAPACIDAD
        DocumentoUrl    NVARCHAR(500)  NOT NULL DEFAULT '',
        NombreArchivo   NVARCHAR(200)  NOT NULL DEFAULT '',
        Version         INT            NOT NULL DEFAULT 1,
        IsDeleted       BIT            NOT NULL DEFAULT 0,
        MotivoEliminacion NVARCHAR(300) NOT NULL DEFAULT '',
        CreatedAtUtc    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        DeletedAtUtc    DATETIME2      NULL,
        CreatedByUserId INT            NULL,
        CreatedByEmail  NVARCHAR(200)  NOT NULL DEFAULT '',
        DeletedByUserId INT            NULL,
        DeletedByEmail  NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_DocumentosExpediente_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(Id) ON DELETE CASCADE
    );
END;

-- 3. Direcciones
IF OBJECT_ID('dbo.DireccionesEmpleado','U') IS NULL
BEGIN
    CREATE TABLE dbo.DireccionesEmpleado (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId      INT            NOT NULL,
        Tipo            INT            NOT NULL DEFAULT 1,  -- 1=DOMICILIO 2=CORRESPONDENCIA
        Provincia       NVARCHAR(100)  NOT NULL DEFAULT '',
        Canton          NVARCHAR(100)  NOT NULL DEFAULT '',
        Distrito        NVARCHAR(100)  NOT NULL DEFAULT '',
        Direccion       NVARCHAR(300)  NOT NULL DEFAULT '',
        CodigoPostal    NVARCHAR(10)   NOT NULL DEFAULT '',
        Lat             DECIMAL(10,6)  NULL,
        Lon             DECIMAL(10,6)  NULL,
        EsPrincipal     BIT            NOT NULL DEFAULT 0,
        IsDeleted       BIT            NOT NULL DEFAULT 0,
        MotivoEliminacion NVARCHAR(300) NOT NULL DEFAULT '',
        CreatedAtUtc    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc    DATETIME2      NULL,
        DeletedAtUtc    DATETIME2      NULL,
        CreatedByUserId INT            NULL,
        CreatedByEmail  NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_DireccionesEmpleado_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(Id) ON DELETE CASCADE
    );
END;

-- 4. Contactos de emergencia
IF OBJECT_ID('dbo.ContactosEmergencia','U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactosEmergencia (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId      INT            NOT NULL,
        Nombre          NVARCHAR(150)  NOT NULL DEFAULT '',
        Parentesco      INT            NOT NULL DEFAULT 6,  -- 1=CONYUGUE..6=OTRO
        Telefono        NVARCHAR(20)   NOT NULL DEFAULT '',
        TelefonoAlt     NVARCHAR(20)   NOT NULL DEFAULT '',
        Email           NVARCHAR(200)  NOT NULL DEFAULT '',
        EsPrincipal     BIT            NOT NULL DEFAULT 0,
        IsDeleted       BIT            NOT NULL DEFAULT 0,
        MotivoEliminacion NVARCHAR(300) NOT NULL DEFAULT '',
        CreatedAtUtc    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc    DATETIME2      NULL,
        DeletedAtUtc    DATETIME2      NULL,
        CreatedByUserId INT            NULL,
        CreatedByEmail  NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_ContactosEmergencia_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(Id) ON DELETE CASCADE
    );
END;

-- 5. Cuentas bancarias
IF OBJECT_ID('dbo.CuentasBancarias','U') IS NULL
BEGIN
    CREATE TABLE dbo.CuentasBancarias (
        Id                INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId        INT            NOT NULL,
        Banco             NVARCHAR(150)  NOT NULL DEFAULT '',
        TipoCuenta        INT            NOT NULL DEFAULT 1,  -- 1=AHORROS 2=CORRIENTE
        Moneda            INT            NOT NULL DEFAULT 1,  -- 1=CRC 2=USD
        NumeroCuenta      NVARCHAR(50)   NOT NULL DEFAULT '',
        Iban              NVARCHAR(30)   NOT NULL DEFAULT '',
        EsPrincipal       BIT            NOT NULL DEFAULT 0,
        Estado            INT            NOT NULL DEFAULT 1,  -- 1=ACTIVA 2=INACTIVA
        MotivoInactivacion NVARCHAR(300) NOT NULL DEFAULT '',
        CreatedAtUtc      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc      DATETIME2      NULL,
        CreatedByUserId   INT            NULL,
        CreatedByEmail    NVARCHAR(200)  NOT NULL DEFAULT '',
        UpdatedByUserId   INT            NULL,
        UpdatedByEmail    NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_CuentasBancarias_Empleados FOREIGN KEY (EmpleadoId) REFERENCES dbo.Empleados(Id) ON DELETE CASCADE
    );
END;

-- 6. Movimientos laborales
IF OBJECT_ID('dbo.MovimientosLaborales','U') IS NULL
BEGIN
    CREATE TABLE dbo.MovimientosLaborales (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        EmpleadoId          INT            NOT NULL,
        PuestoIdNuevo       INT            NOT NULL,
        SucursalIdNueva     INT            NOT NULL,
        PuestoIdAnterior    INT            NULL,
        SucursalIdAnterior  INT            NULL,
        VigenciaDesde       DATE           NOT NULL,
        VigenciaHasta       DATE           NULL,
        Estado              INT            NOT NULL DEFAULT 1,  -- 1=VIGENTE 2=CERRADO 3=ANULADO
        Motivo              NVARCHAR(400)  NOT NULL DEFAULT '',
        MotivoAnulacion     NVARCHAR(400)  NOT NULL DEFAULT '',
        CreatedAtUtc        DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAtUtc        DATETIME2      NULL,
        CreatedByUserId     INT            NULL,
        CreatedByEmail      NVARCHAR(200)  NOT NULL DEFAULT '',
        UpdatedByUserId     INT            NULL,
        UpdatedByEmail      NVARCHAR(200)  NOT NULL DEFAULT '',
        CONSTRAINT FK_MovimientosLaborales_Empleados  FOREIGN KEY (EmpleadoId)      REFERENCES dbo.Empleados(Id),
        CONSTRAINT FK_MovimientosLaborales_Puestos    FOREIGN KEY (PuestoIdNuevo)   REFERENCES dbo.Puestos(Id),
        CONSTRAINT FK_MovimientosLaborales_Sucursales FOREIGN KEY (SucursalIdNueva) REFERENCES dbo.Sucursales(Id)
    );
END;

-- ── Datos de prueba ───────────────────────────────────────────

-- Incapacidades de prueba
SET IDENTITY_INSERT dbo.Incapacidades ON;
INSERT INTO dbo.Incapacidades
    (Id, EmpleadoId, FechaInicio, FechaFin, Diagnostico, TipoDocumento, CentroMedico, NumeroOrden, Estado, CreatedByEmail)
VALUES
(1, 1, '2026-01-10', '2026-01-14', 'Gripe con fiebre alta',          1, 'CCSS Central San José',   'ORD-2026-001', 2, 'admin@mrlee.local'),
(2, 3, '2026-02-03', '2026-02-07', 'Esguince tobillo derecho',       1, 'Clínica Bíblica Heredia',  'ORD-2026-002', 3, 'admin@mrlee.local'),
(3, 5, '2026-03-15', '2026-03-19', 'Infección respiratoria aguda',   2, 'CCSS Escazú',              'CERT-2026-001',1, 'admin@mrlee.local');
SET IDENTITY_INSERT dbo.Incapacidades OFF;

-- Direcciones de prueba
SET IDENTITY_INSERT dbo.DireccionesEmpleado ON;
INSERT INTO dbo.DireccionesEmpleado
    (Id, EmpleadoId, Tipo, Provincia, Canton, Distrito, Direccion, CodigoPostal, EsPrincipal, CreatedByEmail)
VALUES
(1, 1, 1, 'San José',  'San José',  'Carmen',   '200m norte del parque central',  '10101', 1, 'admin@mrlee.local'),
(2, 1, 2, 'San José',  'Escazú',    'San Rafael','Ofibodega 5, frente al POPS',    '10203', 0, 'admin@mrlee.local'),
(3, 2, 1, 'Heredia',   'Heredia',   'Mercedes',  'Casa 14B, condominio Las Palmas', '40101', 1, 'admin@mrlee.local'),
(4, 4, 1, 'San José',  'Desamparados','San Miguel','100m sur de la iglesia',        '10301', 1, 'admin@mrlee.local');
SET IDENTITY_INSERT dbo.DireccionesEmpleado OFF;

-- Contactos de emergencia de prueba
SET IDENTITY_INSERT dbo.ContactosEmergencia ON;
INSERT INTO dbo.ContactosEmergencia
    (Id, EmpleadoId, Nombre, Parentesco, Telefono, TelefonoAlt, Email, EsPrincipal, CreatedByEmail)
VALUES
(1, 1, 'Laura Ramírez',   4, '8765-4321', '2222-3333', 'lramirez@email.com', 1, 'admin@mrlee.local'),
(2, 2, 'Pedro González',  3, '8111-2222', '',          '',                   1, 'admin@mrlee.local'),
(3, 4, 'Sofía Castro',    1, '8444-5555', '2555-6666', 'soficastle@mail.cr', 1, 'admin@mrlee.local'),
(4, 1, 'Marco Ramírez',   5, '8999-0000', '',          '',                   0, 'admin@mrlee.local');
SET IDENTITY_INSERT dbo.ContactosEmergencia OFF;

-- Cuentas bancarias de prueba
SET IDENTITY_INSERT dbo.CuentasBancarias ON;
INSERT INTO dbo.CuentasBancarias
    (Id, EmpleadoId, Banco, TipoCuenta, Moneda, NumeroCuenta, Iban, EsPrincipal, Estado, CreatedByEmail)
VALUES
(1, 1, 'Banco Nacional de CR',  1, 1, '100-01-001-000001-0', 'CR21015101100101001000010', 1, 1, 'admin@mrlee.local'),
(2, 2, 'BAC San José',          1, 1, '912345678',            'CR05010200009123456780',    1, 1, 'admin@mrlee.local'),
(3, 4, 'Banco Popular',         2, 1, '204-12345-6',          'CR65015201040001234560',    1, 1, 'admin@mrlee.local'),
(4, 1, 'Scotiabank CR',         1, 2, '00-1-000-01234-5',     'CR18013000001000001234',    0, 2, 'admin@mrlee.local');
SET IDENTITY_INSERT dbo.CuentasBancarias OFF;

-- Movimientos laborales de prueba
SET IDENTITY_INSERT dbo.MovimientosLaborales ON;
INSERT INTO dbo.MovimientosLaborales
    (Id, EmpleadoId, PuestoIdNuevo, SucursalIdNueva, PuestoIdAnterior, SucursalIdAnterior,
     VigenciaDesde, VigenciaHasta, Estado, Motivo, CreatedByEmail)
VALUES
(1, 7, 4, 1, 1, 1, '2024-01-15', '2025-12-31', 2, 'Ascenso a supervisor por desempeño destacado',     'admin@mrlee.local'),
(2, 7, 1, 1, 4, 1, '2026-01-01', NULL,          1, 'Retorno a panadería por reestructuración de área', 'admin@mrlee.local'),
(3, 3, 3, 2, 3, 1, '2025-06-01', '2025-12-31',  2, 'Traslado temporal sucursal Escazú',               'admin@mrlee.local'),
(4, 3, 3, 2, 3, 2, '2026-01-01', NULL,           1, 'Confirmación definitiva sucursal Escazú',          'admin@mrlee.local');
SET IDENTITY_INSERT dbo.MovimientosLaborales OFF;

-- ── Verificación final ────────────────────────────────────────
SELECT 'Incapacidades'         AS Tabla, COUNT(*) AS Registros FROM dbo.Incapacidades
UNION ALL SELECT 'DocumentosExpediente', COUNT(*) FROM dbo.DocumentosExpediente
UNION ALL SELECT 'DireccionesEmpleado',  COUNT(*) FROM dbo.DireccionesEmpleado
UNION ALL SELECT 'ContactosEmergencia',  COUNT(*) FROM dbo.ContactosEmergencia
UNION ALL SELECT 'CuentasBancarias',     COUNT(*) FROM dbo.CuentasBancarias
UNION ALL SELECT 'MovimientosLaborales', COUNT(*) FROM dbo.MovimientosLaborales;