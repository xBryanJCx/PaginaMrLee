# Sistema Mr Lee

Este ZIP incluye un proyecto **ASP.NET Core MVC (net8.0)** con **arquitectura MVC**, **EF Core (SQL Server)**, autenticación por **cookies**, control de acceso por **roles/permisos**, y los 4 módulos solicitados:

- **Seguimiento de pedidos**: crear pedido, número único, consultar, actualizar estado y ver historial/timeline.

- **Inventario**: catálogo de productos, existencias, entradas/salidas/ajustes (movimientos) y desactivar productos.

- **Usuarios y accesos**: CRUD de usuarios, roles/permisos, activar/desactivar, reset de contraseña y bitácora.

- **Ingresos operativos**: registrar ingresos por ventas/cobros/otros, editar y anular, filtrar por fechas, ver resumen diario, exportar CSV, cerrar períodos contables y adjuntar comprobantes.

> Los requerimientos se tomaron del documento `Requerimientos.pdf` 

---

## Requisitos

- Visual Studio 2022 (o VS Code) con .NET 8
- SQL Server (LocalDB o Express)

---

## 1) Base de datos

### Ejecutar script SQL
1. Abra `Database/01_MrLeeDb.sql` en SQL Server Management Studio.
2. Ejecútelo (crea `MrLeeDb` y tablas).

### Con migraciones EF Core
El proyecto incluye `db.Database.Migrate()` en el arranque.
1. Configure el connection string en `src/MrLee.Web/appsettings.json`
2. Ejecute el proyecto y se crearán tablas automáticamente.

---

## 2) Credenciales iniciales (admin)

Al levantar el sistema por primera vez, se hace **seed** de:
- Roles básicos (Administrador, Ventas, Bodega, Despacho)
- Permisos del sistema
- Usuario admin (si la tabla Users está vacía)

Se leen de `appsettings.json`:

```json
"Seed": {
  "AdminEmail": "admin@mrlee.local",
  "AdminPassword": "Admin123!"
}
```

---

## 3) Módulos incluidos (rutas)

- Pedidos: `/Orders`
- Inventario: `/Inventory`
- Ingresos operativos: `/OperatingIncome`
- Resumen de ingresos: `/OperatingIncome/Summary`
- Cierres contables: `/OperatingIncome/Periods`
- Usuarios: `/Users`
- Bitácora: `/Users/Audit`
- Login: `/Account/Login`

---

## 4) Notas técnicas

- **Passwords**: PBKDF2 (SHA256, 100k iteraciones) almacenado en `Users.PasswordHash`.
- **Bloqueo por intentos fallidos**: al 5to intento, bloqueo 15 minutos (requerimiento SEGR-006).
- **Bitácora**: tabla `ActionLogs`.
- **Comprobantes de ingresos**: se guardan en `wwwroot/uploads/operating-income/`.

---

## Estructura del repositorio

- `MrLeeSystem.sln`
- `src/MrLee.Web/` (proyecto web)
- `Database/01_MrLeeDb.sql` (script SQL Server)
- `wwwroot/img/logo.jpeg` (logo)



## Recuperación de contraseña por correo

Se agregó la opción **¿Olvidó su contraseña?** en el login.

Flujo:
1. El usuario escribe su correo.
2. El sistema genera una contraseña temporal.
3. La contraseña temporal se envía por correo usando SMTP de Brevo.
4. Al iniciar sesión, el usuario debe cambiarla obligatoriamente.

### Configuración SMTP Brevo

Complete la contraseña SMTP en `src/MrLee.Web/appsettings.json` o use variables de entorno:

```json
"Smtp": {
  "Host": "smtp-relay.brevo.com",
  "Port": 587,
  "Username": "",
  "Password": "COLOQUE_AQUI_SU_CLAVE_BREVO",
  "FromEmail": "",
  "FromName": "Mr Lee System",
  "EnableSsl": true
}
```

Variables de entorno equivalentes:
- `Smtp__Host`
- `Smtp__Port`
- `Smtp__Username`
- `Smtp__Password`
- `Smtp__FromEmail`
- `Smtp__FromName`
- `Smtp__EnableSsl`
