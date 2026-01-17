# 🔄 Sistema de Refresh Token

> **Sistema completo de renovación de tokens JWT mediante Refresh Tokens, permitiendo mantener sesiones activas sin necesidad de re-autenticarse constantemente, mejorando la experiencia del usuario y la seguridad.**

---

## 📑 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [¿Qué es un Refresh Token?](#-qué-es-un-refresh-token)
- [Arquitectura del Sistema](#-arquitectura-del-sistema)
- [Entidad RefreshToken](#-entidad-refreshtoken)
- [Implementación](#-implementación)
  - [Creación de Refresh Token](#creación-de-refresh-token)
  - [Renovación de Token](#renovación-de-token)
  - [Gestión de Cookies](#gestión-de-cookies)
- [Endpoints](#-endpoints)
- [Flujo de Autenticación con Refresh Token](#-flujo-de-autenticación-con-refresh-token)
- [Ejemplos de Uso](#-ejemplos-de-uso)
- [Seguridad](#-seguridad)
- [Ventajas](#-ventajas-del-sistema)
- [Configuración](#-configuración)
- [Notas Técnicas](#-notas-técnicas)

---

## 🎯 Descripción General

Se ha implementado un sistema completo de Refresh Tokens que permite renovar los tokens JWT sin necesidad de volver a autenticarse con usuario y contraseña. Este sistema mejora significativamente la experiencia del usuario al mantener sesiones activas durante períodos más largos.

### ✨ Características Principales

- ✅ **Renovación automática** de tokens JWT sin re-autenticación
- ✅ **Almacenamiento seguro** en cookies HTTP-only
- ✅ **Múltiples tokens activos** por usuario
- ✅ **Rotación de tokens** (revocación del anterior al generar uno nuevo)
- ✅ **Validación de estado** (activo, expirado, revocado)
- ✅ **Duración extendida** (10 días vs 1 minuto del JWT)

### 🔄 Diferencia entre Access Token y Refresh Token

| Característica | Access Token (JWT) | Refresh Token |
|----------------|-------------------|---------------|
| **Duración** | Corta (1 minuto) | Larga (10 días) |
| **Uso** | Cada solicitud autenticada | Solo para renovar el access token |
| **Almacenamiento** | Cliente (localStorage/memoria) | Cookie HTTP-only |
| **Exposición** | Enviado en cada request | Solo en endpoint de renovación |
| **Contenido** | Claims del usuario | Token aleatorio |

---

## 🔍 ¿Qué es un Refresh Token?

Un **Refresh Token** es un token de larga duración que se utiliza exclusivamente para obtener nuevos Access Tokens (JWT) cuando estos expiran. A diferencia del Access Token que contiene información del usuario y se envía en cada solicitud, el Refresh Token:

- Es un token opaco (no contiene información)
- Se almacena de forma segura en cookies HTTP-only
- Tiene una duración mucho mayor que el Access Token
- Solo se usa para renovar el Access Token
- Puede ser revocado si se detecta actividad sospechosa

---

## 🏗️ Arquitectura del Sistema

### 📊 Diagrama de Flujo

```
┌─────────────┐
│   Cliente   │
└──────┬──────┘
       │
       │ 1. POST /api/Usuario/token
       │    { username, password }
       │
┌──────▼──────────────────────────┐
│   UsuarioController             │
│   GetTokenAsync                 │
└──────┬──────────────────────────┘
       │
       │ 2. UserServices.GetTokenAsync
       │    - Valida credenciales
       │    - Genera JWT (1 min)
       │    - Verifica refresh token activo
       │    - Si no existe, crea uno nuevo (10 días)
       │
┌──────▼──────────────────────────┐
│   Response                      │
│   - JWT Token                   │
│   - Refresh Token (en cookie)   │
│   - DatosUsuarioDTO              │
└──────┬──────────────────────────┘
       │
       │ 3. Cliente usa JWT para requests
       │    Header: Authorization: Bearer {JWT}
       │
       │ 4. JWT expira (después de 1 min)
       │
       │ 5. POST /api/Usuario/refreshtoken
       │    Cookie: refreshToken={token}
       │
┌──────▼──────────────────────────┐
│   UsuarioController             │
│   RefreshTokenAsync             │
└──────┬──────────────────────────┘
       │
       │ 6. UserServices.RefreshTokenAsync
       │    - Valida refresh token
       │    - Revoca token anterior
       │    - Genera nuevo JWT
       │    - Genera nuevo refresh token
       │
┌──────▼──────────────────────────┐
│   Response                      │
│   - Nuevo JWT Token             │
│   - Nuevo Refresh Token (cookie)│
└─────────────────────────────────┘
```

---

## 📦 Entidad RefreshToken

La entidad `RefreshToken` almacena la información de los tokens de renovación:

**Ubicación:** `Core/Entities/RefreshToken.cs`

```9:19:Core/Entities/RefreshToken.cs
public class RefreshToken: BaseEntity
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }
    public bool IsActive => Revoked == null && !IsExpired;
}
```

### 📊 Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `UsuarioId` | `int` | ID del usuario propietario del token |
| `Usuario` | `Usuario` | Referencia de navegación al usuario |
| `Token` | `string` | Token aleatorio en Base64 |
| `Expires` | `DateTime` | Fecha y hora de expiración |
| `IsExpired` | `bool` | Propiedad calculada: ¿está expirado? |
| `Created` | `DateTime` | Fecha y hora de creación |
| `Revoked` | `DateTime?` | Fecha y hora de revocación (null si está activo) |
| `IsActive` | `bool` | Propiedad calculada: ¿está activo? (no revocado y no expirado) |

### 🔗 Relación con Usuario

**Ubicación:** `Core/Entities/Usuario.cs`

```19:19:Core/Entities/Usuario.cs
public ICollection<RefreshToken> RefreshTokens { get; set; }
```

Un usuario puede tener múltiples Refresh Tokens (relación one-to-many).

---

## 🔧 Implementación

### Creación de Refresh Token

El método `CreateRefreshToken()` genera un nuevo Refresh Token aleatorio:

**Ubicación:** `API/Services/UserServices.cs`

```207:220:API/Services/UserServices.cs
private RefreshToken CreateRefreshToken()
{
    var randomNumber = new byte[32];
    using (var generator = RandomNumberGenerator.Create())
    {
        generator.GetBytes(randomNumber);
        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomNumber),
            Expires = DateTime.UtcNow.AddDays(10),
            Created = DateTime.UtcNow
        };
    }
}
```

#### 🔍 Proceso de Creación

1. **Genera bytes aleatorios**: Crea un array de 32 bytes usando `RandomNumberGenerator`
2. **Convierte a Base64**: Convierte los bytes aleatorios a una cadena Base64
3. **Establece expiración**: Configura la expiración a 10 días desde ahora
4. **Marca fecha de creación**: Establece `Created` con la fecha actual

#### 📋 Características del Token

- **Longitud**: 32 bytes = 44 caracteres en Base64
- **Aleatoriedad**: Usa `RandomNumberGenerator` criptográficamente seguro
- **Duración**: 10 días desde la creación
- **Formato**: Base64 string

---

### Generación en Login

Cuando un usuario hace login, se verifica si tiene un Refresh Token activo:

**Ubicación:** `API/Services/UserServices.cs`

```97:111:API/Services/UserServices.cs
if (usuario.RefreshTokens.Any(a => a.IsActive))
{
    var activeRefreshToken = usuario.RefreshTokens.Where(x => x.IsActive).FirstOrDefault();
    datosUsuarioDTO.RefreshToken = activeRefreshToken.Token;
    datosUsuarioDTO.RefreshTokenExpirete = activeRefreshToken.Expires;
}
else 
{
    var refresthToken =  CreateRefreshToken();
    datosUsuarioDTO.RefreshToken = refresthToken.Token;
    datosUsuarioDTO.RefreshTokenExpirete = refresthToken.Expires;
    usuario.RefreshTokens.Add(refresthToken);
    _unitOfWork.Usuarios.Update(usuario);
    await _unitOfWork.SaveAsync();
}
```

#### 🔄 Lógica de Generación

| Condición | Acción |
|-----------|--------|
| **Usuario tiene Refresh Token activo** | Reutiliza el token existente |
| **Usuario NO tiene Refresh Token activo** | Crea un nuevo Refresh Token |

> 💡 **Nota:** Si el usuario tiene múltiples Refresh Tokens activos, se usa el primero encontrado.

---

### Renovación de Token

El método `RefreshTokenAsync()` renueva el Access Token usando el Refresh Token:

**Ubicación:** `API/Services/UserServices.cs`

```168:206:API/Services/UserServices.cs
public async Task<DatosUsuarioDTO> RefreshTokenAsync(string refreshToken)
{
    var datosUsuarioDTO = new DatosUsuarioDTO();
    var usuario = await _unitOfWork.Usuarios.GetByRefreshTokenAsync(refreshToken);
                        
    if (usuario == null)
    {
        datosUsuarioDTO.EstaAutenticado = false;
        datosUsuarioDTO.Mensaje = "El refresh token no es válido";
        return datosUsuarioDTO;
    }

    var refreshTokenDB = usuario.RefreshTokens.Single(x => x.Token == refreshToken);

    if (!refreshTokenDB.IsActive)
    {
        datosUsuarioDTO.EstaAutenticado = false;
        datosUsuarioDTO.Mensaje = "El refresh token no es válido";
        return datosUsuarioDTO;
    }
    //Revocar el refresh token actual y generar uno nuevo
    refreshTokenDB.Revoked = DateTime.UtcNow;
    //Generar uno nuevo y lo guardamos en la base de datos
    var nuevoRefreshToken = CreateRefreshToken();
    usuario.RefreshTokens.Add(nuevoRefreshToken);
    _unitOfWork.Usuarios.Update(usuario);
    await _unitOfWork.SaveAsync();
    //Generamos un nuevo JWT 🥰
    datosUsuarioDTO.EstaAutenticado = true;
    JwtSecurityToken jwtSecurityToken = CreateJwtToken(usuario);
    datosUsuarioDTO.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
    datosUsuarioDTO.Email = usuario.Email;
    datosUsuarioDTO.UserName = usuario.Username;
    datosUsuarioDTO.Roles = usuario.Roles.Select(u => u.Nombre).ToList();
    datosUsuarioDTO.RefreshToken = nuevoRefreshToken.Token;
    datosUsuarioDTO.RefreshTokenExpirete = nuevoRefreshToken.Expires;
    datosUsuarioDTO.Mensaje = "El token ha sido renovado exitosamente";
    return datosUsuarioDTO; 
}
```

#### 🔄 Proceso de Renovación

1. **Busca el usuario** por Refresh Token
2. **Valida que el token exista** en la base de datos
3. **Verifica que esté activo** (no revocado y no expirado)
4. **Revoca el token actual** estableciendo `Revoked = DateTime.UtcNow`
5. **Genera un nuevo Refresh Token**
6. **Genera un nuevo Access Token (JWT)**
7. **Guarda los cambios** en la base de datos
8. **Retorna** el nuevo JWT y Refresh Token

#### 🔐 Rotación de Tokens

El sistema implementa **rotación de tokens**, lo que significa que cada vez que se renueva un token, el anterior se revoca y se genera uno nuevo. Esto mejora la seguridad al:

- Limitar el tiempo de uso de cada token
- Detectar posibles compromisos (si se intenta usar un token revocado)
- Forzar la rotación periódica

---

### Gestión de Cookies

El Refresh Token se almacena en una cookie HTTP-only para mayor seguridad:

**Ubicación:** `API/Controllers/UsuarioController.cs`

```69:77:API/Controllers/UsuarioController.cs
private void SetRefreshTokenInCookie(string refreshToken)
{
    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Expires = DateTime.UtcNow.AddDays(10)
    };
    Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
}
```

#### 🍪 Configuración de la Cookie

| Propiedad | Valor | Descripción |
|-----------|-------|-------------|
| `HttpOnly` | `true` | Previene acceso desde JavaScript (protección XSS) |
| `Expires` | `DateTime.UtcNow.AddDays(10)` | Expira en 10 días |
| `Secure` | `false` (por defecto) | En producción debería ser `true` para HTTPS |

#### 📋 Uso de Cookies

**En Login:**
```37:37:API/Controllers/UsuarioController.cs
SetRefreshTokenInCookie(resultado.RefreshToken);
```

**En Refresh Token:**
```63:64:API/Controllers/UsuarioController.cs
if (!string.IsNullOrEmpty(resultado.RefreshToken))
    SetRefreshTokenInCookie(resultado.RefreshToken);
```

**Lectura del Cookie:**
```61:61:API/Controllers/UsuarioController.cs
var refreshToken = Request.Cookies["refreshToken"];
```

---

## 🎮 Endpoints

### POST /api/Usuario/token

Endpoint de login que genera tanto el Access Token como el Refresh Token.

**Request:**
```http
POST /api/Usuario/token
Content-Type: application/json

{
  "username": "jperez",
  "password": "MiPassword123!"
}
```

**Response:**
```json
{
  "mensaje": "El usuario jperez ha sido autenticado exitosamente",
  "estaAutenticado": true,
  "userName": "jperez",
  "email": "juan.perez@example.com",
  "roles": ["Empleado"],
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshTokenExpirete": "2025-01-21T10:30:00Z"
}
```

**Cookies:**
```
Set-Cookie: refreshToken=abc123xyz...; HttpOnly; Expires=Mon, 21 Jan 2025 10:30:00 GMT
```

> ⚠️ **Nota:** El `refreshToken` NO se incluye en el JSON response (está marcado con `[JsonIgnore]`), solo se envía en la cookie.

---

### POST /api/Usuario/refreshtoken

Endpoint para renovar el Access Token usando el Refresh Token.

**Request:**
```http
POST /api/Usuario/refreshtoken
Cookie: refreshToken=abc123xyz...
```

**Response (Éxito):**
```json
{
  "mensaje": "El token ha sido renovado exitosamente",
  "estaAutenticado": true,
  "userName": "jperez",
  "email": "juan.perez@example.com",
  "roles": ["Empleado"],
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshTokenExpirete": "2025-01-21T10:35:00Z"
}
```

**Response (Error):**
```json
{
  "mensaje": "El refresh token no es válido",
  "estaAutenticado": false,
  "userName": null,
  "email": null,
  "roles": null,
  "token": null,
  "refreshTokenExpirete": "0001-01-01T00:00:00Z"
}
```

---

## 🔄 Flujo de Autenticación con Refresh Token

### Diagrama de Flujo Completo

```
┌─────────────────────────────────────────────────────────────┐
│                    FLUJO COMPLETO                           │
└─────────────────────────────────────────────────────────────┘

1. LOGIN INICIAL
   ┌─────────────┐
   │   Cliente   │
   └──────┬──────┘
          │
          │ POST /api/Usuario/token
          │ { username, password }
          │
   ┌──────▼──────────────────────────┐
   │   Servidor                      │
   │   - Valida credenciales         │
   │   - Genera JWT (1 min)          │
   │   - Genera Refresh Token (10 días)│
   │   - Guarda Refresh Token en BD  │
   │   - Envía Refresh Token en cookie│
   └──────┬──────────────────────────┘
          │
          │ Response:
          │ - JWT Token (en body)
          │ - Refresh Token (en cookie)
          │
   ┌──────▼──────────────────────────┐
   │   Cliente                       │
   │   - Almacena JWT                │
   │   - Cookie se almacena automáticamente│
   └─────────────────────────────────┘

2. USO DEL ACCESS TOKEN
   ┌─────────────┐
   │   Cliente   │
   └──────┬──────┘
          │
          │ GET /api/Producto
          │ Authorization: Bearer {JWT}
          │
   ┌──────▼──────────────────────────┐
   │   Servidor                      │
   │   - Valida JWT                  │
   │   - Procesa request             │
   └──────┬──────────────────────────┘
          │
          │ Response: Datos solicitados
          │
   ┌──────▼──────────────────────────┐
   │   Cliente                       │
   │   - Recibe datos                │
   └─────────────────────────────────┘

3. JWT EXPIRA (después de 1 minuto)
   ┌─────────────┐
   │   Cliente   │
   └──────┬──────┘
          │
          │ GET /api/Producto
          │ Authorization: Bearer {JWT_EXPIRADO}
          │
   ┌──────▼──────────────────────────┐
   │   Servidor                      │
   │   - Valida JWT                  │
   │   - ❌ Token expirado           │
   └──────┬──────────────────────────┘
          │
          │ Response: 401 Unauthorized
          │
   ┌──────▼──────────────────────────┐
   │   Cliente                       │
   │   - Detecta 401                │
   │   - Llama a /refreshtoken       │
   └─────────────────────────────────┘

4. RENOVACIÓN DEL TOKEN
   ┌─────────────┐
   │   Cliente   │
   └──────┬──────┘
          │
          │ POST /api/Usuario/refreshtoken
          │ Cookie: refreshToken={token}
          │
   ┌──────▼──────────────────────────┐
   │   Servidor                      │
   │   - Lee Refresh Token de cookie │
   │   - Valida Refresh Token        │
   │   - Revoca token anterior      │
   │   - Genera nuevo JWT            │
   │   - Genera nuevo Refresh Token  │
   │   - Guarda en BD                │
   └──────┬──────────────────────────┘
          │
          │ Response:
          │ - Nuevo JWT Token
          │ - Nuevo Refresh Token (cookie)
          │
   ┌──────▼──────────────────────────┐
   │   Cliente                       │
   │   - Actualiza JWT               │
   │   - Cookie se actualiza automáticamente│
   │   - Reintenta request original  │
   └─────────────────────────────────┘
```

---

## 💻 Ejemplos de Uso

### Ejemplo 1: Login y Obtención de Tokens

**Request:**
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
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtZ29uemFsZXoiLCJqdGkiOiI...",
  "refreshTokenExpirete": "2025-01-21T10:30:00Z"
}
```

**Headers de Response:**
```
Set-Cookie: refreshToken=aBc123XyZ456...; HttpOnly; Expires=Mon, 21 Jan 2025 10:30:00 GMT; Path=/
```

---

### Ejemplo 2: Uso del Access Token

**Request:**
```http
GET /api/Producto?PageIndex=1&PageSize=5
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtZ29uemFsZXoiLCJqdGkiOiI...
```

**Response:**
```json
{
  "pageIndex": 1,
  "pageSize": 5,
  "total": 25,
  "search": null,
  "registers": [...]
}
```

---

### Ejemplo 3: Renovación del Token

**Escenario:** El JWT ha expirado (después de 1 minuto)

**Request:**
```http
POST /api/Usuario/refreshtoken
Cookie: refreshToken=aBc123XyZ456...
```

**Response:**
```json
{
  "mensaje": "El token ha sido renovado exitosamente",
  "estaAutenticado": true,
  "userName": "mgonzalez",
  "email": "maria.gonzalez@example.com",
  "roles": ["Empleado"],
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJtZ29uemFsZXoiLCJqdGkiOiI...",
  "refreshTokenExpirete": "2025-01-21T10:35:00Z"
}
```

**Headers de Response:**
```
Set-Cookie: refreshToken=xYz789AbC123...; HttpOnly; Expires=Mon, 21 Jan 2025 10:35:00 GMT; Path=/
```

> 💡 **Nota:** El Refresh Token anterior fue revocado y se generó uno nuevo.

---

### Ejemplo 4: Refresh Token Inválido

**Request:**
```http
POST /api/Usuario/refreshtoken
Cookie: refreshToken=token_invalido_o_expirado
```

**Response:**
```json
{
  "mensaje": "El refresh token no es válido",
  "estaAutenticado": false,
  "userName": null,
  "email": null,
  "roles": null,
  "token": null,
  "refreshTokenExpirete": "0001-01-01T00:00:00Z"
}
```

---

## 🔒 Seguridad

### Mejores Prácticas Implementadas

| Práctica | Implementación |
|----------|----------------|
| **HTTP-only Cookies** | Previene acceso desde JavaScript (protección XSS) |
| **Rotación de Tokens** | Cada renovación genera un nuevo token y revoca el anterior |
| **Validación de Estado** | Verifica que el token esté activo (no revocado, no expirado) |
| **Tokens Aleatorios** | Usa `RandomNumberGenerator` criptográficamente seguro |
| **Almacenamiento en BD** | Los tokens se guardan en base de datos para validación |
| **Expiración Corta del JWT** | Access Token expira en 1 minuto (ajustable) |
| **Expiración Larga del Refresh** | Refresh Token dura 10 días |

### Recomendaciones Adicionales

- ✅ **Usar HTTPS** en producción para proteger cookies en tránsito
- ✅ **Configurar `Secure` flag** en cookies para HTTPS-only
- ✅ **Implementar rate limiting** en el endpoint de refresh token
- ✅ **Logging de intentos** de renovación fallidos
- ✅ **Revocación masiva** si se detecta compromiso
- ✅ **Límite de Refresh Tokens** activos por usuario
- ✅ **SameSite cookie attribute** para protección CSRF

### Posibles Mejoras

1. **Refresh Token Rotation**: Ya implementado ✅
2. **Token Reuse Detection**: Detectar si se intenta usar un token revocado
3. **Device Tracking**: Asociar tokens con dispositivos específicos
4. **Geolocation Validation**: Validar ubicación del usuario
5. **Refresh Token Limit**: Limitar cantidad de tokens activos por usuario

---

## ✨ Ventajas del Sistema

| Ventaja | Descripción |
|---------|-------------|
| 🔐 **Seguridad Mejorada** | Access Tokens de corta duración reducen el riesgo de compromiso |
| 👤 **Mejor UX** | Los usuarios no necesitan re-autenticarse constantemente |
| 🔄 **Rotación Automática** | Los tokens se renuevan automáticamente |
| 🍪 **Almacenamiento Seguro** | Cookies HTTP-only previenen acceso desde JavaScript |
| 📊 **Trazabilidad** | Los tokens se almacenan en BD para auditoría |
| ⚡ **Rendimiento** | No requiere consultas a BD en cada request (solo en renovación) |
| 🛡️ **Protección XSS** | Cookies HTTP-only no son accesibles desde JavaScript |

---

## ⚙️ Configuración

### Duración de Tokens

**Access Token (JWT):**
**Ubicación:** `API/appsettings.json`

```9:14:API/appsettings.json
"JWT": {
  "Key": "EstaEsUnaClaveMuyLargaParaElTokenDeAutenticacion",
  "Issuer": "TiendaApi",
  "Audience": "TiendaApiUser",
  "DurationInMinutes": 1
}
```

**Refresh Token:**
Configurado en código: **10 días**

```216:216:API/Services/UserServices.cs
Expires = DateTime.UtcNow.AddDays(10),
```

### Configuración de Cookie

**Ubicación:** `API/Controllers/UsuarioController.cs`

```71:75:API/Controllers/UsuarioController.cs
var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Expires = DateTime.UtcNow.AddDays(10)
};
```

---

## 🔬 Notas Técnicas

### Repositorio

El repositorio incluye métodos para buscar usuarios por Refresh Token:

**Ubicación:** `Infrastruture/Repositories/UsuarioRepository.cs`

```21:27:Infrastruture/Repositories/UsuarioRepository.cs
public async Task<Usuario> GetByRefreshTokenAsync(string refreshToken) 
{
    return await _context.Usuarios
        .Include(u => u.Roles)
        .Include(u => u.RefreshTokens)
        .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t=>t.Token == refreshToken));
}
```

### DTO

El DTO incluye propiedades para Refresh Token:

**Ubicación:** `API/DTO/DatosUsuarioDTO.cs`

```13:15:API/DTO/DatosUsuarioDTO.cs
[JsonIgnore]
public string RefreshToken { get; set; }
public DateTime RefreshTokenExpirete { get; set; }
```

> 💡 **Nota:** `RefreshToken` está marcado con `[JsonIgnore]` para que no se incluya en la respuesta JSON (solo se envía en la cookie).

### Interfaz del Servicio

**Ubicación:** `API/Services/IUserServices.cs`

```10:10:API/Services/IUserServices.cs
Task<DatosUsuarioDTO> RefreshTokenAsync(string refreshToken);
```

### Configuración de Entidad

**Ubicación:** `Infrastruture/Data/Configuration/RefreshTokenConfiguration.cs`

```10:16:Infrastruture/Data/Configuration/RefreshTokenConfiguration.cs
public class RefreshTokenConfiguration:IEntityTypeConfiguration<Core.Entities.RefreshToken>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Core.Entities.RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");          
    }        
}
```

---

## 📊 Comparación: Con vs Sin Refresh Token

### Sin Refresh Token

```
Usuario → Login → JWT (30 min) → Expira → Login nuevamente
```

**Problemas:**
- Usuario debe re-autenticarse frecuentemente
- Si el JWT es robado, es válido por mucho tiempo
- Mala experiencia de usuario

### Con Refresh Token

```
Usuario → Login → JWT (1 min) + Refresh Token (10 días)
         ↓
    JWT expira
         ↓
    Refresh Token → Nuevo JWT + Nuevo Refresh Token
         ↓
    Continúa usando la aplicación sin re-login
```

**Ventajas:**
- Usuario no necesita re-autenticarse por 10 días
- JWT de corta duración reduce riesgo de compromiso
- Mejor experiencia de usuario

---

## 🔗 Integración con Otros Sistemas

El sistema de Refresh Token se integra con:

1. **Sistema de Autenticación JWT**: Renueva los Access Tokens
2. **Sistema de Roles**: Los roles se mantienen en el nuevo JWT
3. **Sistema de Cookies**: Usa cookies HTTP-only para almacenamiento seguro
4. **Base de Datos**: Almacena tokens para validación y auditoría

---

## 📚 Referencias

- **Entidad:** `Core/Entities/RefreshToken.cs`
- **Servicio:** `API/Services/UserServices.cs`
- **Controlador:** `API/Controllers/UsuarioController.cs`
- **Repositorio:** `Infrastruture/Repositories/UsuarioRepository.cs`
- **DTO:** `API/DTO/DatosUsuarioDTO.cs`
- **Configuración:** `Infrastruture/Data/Configuration/RefreshTokenConfiguration.cs`

---

## 🎯 Casos de Uso

### Caso 1: Sesión Persistente

Un usuario puede mantener su sesión activa durante 10 días sin necesidad de volver a ingresar sus credenciales.

### Caso 2: Múltiples Dispositivos

Un usuario puede tener múltiples Refresh Tokens activos (uno por dispositivo), permitiendo sesiones simultáneas.

### Caso 3: Revocación Selectiva

Si un dispositivo es comprometido, se puede revocar su Refresh Token específico sin afectar otros dispositivos.

### Caso 4: Renovación Automática

Las aplicaciones pueden implementar lógica para renovar automáticamente el token cuando detecten un 401, mejorando la experiencia del usuario.

---

<div align="center">

**Documentación generada para el Sistema de Refresh Token** 🔄

</div>
