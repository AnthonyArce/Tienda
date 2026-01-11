# 🔐 Sistema de Autenticación y Autorización

> **Sistema completo de autenticación basado en JWT (JSON Web Tokens) con autorización basada en roles, permitiendo control de acceso granular a los endpoints de la API.**

---

## 📑 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Arquitectura del Sistema](#-arquitectura-del-sistema)
- [Configuración](#-configuración)
  - [Configuración JWT](#configuración-jwt)
  - [Configuración en Program.cs](#configuración-en-programcs)
  - [Configuración de Servicios](#configuración-de-servicios)
- [Entidades y Modelos](#-entidades-y-modelos)
  - [Entidad Usuario](#entidad-usuario)
  - [Entidad Rol](#entidad-rol)
  - [Entidad UsuariosRoles](#entidad-usuariosroles)
- [Servicios](#-servicios)
  - [UserServices](#userservices)
  - [IUserServices](#iuserservices)
- [Endpoints de Autenticación](#-endpoints-de-autenticación)
  - [Registro de Usuario](#registro-de-usuario)
  - [Login y Obtención de Token](#login-y-obtención-de-token)
  - [Agregar Rol a Usuario](#agregar-rol-a-usuario)
- [Autorización en Controladores](#-autorización-en-controladores)
- [Roles del Sistema](#-roles-del-sistema)
- [Flujo de Autenticación](#-flujo-de-autenticación)
- [Ejemplos de Uso](#-ejemplos-de-uso)
- [Estructura del Token JWT](#-estructura-del-token-jwt)
- [Seguridad](#-seguridad)
- [Ventajas](#-ventajas-del-sistema)
- [Notas Técnicas](#-notas-técnicas)

---

## 🎯 Descripción General

Se ha implementado un sistema completo de autenticación y autorización basado en JWT (JSON Web Tokens) que permite:

- **Autenticación**: Verificación de identidad mediante usuario y contraseña
- **Autorización**: Control de acceso basado en roles (Administrador, Gerente, Empleado)
- **Tokens JWT**: Tokens seguros con información del usuario y sus roles
- **Hash de Contraseñas**: Almacenamiento seguro usando `PasswordHasher`
- **Roles Múltiples**: Los usuarios pueden tener múltiples roles asignados

### ✨ Características Principales

- ✅ **Autenticación JWT** con tokens seguros y firmados
- ✅ **Autorización basada en roles** para control granular de acceso
- ✅ **Hash de contraseñas** usando `PasswordHasher` de ASP.NET Core Identity
- ✅ **Roles predefinidos**: Administrador, Gerente, Empleado
- ✅ **Asignación automática** de rol por defecto (Empleado) al registrarse
- ✅ **Múltiples roles** por usuario mediante relación many-to-many
- ✅ **Validación de tokens** en cada solicitud autenticada

---

## 🏗️ Arquitectura del Sistema

### 📊 Diagrama de Componentes

```
┌─────────────────┐
│   Cliente       │
│  (Frontend)     │
└────────┬────────┘
         │
         │ 1. POST /api/Usuario/register
         │ 2. POST /api/Usuario/token
         │ 3. GET /api/Producto (con Bearer Token)
         │
┌────────▼────────────────────────┐
│   UsuarioController             │
│   - RegisterAsync               │
│   - GetTokenAsync               │
│   - AddRoleAsync                │
└────────┬────────────────────────┘
         │
         │
┌────────▼────────────────────────┐
│   UserServices                  │
│   - RegisterAsync               │
│   - GetTokenAsync               │
│   - AddRoleAsync                │
│   - CreateJwtToken              │
└────────┬────────────────────────┘
         │
         ├─────────────────┬─────────────────┐
         │                 │                 │
┌────────▼────────┐ ┌──────▼──────┐ ┌───────▼────────┐
│ UnitOfWork      │ │ Password    │ │ JWT Config     │
│ - Usuarios      │ │ Hasher      │ │ - Key          │
│ - Roles         │ │             │ │ - Issuer       │
└────────┬────────┘ └────────────┘ │ - Audience     │
         │                          │ - Duration     │
         │                          └────────────────┘
┌────────▼────────┐
│   Database      │
│   - Usuarios    │
│   - Roles       │
│   - UsuariosRoles│
└─────────────────┘
```

---

## ⚙️ Configuración

### Configuración JWT

La configuración JWT se encuentra en `appsettings.json`:

**Ubicación:** `API/appsettings.json`

```json
{
  "JWT": {
    "Key": "EstaEsUnaClaveMuyLargaParaElTokenDeAutenticacion",
    "Issuer": "TiendaApi",
    "Audience": "TiendaApiUser",
    "DurationInMinutes": 30
  }
}
```

#### 📋 Parámetros de Configuración

| Parámetro | Descripción | Recomendación |
|-----------|-------------|---------------|
| `Key` | Clave secreta para firmar los tokens JWT | Mínimo 32 caracteres, usar una clave fuerte en producción |
| `Issuer` | Emisor del token (quién lo crea) | Nombre de la aplicación |
| `Audience` | Audiencia del token (para quién es) | Nombre de los usuarios/clientes |
| `DurationInMinutes` | Duración de validez del token en minutos | 30 minutos (ajustable según necesidades) |

> ⚠️ **Importante:** En producción, la `Key` debe ser una cadena larga y aleatoria almacenada de forma segura (variables de entorno, Azure Key Vault, etc.).

---

### Configuración en Program.cs

La configuración de autenticación y autorización se realiza en `Program.cs`:

**Ubicación:** `API/Program.cs`

```16:16:API/Program.cs
builder.Services.AddJwt(builder.Configuration);
```

```65:66:API/Program.cs
app.UseAuthentication();
app.UseAuthorization();
```

#### 🔍 Orden de Middleware

El orden es crítico:
1. `UseCors()` - Configuración CORS
2. `UseHttpsRedirection()` - Redirección HTTPS
3. `UseAuthentication()` - **Debe ir antes de UseAuthorization**
4. `UseAuthorization()` - Validación de permisos
5. `MapControllers()` - Enrutamiento

---

### Configuración de Servicios

La configuración detallada de JWT se realiza en `ApplicationServicesExtensions`:

**Ubicación:** `API/Extensions/ApplicationServicesExtensions.cs`

```83:109:API/Extensions/ApplicationServicesExtensions.cs
public static void AddJwt(this IServiceCollection services, IConfiguration configuration) 
{
    //Configuracion from AppSettings
    services.Configure<JWT>(configuration.GetSection("JWT"));

    //Adding Authentication - JWT
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.SaveToken = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = configuration["JWT:Issuer"],
            ValidAudience = configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["JWT:Key"]!))
        };
    });
}
```

#### ⚙️ Parámetros de Validación

| Parámetro | Valor | Descripción |
|-----------|-------|-------------|
| `ValidateIssuerSigningKey` | `true` | Valida que la clave de firma sea correcta |
| `ValidateIssuer` | `true` | Valida que el emisor del token sea válido |
| `ValidateAudience` | `true` | Valida que la audiencia del token sea correcta |
| `ValidateLifetime` | `true` | Valida que el token no haya expirado |
| `ClockSkew` | `TimeSpan.Zero` | Sin tolerancia para expiración (más estricto) |
| `RequireHttpsMetadata` | `false` | Permite HTTP en desarrollo (cambiar a `true` en producción) |

---

## 📦 Entidades y Modelos

### Entidad Usuario

**Ubicación:** `Core/Entities/Usuario.cs`

```9:22:Core/Entities/Usuario.cs
public class Usuario: BaseEntity
{
    public string Nombre { get; set; }
    public string ApellidoPaterno { get; set; }
    public string ApellidoMaterno { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public ICollection<Rol> Roles = new HashSet<Rol>();
    public ICollection<UsuariosRoles> UsuariosRoles { get; set; }
   

}
```

#### 📊 Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Nombre` | `string` | Nombre del usuario |
| `ApellidoPaterno` | `string` | Apellido paterno |
| `ApellidoMaterno` | `string` | Apellido materno |
| `Username` | `string` | Nombre de usuario único para login |
| `Email` | `string` | Correo electrónico único |
| `Password` | `string` | Contraseña hasheada (nunca en texto plano) |
| `Roles` | `ICollection<Rol>` | Roles asignados al usuario |
| `UsuariosRoles` | `ICollection<UsuariosRoles>` | Relación many-to-many con roles |

---

### Entidad Rol

**Ubicación:** `Core/Entities/Rol.cs`

```9:15:Core/Entities/Rol.cs
public class Rol : BaseEntity
{
    public string Nombre { get; set; }        
    public ICollection<Usuario> Usuarios { get; set; } = new HashSet<Usuario>();
    public ICollection<UsuariosRoles> UsuariosRoles { get; set; }
}
```

#### 📊 Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Nombre` | `string` | Nombre del rol (Administrador, Gerente, Empleado) |
| `Usuarios` | `ICollection<Usuario>` | Usuarios que tienen este rol |
| `UsuariosRoles` | `ICollection<UsuariosRoles>` | Relación many-to-many con usuarios |

---

### Entidad UsuariosRoles

Tabla intermedia para la relación many-to-many entre Usuarios y Roles:

**Ubicación:** `Core/Entities/UsuariosRoles.cs`

```9:16:Core/Entities/UsuariosRoles.cs
public class UsuariosRoles
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    public int RolId { get; set; }
    public Rol Rol { get; set; }
}
```

---

## 🔧 Servicios

### UserServices

Servicio principal que maneja la lógica de autenticación y autorización:

**Ubicación:** `API/Services/UserServices.cs`

#### 📋 Métodos Principales

| Método | Descripción |
|--------|-------------|
| `RegisterAsync` | Registra un nuevo usuario con rol por defecto |
| `GetTokenAsync` | Autentica un usuario y genera un token JWT |
| `AddRoleAsync` | Agrega un rol adicional a un usuario existente |
| `CreateJwtToken` | Crea un token JWT con los claims del usuario |

#### 🔐 Registro de Usuario

```27:67:API/Services/UserServices.cs
public async Task<string> RegisterAsync(RegisterDTO registerDTO)
{
    try
    {
        var userExiste = _unitOfWork.Usuarios
                            .Find(u => u.Email.ToLower() == registerDTO.Email.ToLower())
                            .FirstOrDefault();

        if (userExiste != null)
        {
            return $"El email {registerDTO.Email} ya se encuentra registrado";
        }

        var usuario = new Usuario
        {
            Nombre = registerDTO.Nombres,
            ApellidoPaterno = registerDTO.ApellidoPaterno,
            ApellidoMaterno = registerDTO.ApellidoMaterno,
            Email = registerDTO.Email,
            Username = registerDTO.Username
        };

        usuario.Password = _passwordHasher.HashPassword(usuario, registerDTO.Password);

        var rolPredeterminado = _unitOfWork.Roles
                                        .Find(r => r.Nombre == Autorizacion.rol_predeterminado.ToString())
                                        .FirstOrDefault();

        usuario.Roles.Add(rolPredeterminado);
        _unitOfWork.Usuarios.Add(usuario);

        await _unitOfWork.SaveAsync();

        return $"El usuario {registerDTO.Username} ha sido registrado exitosamente";
    }
    catch (Exception ex)
    {
        return $"Error al registrar el usuario {registerDTO.Username}: {ex.Message}";

    }
}
```

**Flujo de Registro:**
1. ✅ Verifica que el email no esté registrado
2. ✅ Crea el objeto Usuario
3. ✅ Hashea la contraseña usando `PasswordHasher`
4. ✅ Asigna el rol por defecto (Empleado)
5. ✅ Guarda el usuario en la base de datos

#### 🔑 Login y Generación de Token

```69:114:API/Services/UserServices.cs
public async Task<DatosUsuarioDTO> GetTokenAsync(LoginDTO loginDTO)
{
    try
    {
        DatosUsuarioDTO datosUsuarioDTO = new DatosUsuarioDTO();
        var usuario = await _unitOfWork.Usuarios.GetByUsernameAsync(loginDTO.Username);


        if (usuario == null)
        {
            datosUsuarioDTO.EstaAutenticado = false;
            datosUsuarioDTO.Mensaje = $"El usuario {loginDTO.Username} no existe";
            return datosUsuarioDTO;
        }

        var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.Password, loginDTO.Password);

        if (resultado == PasswordVerificationResult.Success)
        {
            datosUsuarioDTO.EstaAutenticado = true;
            JwtSecurityToken jwtSecurityToken = CreateJwtToken(usuario);
            datosUsuarioDTO.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            datosUsuarioDTO.Mensaje = $"El usuario {loginDTO.Username} ha sido autenticado exitosamente";
            datosUsuarioDTO.UserName = usuario.Username;
            datosUsuarioDTO.Email = usuario.Email;
            datosUsuarioDTO.Roles = usuario.Roles.Select(u => u.Nombre).ToList();

            return datosUsuarioDTO;
        }
        else
        {
            datosUsuarioDTO.EstaAutenticado = false;
            datosUsuarioDTO.Mensaje = $"Las credenciales del usuario {loginDTO.Username} son incorrectas";
            return datosUsuarioDTO;
        }

    }
    catch (Exception ex)
    {
        return new DatosUsuarioDTO
        {
            Mensaje = $"Error al autenticar el usuario {loginDTO.Username}: {ex.Message}",
            EstaAutenticado = false
        };
    }
}
```

**Flujo de Autenticación:**
1. ✅ Busca el usuario por username
2. ✅ Verifica la contraseña usando `PasswordHasher`
3. ✅ Si es correcta, genera un token JWT
4. ✅ Retorna el token junto con información del usuario y sus roles

#### 🎫 Creación del Token JWT

```151:179:API/Services/UserServices.cs
public JwtSecurityToken CreateJwtToken(Usuario usuario)
{
    var roles = usuario.Roles;
    var roleClaims = new List<Claim>();

    foreach (var rol in roles)
    {
        roleClaims.Add(new Claim(ClaimTypes.Role, rol.Nombre));
    }
    var claims = new List<Claim>
    {
       new Claim(JwtRegisteredClaimNames.Sub, usuario.Username),
       new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
       new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
       new Claim("uid", usuario.Id.ToString())

    }.Union(roleClaims);

    var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
    var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
    var jwtSecurityToken = new JwtSecurityToken(
        issuer: _jwt.Issuer,
        audience: _jwt.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
        signingCredentials: signingCredentials
        );
    return jwtSecurityToken;
}
```

**Claims Incluidos en el Token:**

| Claim | Valor | Descripción |
|-------|-------|-------------|
| `Sub` | Username | Subject (identificador del usuario) |
| `Jti` | GUID | JWT ID (identificador único del token) |
| `Email` | Email del usuario | Correo electrónico |
| `uid` | ID del usuario | ID numérico del usuario |
| `Role` | Nombre del rol | Roles del usuario (puede haber múltiples) |

---

### IUserServices

Interfaz que define el contrato del servicio:

**Ubicación:** `API/Services/IUserServices.cs`

```5:11:API/Services/IUserServices.cs
public interface IUserServices
{
    Task<string> RegisterAsync(RegisterDTO model);
    Task<DatosUsuarioDTO> GetTokenAsync(LoginDTO model);
    Task<string> AddRoleAsync(AddRoleDTO model);
}
```

---

## 🎮 Endpoints de Autenticación

### Registro de Usuario

**Endpoint:** `POST /api/Usuario/register`

**Request Body:**
```json
{
  "nombres": "Juan",
  "apellidoPaterno": "Pérez",
  "apellidoMaterno": "García",
  "email": "juan.perez@example.com",
  "username": "jperez",
  "password": "MiPassword123!"
}
```

**Response (Éxito):**
```json
{
  "El usuario jperez ha sido registrado exitosamente"
}
```

**Response (Error):**
```json
{
  "El email juan.perez@example.com ya se encuentra registrado"
}
```

---

### Login y Obtención de Token

**Endpoint:** `POST /api/Usuario/token`

**Request Body:**
```json
{
  "username": "jperez",
  "password": "MiPassword123!"
}
```

**Response (Éxito):**
```json
{
  "mensaje": "El usuario jperez ha sido autenticado exitosamente",
  "estaAutenticado": true,
  "userName": "jperez",
  "email": "juan.perez@example.com",
  "roles": ["Empleado"],
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (Error):**
```json
{
  "mensaje": "Las credenciales del usuario jperez son incorrectas",
  "estaAutenticado": false,
  "userName": null,
  "email": null,
  "roles": null,
  "token": null
}
```

---

### Agregar Rol a Usuario

**Endpoint:** `POST /api/Usuario/addrole`

**Request Body:**
```json
{
  "userName": "jperez",
  "password": "MiPassword123!",
  "role": "Gerente"
}
```

**Response (Éxito):**
```json
{
  "Rol Gerente agregado a la cuenta jperez de forma exitosa"
}
```

**Response (Error):**
```json
{
  "El rol Administrador no existe"
}
```

---

## 🛡️ Autorización en Controladores

### Uso del Atributo [Authorize]

Los controladores y acciones pueden protegerse usando el atributo `[Authorize]`:

**Ejemplo en ProductoController:**

```14:14:API/Controllers/ProductoController.cs
[Authorize(Roles = "Administrador")]
```

#### 📋 Niveles de Autorización

| Nivel | Sintaxis | Descripción |
|-------|----------|-------------|
| **Controlador Completo** | `[Authorize(Roles = "Administrador")]` | Todos los endpoints requieren el rol |
| **Acción Específica** | `[Authorize(Roles = "Gerente")]` | Solo esa acción requiere el rol |
| **Múltiples Roles** | `[Authorize(Roles = "Administrador,Gerente")]` | Cualquiera de los roles permite acceso |
| **Solo Autenticado** | `[Authorize]` | Solo requiere estar autenticado (sin rol específico) |

---

## 👥 Roles del Sistema

### Roles Predefinidos

Los roles se inicializan automáticamente al iniciar la aplicación:

**Ubicación:** `Infrastruture/Data/TiendaContextSeed.cs`

```79:102:Infrastruture/Data/TiendaContextSeed.cs
public static async Task SeedRolesAsync(TiendaContext context, ILoggerFactory loggerFactory)
{
    try
    {
        if (!context.Roles.Any())
        {
            var roles = new List<Rol>()
           {
               new Rol(){Id=1, Nombre="Administrador" },
               new Rol(){Id=2, Nombre="Gerente" },
               new Rol(){Id=3, Nombre="Empleado" },
           };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();

        }
    }
    catch (Exception ex)
    {
        var logger = loggerFactory.CreateLogger<TiendaContext>();
        logger.LogError(ex.Message);
    }
}
```

#### 📊 Roles Disponibles

| Rol | ID | Descripción | Permisos Típicos |
|-----|----|-------------|------------------|
| **Administrador** | 1 | Acceso completo al sistema | CRUD completo, gestión de usuarios y roles |
| **Gerente** | 2 | Acceso a operaciones de gestión | Lectura y modificación de datos |
| **Empleado** | 3 | Acceso básico (rol por defecto) | Solo lectura de datos |

### Rol por Defecto

**Ubicación:** `API/Helpers/Autorizacion.cs`

```3:14:API/Helpers/Autorizacion.cs
public class Autorizacion
{
    public enum Roles
    { 
        Administrador,
        Gerente,
        Empleado
    }

    public const Roles rol_predeterminado = Roles.Empleado;
}
```

Todos los usuarios nuevos se registran automáticamente con el rol **Empleado**.

---

## 🔄 Flujo de Autenticación

### Diagrama de Flujo Completo

```
┌─────────────┐
│   Cliente   │
└──────┬──────┘
       │
       │ 1. POST /api/Usuario/register
       │    { username, password, email, ... }
       │
┌──────▼──────────────────────────┐
│   UsuarioController             │
│   RegisterAsync                 │
└──────┬──────────────────────────┘
       │
       │ 2. UserServices.RegisterAsync
       │    - Verifica email único
       │    - Hashea password
       │    - Asigna rol Empleado
       │    - Guarda en BD
       │
┌──────▼──────────────────────────┐
│   Base de Datos                 │
│   Usuario creado                │
└─────────────────────────────────┘

┌─────────────┐
│   Cliente   │
└──────┬──────┘
       │
       │ 3. POST /api/Usuario/token
       │    { username, password }
       │
┌──────▼──────────────────────────┐
│   UsuarioController             │
│   GetTokenAsync                 │
└──────┬──────────────────────────┘
       │
       │ 4. UserServices.GetTokenAsync
       │    - Busca usuario
       │    - Verifica password
       │    - Crea token JWT
       │
┌──────▼──────────────────────────┐
│   Token JWT Generado            │
│   { token, roles, userInfo }    │
└──────┬──────────────────────────┘
       │
       │ 5. Cliente almacena token
       │
┌──────▼──────────────────────────┐
│   Cliente                       │
│   Token almacenado              │
└──────┬──────────────────────────┘
       │
       │ 6. GET /api/Producto
       │    Header: Authorization: Bearer {token}
       │
┌──────▼──────────────────────────┐
│   ProductoController             │
│   [Authorize(Roles="Admin")]    │
└──────┬──────────────────────────┘
       │
       │ 7. Middleware valida token
       │    - Verifica firma
       │    - Verifica expiración
       │    - Verifica roles
       │
┌──────▼──────────────────────────┐
│   Respuesta                      │
│   - 200 OK (si autorizado)      │
│   - 401 Unauthorized (si inválido)│
│   - 403 Forbidden (sin permisos) │
└─────────────────────────────────┘
```

---

## 💻 Ejemplos de Uso

### Ejemplo 1: Registro Completo

**Request:**
```http
POST /api/Usuario/register
Content-Type: application/json

{
  "nombres": "María",
  "apellidoPaterno": "González",
  "apellidoMaterno": "López",
  "email": "maria.gonzalez@example.com",
  "username": "mgonzalez",
  "password": "SecurePass123!"
}
```

**Response:**
```json
"El usuario mgonzalez ha sido registrado exitosamente"
```

---

### Ejemplo 2: Login y Uso del Token

**Paso 1: Obtener Token**
```http
POST /api/Usuario/token
Content-Type: application/json

{
  "username": "mgonzalez",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "mensaje": "El usuario mgonzalez ha sido autenticado exitosamente",
  "estaAutenticado": true,
  "userName": "mgonzalez",
  "email": "maria.gonzalez@example.com",
  "roles": ["Empleado"],
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtZ29uemFsZXoiLCJqdGkiOiI..."
}
```

**Paso 2: Usar Token en Solicitudes**
```http
GET /api/Producto?PageIndex=1&PageSize=5
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtZ29uemFsZXoiLCJqdGkiOiI...
```

---

### Ejemplo 3: Agregar Rol Administrador

**Request:**
```http
POST /api/Usuario/addrole
Content-Type: application/json

{
  "userName": "mgonzalez",
  "password": "SecurePass123!",
  "role": "Administrador"
}
```

**Response:**
```json
"Rol Administrador agregado a la cuenta mgonzalez de forma exitosa"
```

**Nuevo Login:**
```json
{
  "roles": ["Empleado", "Administrador"]
}
```

---

## 🎫 Estructura del Token JWT

### Token Decodificado

Un token JWT tiene tres partes separadas por puntos:

```
header.payload.signature
```

#### Header (Encabezado)
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

#### Payload (Carga Útil)
```json
{
  "sub": "mgonzalez",
  "jti": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "email": "maria.gonzalez@example.com",
  "uid": "1",
  "role": ["Empleado", "Administrador"],
  "iss": "TiendaApi",
  "aud": "TiendaApiUser",
  "exp": 1704567890,
  "iat": 1704566090
}
```

#### Signature (Firma)
```
HMACSHA256(
  base64UrlEncode(header) + "." +
  base64UrlEncode(payload),
  secret_key
)
```

---

## 🔒 Seguridad

### Mejores Prácticas Implementadas

| Práctica | Implementación |
|----------|----------------|
| **Hash de Contraseñas** | Usa `PasswordHasher` de ASP.NET Core Identity |
| **Tokens Firmados** | Tokens firmados con clave secreta usando HMAC SHA256 |
| **Validación de Expiración** | Tokens expiran después de 30 minutos |
| **Validación de Issuer/Audience** | Verifica que el token sea emitido por la API correcta |
| **Sin Clock Skew** | Validación estricta de expiración sin tolerancia |
| **Claims de Roles** | Roles incluidos en el token para autorización rápida |

### Recomendaciones Adicionales

- ✅ **Usar HTTPS** en producción para proteger tokens en tránsito
- ✅ **Almacenar tokens** de forma segura (no en localStorage si es posible)
- ✅ **Implementar refresh tokens** para renovar tokens sin re-autenticación
- ✅ **Rotar la clave JWT** periódicamente
- ✅ **Implementar rate limiting** en endpoints de autenticación
- ✅ **Logging de intentos** de autenticación fallidos
- ✅ **Validación de contraseñas** fuertes en el frontend y backend

---

## ✨ Ventajas del Sistema

| Ventaja | Descripción |
|---------|-------------|
| 🔐 **Seguridad** | Tokens JWT firmados y contraseñas hasheadas |
| ⚡ **Rendimiento** | Validación rápida sin consultas a BD en cada request |
| 🔄 **Escalabilidad** | Stateless (sin estado en servidor) |
| 👥 **Roles Múltiples** | Un usuario puede tener varios roles |
| 🎯 **Granularidad** | Control de acceso a nivel de controlador y acción |
| ♻️ **Reutilizable** | Sistema genérico aplicable a cualquier endpoint |
| 📱 **Compatible** | Funciona con cualquier cliente (web, móvil, desktop) |

---

## 🔬 Notas Técnicas

### Dependencias Requeridas

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.22" />
<PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.2.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.15.0" />
```

### Configuración de Servicios

```csharp
services.AddScoped<IUserServices, UserServices>();
services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
```

### Inicialización de Roles

Los roles se crean automáticamente al iniciar la aplicación mediante `SeedRolesAsync()` en `Program.cs`.

### Estructura de Base de Datos

- **Tabla Usuarios**: Almacena información de usuarios
- **Tabla Roles**: Almacena los roles disponibles
- **Tabla UsuariosRoles**: Tabla intermedia para relación many-to-many

---

## 📚 Referencias

- **Configuración JWT:** `API/Extensions/ApplicationServicesExtensions.cs`
- **Servicio de Usuarios:** `API/Services/UserServices.cs`
- **Controlador:** `API/Controllers/UsuarioController.cs`
- **Entidades:** `Core/Entities/Usuario.cs`, `Core/Entities/Rol.cs`
- **Helpers:** `API/Helpers/JWT.cs`, `API/Helpers/Autorizacion.cs`
- **DTOs:** `API/DTO/RegisterDTO.cs`, `API/DTO/LoginDTO.cs`, `API/DTO/DatosUsuarioDTO.cs`
- **Seed:** `Infrastruture/Data/TiendaContextSeed.cs`

---

## 🔗 Integración con Otros Sistemas

El sistema de autenticación se integra con:

1. **Sistema de Paginación**: Los endpoints protegidos pueden usar paginación
2. **Sistema de Búsqueda**: La búsqueda puede estar protegida por roles
3. **Sistema XML**: Los tokens funcionan con respuestas JSON y XML
4. **Rate Limiting**: Los endpoints de autenticación pueden tener límites de tasa

---

<div align="center">

**Documentación generada para el Sistema de Autenticación y Autorización** 🔐

</div>
