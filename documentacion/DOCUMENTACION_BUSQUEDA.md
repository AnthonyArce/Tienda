# 🔍 Sistema de Búsqueda

> **Sistema completo de búsqueda integrado con paginación que permite filtrar registros por texto, mejorando la experiencia del usuario al encontrar información específica de manera rápida y eficiente.**

---

## 📑 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Componentes Principales](#-componentes-principales)
  - [Propiedad `Search` en `Params`](#1-propiedad-search-en-params)
  - [Propiedad `Search` en `Pager<T>`](#2-propiedad-search-en-pagert)
- [Implementación](#-implementación)
  - [Interfaz del Repositorio](#interfaz-igenericrepositoryt)
  - [Implementación Genérica](#implementación-genérica)
  - [Implementación Específica](#implementación-específica-en-productorepository)
- [Uso en Controladores](#-uso-en-controladores)
- [Ejemplos de Uso](#-ejemplos-de-uso)
- [Estructura de Respuesta](#-estructura-de-respuesta-json)
- [Características de Búsqueda](#-características-de-búsqueda)
- [Ventajas](#-ventajas-de-la-implementación)
- [Extensibilidad](#-extensibilidad)
- [Notas Técnicas](#-notas-técnicas)

---

## 🎯 Descripción General

Se ha implementado un sistema de búsqueda completo que se integra perfectamente con el sistema de paginación existente. Esta funcionalidad permite a los usuarios filtrar registros mediante búsqueda de texto, mejorando significativamente la capacidad de encontrar información específica en grandes conjuntos de datos.

### ✨ Características Principales

- ✅ **Búsqueda case-insensitive** (no distingue mayúsculas/minúsculas)
- ✅ **Integración con paginación** para resultados paginados
- ✅ **Normalización automática** del texto de búsqueda
- ✅ **Búsqueda parcial** usando `Contains` (coincidencias parciales)
- ✅ **Búsqueda en tiempo real** con filtrado eficiente

---

## 🧩 Componentes Principales

### 1. Propiedad `Search` en `Params`

La clase `Params` ahora incluye una propiedad `Search` que normaliza automáticamente el texto de búsqueda recibido desde la query string.

**Ubicación:** `API/Helpers/Params.cs`

```21:25:API/Helpers/Params.cs
public string Search
{
    get => _search;
    set => _search = (!String.IsNullOrEmpty(value))?value.ToLower():"";
}
```

#### ⚙️ Características

| Característica | Descripción |
|----------------|-------------|
| **Normalización** | Convierte automáticamente el texto a minúsculas |
| **Validación** | Si el valor es `null` o vacío, se establece como cadena vacía |
| **Case-Insensitive** | La búsqueda no distingue entre mayúsculas y minúsculas |

#### 🔄 Proceso de Normalización

```
Entrada: "LAPTOP"     → Normalizado: "laptop"
Entrada: "Laptop"    → Normalizado: "laptop"
Entrada: "laptop"     → Normalizado: "laptop"
Entrada: null        → Normalizado: ""
Entrada: ""          → Normalizado: ""
```

---

### 2. Propiedad `Search` en `Pager<T>`

La clase `Pager<T>` ahora incluye una propiedad `Search` que almacena el término de búsqueda utilizado, permitiendo que el cliente conozca el filtro aplicado.

**Ubicación:** `API/Helpers/Pager.cs`

```8:18:API/Helpers/Pager.cs
public string Search { get; private set; }
public IEnumerable<T> Registers { get; private set; }

public Pager(IEnumerable<T> registers, int total, int pageIndex, int pageSize, string search)
{
    Registers = registers;
    Total = total;
    PageIndex = pageIndex;
    PageSize = pageSize;
    Search = search;
}
```

#### 📊 Propiedades Relacionadas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Search` | `string` | Término de búsqueda utilizado (read-only) |
| `Registers` | `IEnumerable<T>` | Registros filtrados que coinciden con la búsqueda |
| `Total` | `int` | Total de registros que coinciden con la búsqueda |

> 💡 **Nota:** La propiedad `Total` refleja el total de registros **después** de aplicar el filtro de búsqueda, no el total de registros en la tabla.

---

## 🔧 Implementación

### Interfaz `IGenericRepository<T>`

Se actualizó el método `GetAllAsync` para incluir el parámetro de búsqueda:

**Ubicación:** `Core/Interfaces/IGenericRepository.cs`

```16:16:Core/Interfaces/IGenericRepository.cs
Task<(int totalRegistros, IEnumerable<T> registros)> GetAllAsync(int pageIndex, int pageSize, string search);
```

#### 📤 Parámetros

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `pageIndex` | `int` | Número de página (basado en 1) |
| `pageSize` | `int` | Cantidad de registros por página |
| `search` | `string` | Término de búsqueda (puede ser `null` o vacío) |

---

### Implementación Genérica

En `Infrastruture/Repositories/GenericRepository.cs` se implementa el método base que no aplica filtrado (debe ser sobrescrito):

**Ubicación:** `Infrastruture/Repositories/GenericRepository.cs`

```23:32:Infrastruture/Repositories/GenericRepository.cs
public virtual async Task<(int totalRegistros, IEnumerable<T> registros)> GetAllAsync(int pageIndex, int pageSize, string search)
{
    var totalRegistros = await _context.Set<T>().CountAsync();
    var registros = await _context.Set<T>()
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (totalRegistros, registros);
}
```

> ⚠️ **Nota:** La implementación genérica no aplica filtrado de búsqueda. Cada repositorio específico debe sobrescribir este método para implementar su lógica de búsqueda personalizada.

---

### Implementación Específica en `ProductoRepository`

El repositorio de productos sobrescribe el método para implementar la búsqueda por nombre del producto:

**Ubicación:** `Infrastruture/Repositories/ProductoRepository.cs`

```44:62:Infrastruture/Repositories/ProductoRepository.cs
public override async Task<(int totalRegistros, IEnumerable<Producto> registros)> GetAllAsync(int pageIndex, int pageSize, string search)
{
    var consulta = _context.Productos as IQueryable<Producto>;

    if (!String.IsNullOrEmpty(search))
    {
        consulta = consulta.Where(p => p.Nombre.ToLower().Contains(search));
    }

    var totalRegistros = await consulta.CountAsync();

    var registros = await consulta
        .Include(u => u.Marca)
        .Include(u => u.Categoria)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    return (totalRegistros, registros);
}
```

#### 🔍 Lógica de Búsqueda

| Paso | Descripción |
|------|-------------|
| **1. Crear consulta** | Se crea una consulta `IQueryable<Producto>` desde el contexto |
| **2. Aplicar filtro** | Si `search` no está vacío, se filtra por `Nombre.Contains(search)` |
| **3. Contar totales** | Se cuenta el total de registros **después** del filtro |
| **4. Incluir relaciones** | Se cargan las relaciones (`Marca` y `Categoria`) |
| **5. Aplicar paginación** | Se aplican `Skip()` y `Take()` para la paginación |
| **6. Ejecutar consulta** | Se ejecuta la consulta y se retornan los resultados |

#### 🎯 Campo de Búsqueda

En la implementación actual, la búsqueda se realiza sobre el campo **`Nombre`** del producto:

```csharp
consulta = consulta.Where(p => p.Nombre.ToLower().Contains(search));
```

**Ejemplos de coincidencias:**
- Búsqueda: `"lap"` → Encuentra: "Laptop", "Lapicera", "Lápiz"
- Búsqueda: `"top"` → Encuentra: "Laptop", "Desktop"
- Búsqueda: `"pro"` → Encuentra: "Producto", "Procesador"

---

## 🎮 Uso en Controladores

El controlador `ProductoController` utiliza la búsqueda junto con la paginación en el endpoint `GET`:

**Ubicación:** `API/Controllers/ProductoController.cs`

```28:34:API/Controllers/ProductoController.cs
public async Task<ActionResult<Pager<ProductoListDTO>>> Get([FromQuery] Params productParams)
{
    var resultado = await _unitOfWork.Productos.GetAllAsync(productParams.PageIndex, productParams.PageSize, productParams.Search);

    var listaProductosDTO = _mapper.Map<List<ProductoListDTO>>(resultado.registros);
    return Ok(new Pager<ProductoListDTO>(listaProductosDTO, resultado.totalRegistros, productParams.PageIndex, productParams.PageSize, productParams.Search));
}
```

### 🔄 Flujo de Ejecución

```mermaid
graph LR
    A[Cliente] -->|Query: Search=laptop| B[Controlador]
    B -->|Normaliza: laptop| C[Repositorio]
    C -->|Filtra por Nombre| D[Consulta SQL]
    D -->|Resultados filtrados| E[Paginación]
    E -->|DTOs + Search| F[Pager]
    F -->|JSON| A
```

1. 📥 Recibe el parámetro `Search` desde la query string
2. 🔄 La clase `Params` normaliza el texto a minúsculas
3. 🔍 El repositorio filtra los productos por nombre usando `Contains`
4. 📄 Se aplica la paginación sobre los resultados filtrados
5. 🔄 Se mapean los resultados a DTOs usando AutoMapper
6. 📤 Se retorna un objeto `Pager<ProductoListDTO>` con los resultados filtrados y paginados

---

## 💻 Ejemplos de Uso

### 📌 Ejemplo 1: Búsqueda Básica

**Request:**
```http
GET /api/Producto?Search=laptop
```

**Response:**
```json
{
  "pageIndex": 1,
  "pageSize": 5,
  "total": 3,
  "search": "laptop",
  "registers": [
    {
      "id": 1,
      "nombre": "Laptop Dell XPS",
      "precio": 1200.00,
      "marca": "Dell",
      "categoria": "Electrónica"
    },
    {
      "id": 5,
      "nombre": "Laptop HP Pavilion",
      "precio": 800.00,
      "marca": "HP",
      "categoria": "Electrónica"
    },
    {
      "id": 12,
      "nombre": "Laptop Gaming ASUS",
      "precio": 1500.00,
      "marca": "ASUS",
      "categoria": "Electrónica"
    }
  ],
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

### 📌 Ejemplo 2: Búsqueda con Paginación

**Request:**
```http
GET /api/Producto?Search=pro&PageIndex=1&PageSize=2
```

**Response:**
```json
{
  "pageIndex": 1,
  "pageSize": 2,
  "total": 5,
  "search": "pro",
  "registers": [
    {
      "id": 2,
      "nombre": "Producto A",
      "precio": 100.00,
      "marca": "Marca X",
      "categoria": "Categoría Y"
    },
    {
      "id": 8,
      "nombre": "Procesador Intel",
      "precio": 300.00,
      "marca": "Intel",
      "categoria": "Hardware"
    }
  ],
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### 📌 Ejemplo 3: Búsqueda Sin Resultados

**Request:**
```http
GET /api/Producto?Search=xyz123
```

**Response:**
```json
{
  "pageIndex": 1,
  "pageSize": 5,
  "total": 0,
  "search": "xyz123",
  "registers": [],
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

### 📌 Ejemplo 4: Búsqueda Case-Insensitive

**Request:**
```http
GET /api/Producto?Search=LAPTOP
```

**Comportamiento:**
- El texto se normaliza a `"laptop"` automáticamente
- La búsqueda encuentra productos con nombre "Laptop", "LAPTOP", "laptop", etc.

**Response:**
```json
{
  "pageIndex": 1,
  "pageSize": 5,
  "total": 3,
  "search": "laptop",
  "registers": [
    // ... productos encontrados
  ],
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

---

### 📌 Ejemplo 5: Búsqueda Vacía (Sin Filtro)

**Request:**
```http
GET /api/Producto?Search=
```

**Comportamiento:**
- Si `Search` está vacío o no se proporciona, se retornan todos los productos
- La paginación se aplica normalmente sobre todos los registros

**Response:**
```json
{
  "pageIndex": 1,
  "pageSize": 5,
  "total": 25,
  "search": "",
  "registers": [
    // ... todos los productos (paginados)
  ],
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## 📋 Estructura de Respuesta JSON

```json
{
  "pageIndex": 1,           // Número de página actual
  "pageSize": 5,            // Registros por página
  "total": 10,              // Total de registros que coinciden con la búsqueda
  "search": "laptop",       // Término de búsqueda utilizado (normalizado)
  "registers": [            // Array de objetos filtrados
    {
      "id": 1,
      "nombre": "Laptop Dell",
      // ... más propiedades
    }
  ],
  "totalPages": 2,          // Total de páginas (calculado sobre resultados filtrados)
  "hasPreviousPage": false, // ¿Existe página anterior?
  "hasNextPage": true       // ¿Existe página siguiente?
}
```

### 📊 Diagrama de Estructura

```
Pager<T>
├── PageIndex (int)
├── PageSize (int)
├── Total (int) [filtrado]
├── Search (string) [nuevo]
├── Registers (IEnumerable<T>) [filtrados]
├── TotalPages (int) [calculado sobre filtrados]
├── HasPreviousPage (bool)
└── HasNextPage (bool)
```

---

## 🎯 Características de Búsqueda

### 🔤 Tipo de Búsqueda

| Característica | Descripción |
|---------------|-------------|
| **Búsqueda Parcial** | Usa `Contains()` para encontrar coincidencias parciales |
| **Case-Insensitive** | No distingue entre mayúsculas y minúsculas |
| **Normalización** | Convierte automáticamente a minúsculas |
| **Búsqueda en Campo Específico** | Actualmente busca solo en el campo `Nombre` |

### 📝 Ejemplos de Coincidencias

| Término de Búsqueda | Coincide con | No Coincide con |
|---------------------|--------------|-----------------|
| `"lap"` | "Laptop", "Lapicera" | "Mouse", "Teclado" |
| `"top"` | "Laptop", "Desktop" | "Monitor", "Impresora" |
| `"pro"` | "Producto", "Procesador" | "Mouse", "Teclado" |
| `"xyz"` | Ninguno | Todos los productos |

### ⚡ Rendimiento

| Aspecto | Detalle |
|---------|---------|
| **Consulta SQL** | Se genera un `WHERE` con `LIKE` en la base de datos |
| **Índices** | Se recomienda tener un índice en la columna `Nombre` para mejor rendimiento |
| **Filtrado** | El filtrado ocurre **antes** de la paginación, optimizando la consulta |

---

## ✨ Ventajas de la Implementación

| Ventaja | Descripción |
|---------|-------------|
| 🔍 **Búsqueda Intuitiva** | Los usuarios pueden buscar fácilmente por texto |
| 🚀 **Rendimiento Optimizado** | El filtrado ocurre a nivel de base de datos |
| 🔄 **Integración Perfecta** | Funciona sin problemas con el sistema de paginación |
| 📊 **Metadatos Completos** | El término de búsqueda se incluye en la respuesta |
| 🎯 **Búsqueda Flexible** | Coincidencias parciales permiten encontrar resultados fácilmente |
| ♻️ **Extensible** | Cada repositorio puede implementar su propia lógica de búsqueda |

---

## 🔨 Extensibilidad

### 📝 Pasos para Implementar Búsqueda en Otros Repositorios

1. **Sobrescribir el método en el repositorio específico**

```csharp
public override async Task<(int totalRegistros, IEnumerable<MiEntidad> registros)> 
    GetAllAsync(int pageIndex, int pageSize, string search)
{
    var consulta = _context.MiEntidades as IQueryable<MiEntidad>;

    if (!String.IsNullOrEmpty(search))
    {
        // Implementar lógica de búsqueda personalizada
        consulta = consulta.Where(e => 
            e.Campo1.ToLower().Contains(search) ||
            e.Campo2.ToLower().Contains(search)
        );
    }

    var totalRegistros = await consulta.CountAsync();

    var registros = await consulta
        .Include(e => e.Relacion) // Si es necesario
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return (totalRegistros, registros);
}
```

2. **Búsqueda en Múltiples Campos**

```csharp
if (!String.IsNullOrEmpty(search))
{
    consulta = consulta.Where(p => 
        p.Nombre.ToLower().Contains(search) ||
        p.Descripcion.ToLower().Contains(search) ||
        p.Marca.Nombre.ToLower().Contains(search)
    );
}
```

3. **Búsqueda con Ordenamiento**

```csharp
var registros = await consulta
    .OrderBy(p => p.Nombre) // Ordenar por relevancia o alfabéticamente
    .Include(u => u.Marca)
    .Skip((pageIndex - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

## 🔬 Notas Técnicas

### Implementación Interna

- **Entity Framework Core**: Utiliza `Where()` con `Contains()` para generar consultas SQL con `LIKE`
- **Case-Insensitive**: La normalización a minúsculas asegura búsquedas sin distinción de mayúsculas/minúsculas
- **IQueryable**: Se usa `IQueryable` para construir consultas de forma eficiente antes de ejecutarlas
- **Filtrado Antes de Paginación**: El filtrado ocurre antes de aplicar `Skip()` y `Take()`, optimizando el rendimiento

### ⚡ Consideraciones de Rendimiento

| Aspecto | Recomendación |
|---------|---------------|
| **Índices** | Crear índices en las columnas utilizadas para búsqueda |
| **Búsqueda Completa** | Para búsquedas más complejas, considerar Full-Text Search |
| **Límite de Caracteres** | Considerar limitar la longitud del término de búsqueda |
| **Caché** | Para búsquedas frecuentes, considerar implementar caché |

### 🎯 Buenas Prácticas

- ✅ Normalizar siempre el texto de búsqueda antes de comparar
- ✅ Usar `IQueryable` para construir consultas de forma eficiente
- ✅ Aplicar el filtro **antes** de contar y paginar
- ✅ Incluir relaciones necesarias después del filtrado
- ✅ Considerar búsqueda en múltiples campos para mejor experiencia de usuario
- ✅ Validar y sanitizar el término de búsqueda para prevenir SQL injection (Entity Framework lo hace automáticamente)

### 🔒 Seguridad

- ✅ **Entity Framework Core** previene automáticamente SQL injection mediante parámetros
- ✅ La normalización a minúsculas ayuda a prevenir algunos ataques de inyección
- ✅ Considerar limitar la longitud del término de búsqueda para prevenir DoS

---

## 📚 Referencias

- **Parámetros:** `API/Helpers/Params.cs`
- **Pager:** `API/Helpers/Pager.cs`
- **Controlador de Ejemplo:** `API/Controllers/ProductoController.cs`
- **Repositorio Genérico:** `Infrastruture/Repositories/GenericRepository.cs`
- **Repositorio Específico:** `Infrastruture/Repositories/ProductoRepository.cs`
- **Interfaz:** `Core/Interfaces/IGenericRepository.cs`

---

## 🔗 Integración con Paginación

El sistema de búsqueda está completamente integrado con el sistema de paginación. Ambos sistemas trabajan juntos:

1. **Búsqueda primero**: Se filtran los registros según el término de búsqueda
2. **Paginación después**: Se aplica la paginación sobre los resultados filtrados
3. **Metadatos combinados**: La respuesta incluye información tanto de búsqueda como de paginación

**Ejemplo combinado:**
```http
GET /api/Producto?Search=laptop&PageIndex=2&PageSize=10
```

Esto buscará productos que contengan "laptop" y retornará la página 2 con 10 resultados por página.

---

<div align="center">

**Documentación generada para el Sistema de Búsqueda** 🔍

</div>


