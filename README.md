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
- **Membresía**: un cliente tiene un plan durante un período, con su estado. La fecha de fin se calcula sola sumando la duración del plan a la fecha de inicio.
- **Pago**: cada membresía genera pagos (monto, fecha, método, comprobante).
- **Clase**: clase que dicta un empleado, con cupo máximo.
- **Horario**: días y horas en que se dicta cada clase.
- **Turno**: reserva de un cliente a un horario en una fecha.

Las tablas que empiezan con `AspNet` las crea Identity. `AspNetUsers` es nuestra clase Usuario (con nombre, apellido y avatar), `AspNetRoles` guarda los roles y `AspNetUserRoles` guarda qué rol tiene cada usuario.

Un cliente no puede reservar dos veces el mismo horario en la misma fecha: hay un índice único en Turnos (`HorarioId`, `ClienteId`, `Fecha`).

Al eliminar un usuario empleado se elimina también su registro de Empleado y, en cascada, sus clases y los horarios de esas clases.

### Diagrama entidad-relación

## Estructura del proyecto

- **Controllers/**: CuentaController (login y logout), UsuarioController (ABMC de usuarios con paginado, avatar y apto médico), PlanController (ABMC de planes), ClaseController (ABMC de clases con búsqueda AJAX de entrenador), HorarioController (ABMC de horarios), MembresiaController (ABMC de membresías con búsqueda AJAX de cliente) y HomeController.
- **Services/ServicioArchivos.cs**: guarda y borra los archivos subidos (avatares y aptos médicos).
- **wwwroot/uploads/**: carpeta donde se guardan los archivos subidos. Se crea sola y no se sube al repositorio.
- **Data/ContextoDatos.cs**: conecta las clases con la base de datos y define reglas de las tablas.
- **Data/SemillaDatos.cs**: al arrancar la app crea los roles y el usuario administrador si no existen.
- **Models/**: las clases de las entidades, los enums (EstadoMembresia, EstadoTurno, MetodoPago) y los roles (Roles.cs).
- **Models/ViewModels/**: modelos usados en las vistas (UsuarioViewModel, ClaseViewModel, HorarioViewModel y MembresiaViewModel).
- **Views/**: vistas simples.
- **BaseDatos/gimnasio.sql**: exportación de la base de datos.
- **Program.cs**: configuración de la app (base de datos, Identity, cookies de login, archivos estáticos).

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

Al arrancar, el seeder crea los roles Administrador, Recepcionista, Entrenador, Nutricionista, Administrativo y Cliente, y este usuario:

- Rol: Administrador
- Email: admin@gimnasio.com
- Contraseña: Admin123

## Seguridad

- El login usa Identity con cookies. Si alguien intenta acceder sin iniciar sesión, se lo redirige a `/Cuenta/Login`.
- `UsuarioController`, `PlanController`, `ClaseController` y `HorarioController` solo pueden ser utilizados por el rol Administrador.
- `MembresiaController` puede ser utilizado por los roles Administrador y Recepcionista.
- Quien esté logueado sin el rol necesario verá la pantalla de acceso denegado.
- Las contraseñas se guardan hasheadas con Identity.

## Requisitos mínimos

### Hecho

- Al menos 4 tablas relacionadas, con una relación 1 a muchos. Está en `Models/` y `Data/ContextoDatos.cs`.
- Login con `Authorize` y roles. Está en `Controllers/CuentaController.cs`, `Program.cs` y `Data/SemillaDatos.cs`.
- Funcionalidad restringida por rol. Está en los controladores de usuarios, planes, clases, horarios y membresías.
- Avatar en los usuarios. Se sube al crear o editar un usuario y se muestra en el listado y en el detalle. Está en `Controllers/UsuarioController.cs` y `Services/ServicioArchivos.cs`.
- Uso de archivos además del avatar: apto médico del cliente, que se sube al crear o editar un cliente. Está en `Controllers/UsuarioController.cs`.
- Listados con paginado del lado del servidor (10 por página, con `Skip` y `Take`). Está en todos los listados de los controladores ya mencionados.
- Selección de entidades relacionadas con búsqueda AJAX: al crear o editar una clase se busca el entrenador escribiendo su nombre, y al crear o editar una membresía se busca el cliente. Está en `ClaseController` (`BuscarEntrenadores`), `MembresiaController` (`BuscarClientes`) y sus vistas.
- ABMC de planes, clases, horarios y membresías.

### Pendiente

- Uso de archivos: el campo `Comprobante` (Pago) ya está creado, falta la subida.
- ABM con Vue.js vía AJAX.
- API con JWT y colección de Postman.

## Qué falta hacer

- ABMC de Pagos, con subida del comprobante.
- ABMC de Turnos.
- ABM con Vue.js vía AJAX.
- API con JWT y colección de Postman.