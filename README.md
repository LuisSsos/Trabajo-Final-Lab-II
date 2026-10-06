# Gimnasio - Trabajo Final Laboratorio II

Sitio web para administrar un gimnasio, hecho con ASP.NET Core MVC. Permite gestionar usuarios, clientes, empleados, planes, membresías, pagos, clases, horarios y turnos.

## Tecnologías

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core con MariaDB/MySQL (paquete Microting.EntityFrameworkCore.MySql)
- ASP.NET Core Identity para login, usuarios y roles

## Modelo de datos

Entidades propias:

- **Cliente**: datos propios del cliente (fecha de nacimiento, apto médico). Se relaciona 1 a 1 con un usuario.
- **Empleado**: datos propios del empleado (cargo). Se relaciona 1 a 1 con un usuario.
- **Plan**: planes de suscripción (nombre, precio, duración en días).
- **Membresía**: un cliente tiene un plan durante un período, con su estado.
- **Pago**: cada membresía genera pagos (monto, fecha, método, comprobante).
- **Clase**: clase que dicta un empleado, con cupo máximo.
- **Horario**: días y horas en que se dicta cada clase.
- **Turno**: reserva de un cliente a un horario en una fecha.

Las tablas que empiezan con `AspNet` las crea Identity. `AspNetUsers` es nuestra clase Usuario (con nombre, apellido y avatar), `AspNetRoles` guarda los roles y `AspNetUserRoles` guarda qué rol tiene cada usuario. Las otras cuatro quedan vacías.

Un cliente no puede reservar dos veces el mismo horario en la misma fecha: hay un índice único en Turnos (`HorarioId`, `ClienteId`, `Fecha`).

### Diagrama entidad-relación

## Estructura del proyecto

- **Controllers/**: CuentaController (login y logout), UsuarioController (ABMC de usuarios) y HomeController.
- **Data/ContextoDatos.cs**: conecta las clases con la base de datos y define reglas de las tablas.
- **Data/SemillaDatos.cs**: al arrancar la app crea los roles y el usuario administrador si no existen.
- **Models/**: las clases de las entidades, los enums (EstadoMembresia, EstadoTurno, MetodoPago) y los roles (Roles.cs).
- **Models/ViewModels/**: modelos usados en las vistas (por ahora UsuarioViewModel).
- **Views/**: vistas simples.
- **BaseDatos/gimnasio.sql**: exportación de la base de datos.
- **Program.cs**: configuración de la app (base de datos, Identity, cookies de login).

## Cómo correr el proyecto

### Requisitos

- .NET 10 SDK
- MariaDB o MySQL
- Git

El proyecto puede ejecutarse tanto en **Windows como en Linux**.

### 1. Clonar el repositorio

```bash
git clone <URL_DEL_REPOSITORIO>
cd Trabajo-Final-Lab-II
```

### 2. Crear la base de datos

Primero hay que tener el servidor de MariaDB/MySQL instalado y ejecutándose.

Luego, crear la base de datos y el usuario.

**Linux:**

```bash
sudo mariadb
```

**Windows:**

Abrir la consola de MariaDB/MySQL y ejecutar el siguiente SQL:

```sql
CREATE DATABASE gimnasio CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER 'gimnasio'@'localhost' IDENTIFIED BY 'gimnasio123';
GRANT ALL PRIVILEGES ON gimnasio.* TO 'gimnasio'@'localhost';
FLUSH PRIVILEGES;
```

En caso de utilizar un usuario de MariaDB/MySQL diferente, adaptar el connection string de `appsettings.json`.

### 3. Cargar las tablas

El proyecto incluye una exportación de la base de datos en:

```text
BaseDatos/gimnasio.sql
```

**Linux:**

```bash
sudo mariadb gimnasio < BaseDatos/gimnasio.sql
```

**Windows:**

Se puede importar el archivo `BaseDatos/gimnasio.sql` desde MySQL Workbench, phpMyAdmin o la consola de MariaDB/MySQL.

### 4. Revisar la conexión

Verificar que el connection string de `appsettings.json` coincida con el usuario, contraseña, servidor y base de datos configurados.

### 5. Ejecutar la aplicación

Desde la raíz del repositorio:

```bash
dotnet run
```

La aplicación mostrará en la terminal la dirección local donde está disponible.

### 6. Iniciar sesión

Abrir la dirección indicada por la terminal y entrar a:

```text
/Cuenta/Login
```

## Usuario de prueba

Al arrancar, el seeder crea los roles Administrador, Empleado y Cliente, y este usuario:

- Rol: Administrador
- Email: admin@gimnasio.com
- Contraseña: Admin123

## Seguridad

- El login usa Identity con cookies. Si alguien intenta acceder sin iniciar sesión, se lo redirige a `/Cuenta/Login`.
- `UsuarioController` solo puede ser utilizado por el rol Administrador. Quien esté logueado sin ese rol verá la pantalla de acceso denegado.
- Las contraseñas se guardan hasheadas con Identity.

## Requisitos mínimos

### Hecho

- Al menos 4 tablas relacionadas, con una relación 1 a muchos. Está en `Models/` y `Data/ContextoDatos.cs`.
- Login con `Authorize` y roles. Está en `Controllers/CuentaController.cs`, `Program.cs` y `Data/SemillaDatos.cs`.
- Funcionalidad restringida por rol. Está en `Controllers/UsuarioController.cs`.

### Pendiente

- Avatar en los usuarios. El campo `Avatar` ya existe en `Models/Usuario.cs`, falta la subida.
- Uso de archivos además del avatar. Los campos `AptoMedico` (Cliente) y `Comprobante` (Pago) ya están creados, falta la subida.
- ABM con Vue.js vía AJAX.
- Listados con paginado del lado del servidor.
- Selección de entidades relacionadas con búsqueda AJAX.
- API con JWT y colección de Postman.

## Qué falta hacer

- Que al crear un usuario se le asigne un rol y se cree su registro de cliente o empleado.
- ABMC del resto de las entidades.
- Avatar y subida de archivos.
- Paginado, búsqueda AJAX, ABM con Vue.js y API con JWT.