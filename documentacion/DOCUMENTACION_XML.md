# 📄 Soporte de Formato XML

> **Sistema completo de soporte para respuestas en formato XML que permite a los clientes solicitar datos en XML además de JSON, mejorando la compatibilidad con sistemas legacy y aplicaciones que requieren este formato.**

---

## 📑 Tabla de Contenidos

- [Descripción General](#-descripción-general)
- [Configuración](#-configuración)
  - [Configuración en Program.cs](#configuración-en-programcs)
  - [Opciones de Formato](#opciones-de-formato)
- [Implementación](#-implementación)
  - [Atributos DataContract y DataMember](#atributos-datacontract-y-datamember)
  - [Clase Pager con Soporte XML](#clase-pager-con-soporte-xml)
  - [DTOs con Soporte XML](#dtos-con-soporte-xml)
  - [Entidades con XmlIgnore](#entidades-con-xmlignore)
- [Uso en la API](#-uso-en-la-api)
- [Ejemplos de Uso](#-ejemplos-de-uso)
- [Estructura de Respuesta XML](#-estructura-de-respuesta-xml)
- [Comparación JSON vs XML](#-comparación-json-vs-xml)
- [Ventajas](#-ventajas-del-soporte-xml)
- [Notas Técnicas](#-notas-técnicas)

---

## 🎯 Descripción General

Se ha implementado soporte completo para respuestas en formato XML en la API, permitiendo que los clientes soliciten datos tanto en JSON (formato por defecto) como en XML mediante el header `Accept` de HTTP. Esta funcionalidad mejora la compatibilidad con sistemas legacy y aplicaciones que requieren formato XML.

### ✨ Características Principales

- ✅ **Soporte dual**: JSON (por defecto) y XML
- ✅ **Negociación de contenido**: Respeta el header `Accept` del cliente
- ✅ **Serialización controlada**: Uso de atributos `[DataMember]` para control preciso
- ✅ **Prevención de referencias circulares**: Uso de `[XmlIgnore]` en relaciones
- ✅ **Orden de propiedades**: Control del orden de elementos en XML

---

## ⚙️ Configuración

### Configuración en Program.cs

La configuración del soporte XML se realiza en el archivo `Program.cs`:

**Ubicación:** `API/Program.cs`

```16:20:API/Program.cs
builder.Services.AddControllers(options =>
{
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = false; //Cuando esta activa al enviarle atraves de Accept un formato que el servidor no soporte devuelve un 406, por defecto json
}).AddXmlDataContractSerializerFormatters();
```

#### 🔍 Explicación de la Configuración

| Opción | Valor | Descripción |
|--------|-------|-------------|
| `RespectBrowserAcceptHeader` | `true` | Respeta el header `Accept` enviado por el cliente para negociar el formato |
| `ReturnHttpNotAcceptable` | `false` | Si el formato solicitado no está disponible, retorna JSON en lugar de error 406 |
| `AddXmlDataContractSerializerFormatters()` | - | Agrega el formateador XML usando `DataContractSerializer` |

### Opciones de Formato

#### 📋 Headers HTTP Aceptados

| Header | Valor | Formato Retornado |
|--------|-------|-------------------|
| `Accept: application/json` | `application/json` | JSON (por defecto) |
| `Accept: application/xml` | `application/xml` | XML |
| `Accept: text/xml` | `text/xml` | XML |
| Sin header `Accept` | - | JSON (por defecto) |

---

## 🔧 Implementación

### Atributos DataContract y DataMember

Para que las clases puedan ser serializadas correctamente a XML, se utilizan los siguientes atributos:

#### 📦 Namespace Requerido

```csharp
using System.Runtime.Serialization;
```

#### 🏷️ Atributos Disponibles

| Atributo | Uso | Descripción |
|----------|-----|-------------|
| `[DataContract]` | Clase | Marca la clase como serializable para XML |
| `[DataMember]` | Propiedad | Marca la propiedad para incluirla en la serialización XML |
| `[DataMember(Order = n)]` | Propiedad | Especifica el orden de la propiedad en el XML |
| `[IgnoreDataMember]` | Propiedad | Excluye la propiedad de la serialización XML |

---

### Clase Pager con Soporte XML

La clase `Pager<T>` ha sido configurada con atributos para soporte XML completo:

**Ubicación:** `API/Helpers/Pager.cs`

```5:47:API/Helpers/Pager.cs
[DataContract]
public class Pager<T> where T : class
{
    [DataMember(Order = 1)]
    public int PageIndex { get; private set; }
    [DataMember(Order = 2)]
    public int PageSize { get; private set; }
    [DataMember(Order = 3)]
    public int Total { get; private set; }
    [DataMember(Order = 4)]
    public string Search { get; private set; }
    [DataMember(Order = 5)]
    public IEnumerable<T> Registers { get; private set; }

        public Pager(IEnumerable<T> registers, int total, int pageIndex, int pageSize, string search)
        {
            Registers = registers;
            Total = total;
            PageIndex = pageIndex;
            PageSize = pageSize;
            Search = search;
    }
    [DataMember(Order = 6)]
    public int TotalPages
        {
        get
        {
            return (int)Math.Ceiling((double)Total / PageSize);
        }
        private set { }
           
        }
    [IgnoreDataMember]
    public bool HasPreviousPage
        {
            get { return PageIndex > 1; }
        }
    [IgnoreDataMember]
    public bool HasNextPage
        {
            get { return (PageIndex < TotalPages); }
        }

}
```

#### 📊 Propiedades y Atributos

| Propiedad | Atributo | Orden | Incluida en XML |
|-----------|----------|-------|-----------------|
| `PageIndex` | `[DataMember(Order = 1)]` | 1 | ✅ Sí |
| `PageSize` | `[DataMember(Order = 2)]` | 2 | ✅ Sí |
| `Total` | `[DataMember(Order = 3)]` | 3 | ✅ Sí |
| `Search` | `[DataMember(Order = 4)]` | 4 | ✅ Sí |
| `Registers` | `[DataMember(Order = 5)]` | 5 | ✅ Sí |
| `TotalPages` | `[DataMember(Order = 6)]` | 6 | ✅ Sí |
| `HasPreviousPage` | `[IgnoreDataMember]` | - | ❌ No |
| `HasNextPage` | `[IgnoreDataMember]` | - | ❌ No |

> 💡 **Nota:** Las propiedades `HasPreviousPage` y `HasNextPage` están marcadas con `[IgnoreDataMember]` porque son propiedades calculadas que pueden derivarse de `PageIndex` y `TotalPages`.

---

### DTOs con Soporte XML

Los DTOs también están configurados con atributos `[DataMember]`:

**Ubicación:** `API/DTO/ProductoListDTO.cs`

```5:21:API/DTO/ProductoListDTO.cs
public class ProductoListDTO
{
    [DataMember]
    public int Id { get; set; }
    [DataMember]
    public string Nombre { get; set; }
    [DataMember]
    public decimal Precio { get; set; }
    [DataMember]
    public int MarcaId { get; set; }
    [DataMember]
    public string Marca { get; set; }
    [DataMember]
    public int CategoriaId { get; set; }
    [DataMember]
    public string Categoria { get; set; }
}
```

#### 📋 Propiedades Serializadas

Todas las propiedades del DTO están marcadas con `[DataMember]`, lo que significa que todas se incluirán en la respuesta XML.

---

### Entidades con XmlIgnore

Las entidades `Marca` y `Categoria` utilizan `[XmlIgnore]` para evitar referencias circulares:

**Ubicación:** `Core/Entities/Marca.cs`

```11:16:Core/Entities/Marca.cs
public class Marca: BaseEntity
{       
    public string Nombre { get; set; }
    [XmlIgnore]
    public ICollection<Producto> productos { get; set; }
}
```

**Ubicación:** `Core/Entities/Categoria.cs`

```10:15:Core/Entities/Categoria.cs
public class Categoria: BaseEntity
{        
    public string Nombre { get; set; }
    [XmlIgnore]
    public ICollection<Producto> productos { get; set; }
}
```

#### 🔄 Prevención de Referencias Circulares

| Entidad | Propiedad Ignorada | Razón |
|---------|-------------------|-------|
| `Marca` | `productos` | Evita referencia circular (Marca → Productos → Marca) |
| `Categoria` | `productos` | Evita referencia circular (Categoria → Productos → Categoria) |

> ⚠️ **Importante:** Las relaciones de navegación (`ICollection<Producto>`) se ignoran en la serialización XML para prevenir referencias circulares infinitas.

---

## 🎮 Uso en la API

### Solicitud con Header Accept

Los clientes pueden solicitar respuestas en XML usando el header `Accept`:

```http
GET /api/Producto?PageIndex=1&PageSize=5
Accept: application/xml
```

### Comportamiento por Defecto

Si no se especifica el header `Accept`, la API retorna JSON por defecto:

```http
GET /api/Producto?PageIndex=1&PageSize=5
```

---

## 💻 Ejemplos de Uso

### 📌 Ejemplo 1: Solicitud XML Básica

**Request:**
```http
GET /api/Producto?PageIndex=1&PageSize=2
Accept: application/xml
```

**Response (XML):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<PagerOfProductoListDTO xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/API.Helpers">
    <PageIndex>1</PageIndex>
    <PageSize>2</PageSize>
    <Registers>
        <ProductoListDTO>
            <Categoria>Electrónica</Categoria>
            <CategoriaId>1</CategoriaId>
            <Id>1</Id>
            <Marca>Dell</Marca>
            <MarcaId>1</MarcaId>
            <Nombre>Laptop Dell XPS</Nombre>
            <Precio>1200.00</Precio>
        </ProductoListDTO>
        <ProductoListDTO>
            <Categoria>Electrónica</Categoria>
            <CategoriaId>1</CategoriaId>
            <Id>2</Id>
            <Marca>HP</Marca>
            <MarcaId>2</MarcaId>
            <Nombre>Laptop HP Pavilion</Nombre>
            <Precio>800.00</Precio>
        </ProductoListDTO>
    </Registers>
    <Search i:nil="true" />
    <Total>25</Total>
    <TotalPages>13</TotalPages>
</PagerOfProductoListDTO>
```

---

### 📌 Ejemplo 2: Solicitud XML con Búsqueda

**Request:**
```http
GET /api/Producto?Search=laptop&PageIndex=1&PageSize=2
Accept: application/xml
```

**Response (XML):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<PagerOfProductoListDTO xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/API.Helpers">
    <PageIndex>1</PageIndex>
    <PageSize>2</PageSize>
    <Registers>
        <ProductoListDTO>
            <Categoria>Electrónica</Categoria>
            <CategoriaId>1</CategoriaId>
            <Id>1</Id>
            <Marca>Dell</Marca>
            <MarcaId>1</MarcaId>
            <Nombre>Laptop Dell XPS</Nombre>
            <Precio>1200.00</Precio>
        </ProductoListDTO>
        <ProductoListDTO>
            <Categoria>Electrónica</Categoria>
            <CategoriaId>1</CategoriaId>
            <Id>5</Id>
            <Marca>HP</Marca>
            <MarcaId>2</MarcaId>
            <Nombre>Laptop HP Pavilion</Nombre>
            <Precio>800.00</Precio>
        </ProductoListDTO>
    </Registers>
    <Search>laptop</Search>
    <Total>3</Total>
    <TotalPages>2</TotalPages>
</PagerOfProductoListDTO>
```

---

### 📌 Ejemplo 3: Comparación JSON vs XML

#### Solicitud JSON
```http
GET /api/Producto?PageIndex=1&PageSize=1
Accept: application/json
```

**Response (JSON):**
```json
{
  "pageIndex": 1,
  "pageSize": 1,
  "total": 25,
  "search": null,
  "registers": [
    {
      "id": 1,
      "nombre": "Laptop Dell XPS",
      "precio": 1200.00,
      "marcaId": 1,
      "marca": "Dell",
      "categoriaId": 1,
      "categoria": "Electrónica"
    }
  ],
  "totalPages": 25,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Solicitud XML
```http
GET /api/Producto?PageIndex=1&PageSize=1
Accept: application/xml
```

**Response (XML):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<PagerOfProductoListDTO xmlns:i="http://www.w3.org/2001/XMLSchema-instance" xmlns="http://schemas.datacontract.org/2004/07/API.Helpers">
    <PageIndex>1</PageIndex>
    <PageSize>1</PageSize>
    <Registers>
        <ProductoListDTO>
            <Categoria>Electrónica</Categoria>
            <CategoriaId>1</CategoriaId>
            <Id>1</Id>
            <Marca>Dell</Marca>
            <MarcaId>1</MarcaId>
            <Nombre>Laptop Dell XPS</Nombre>
            <Precio>1200.00</Precio>
        </ProductoListDTO>
    </Registers>
    <Search i:nil="true" />
    <Total>25</Total>
    <TotalPages>25</TotalPages>
</PagerOfProductoListDTO>
```

---

## 📋 Estructura de Respuesta XML

### Estructura General

```xml
<?xml version="1.0" encoding="utf-8"?>
<PagerOf[DTOType] xmlns:i="http://www.w3.org/2001/XMLSchema-instance" 
                   xmlns="http://schemas.datacontract.org/2004/07/API.Helpers">
    <PageIndex>int</PageIndex>
    <PageSize>int</PageSize>
    <Registers>
        <[DTOType]>
            <!-- Propiedades del DTO -->
        </[DTOType]>
        <!-- Más elementos -->
    </Registers>
    <Search>string o i:nil="true"</Search>
    <Total>int</Total>
    <TotalPages>int</TotalPages>
</PagerOf[DTOType]>
```

### 📊 Elementos XML

| Elemento | Tipo | Descripción |
|----------|------|-------------|
| `PageIndex` | `int` | Número de página actual |
| `PageSize` | `int` | Registros por página |
| `Registers` | `Array` | Contenedor de elementos DTO |
| `Search` | `string` o `nil` | Término de búsqueda (puede ser nulo) |
| `Total` | `int` | Total de registros |
| `TotalPages` | `int` | Total de páginas |

### 🔍 Namespaces XML

El XML generado incluye namespaces estándar:

- `xmlns:i="http://www.w3.org/2001/XMLSchema-instance"` - Para atributos como `i:nil`
- `xmlns="http://schemas.datacontract.org/2004/07/API.Helpers"` - Namespace de la clase

---

## 🔄 Comparación JSON vs XML

### 📊 Tabla Comparativa

| Característica | JSON | XML |
|----------------|------|-----|
| **Tamaño** | Más compacto | Más verboso |
| **Legibilidad** | Fácil de leer | Más estructurado |
| **Parsing** | Más rápido | Más lento |
| **Compatibilidad** | Moderno | Legacy y sistemas empresariales |
| **Soporte de Tipos** | Limitado | Más completo |
| **Validación** | JSON Schema | XML Schema (XSD) |

### 🎯 Cuándo Usar Cada Formato

| Formato | Cuándo Usar |
|---------|-------------|
| **JSON** | Aplicaciones web modernas, APIs REST, aplicaciones móviles |
| **XML** | Sistemas legacy, integraciones empresariales, SOAP, aplicaciones que requieren validación estricta |

---

## ✨ Ventajas del Soporte XML

| Ventaja | Descripción |
|---------|-------------|
| 🔄 **Compatibilidad** | Permite integrar con sistemas legacy que requieren XML |
| 🎯 **Flexibilidad** | Los clientes pueden elegir el formato que prefieren |
| 🏢 **Empresarial** | Cumple con estándares empresariales que requieren XML |
| 🔒 **Validación** | Permite validación estricta con XML Schema |
| 📋 **Estructura** | Formato más estructurado y autocontenido |
| 🌐 **Estándar** | Formato ampliamente aceptado en integraciones empresariales |

---

## 🔬 Notas Técnicas

### Implementación Interna

- **DataContractSerializer**: Utiliza `DataContractSerializer` de .NET para la serialización XML
- **Negociación de Contenido**: ASP.NET Core negocia automáticamente el formato basado en el header `Accept`
- **Orden de Propiedades**: El atributo `Order` en `[DataMember]` controla el orden de los elementos en XML
- **Namespaces**: Los namespaces se generan automáticamente basados en el namespace de la clase

### ⚡ Consideraciones de Rendimiento

| Aspecto | Detalle |
|---------|---------|
| **Serialización** | XML es más lento que JSON debido a su verbosidad |
| **Tamaño** | XML ocupa aproximadamente 2-3 veces más espacio que JSON |
| **Parsing** | El parsing de XML es más costoso computacionalmente |
| **Memoria** | XML requiere más memoria para procesar |

### 🎯 Buenas Prácticas

- ✅ Usar `[DataMember(Order = n)]` para controlar el orden de elementos
- ✅ Marcar propiedades calculadas con `[IgnoreDataMember]` si no son necesarias
- ✅ Usar `[XmlIgnore]` en relaciones de navegación para evitar referencias circulares
- ✅ Documentar qué formato usar según el caso de uso
- ✅ Considerar el impacto en rendimiento al elegir XML sobre JSON

### 🔒 Seguridad

- ✅ **Validación**: XML puede ser validado con XSD para mayor seguridad
- ✅ **XXE Attacks**: `DataContractSerializer` es más seguro contra ataques XXE que `XmlSerializer`
- ✅ **Sanitización**: Los datos se serializan automáticamente, reduciendo riesgos de inyección

### 📝 Diferencias con XmlSerializer

| Característica | DataContractSerializer | XmlSerializer |
|----------------|------------------------|---------------|
| **Atributos** | `[DataContract]`, `[DataMember]` | `[XmlRoot]`, `[XmlElement]` |
| **Rendimiento** | Más rápido | Más lento |
| **Control** | Menos control sobre estructura | Más control sobre estructura |
| **Recomendado** | ✅ Para APIs REST | Para casos específicos |

---

## 📚 Referencias

- **Configuración:** `API/Program.cs`
- **Pager:** `API/Helpers/Pager.cs`
- **DTOs:** `API/DTO/ProductoListDTO.cs`
- **Entidades:** `Core/Entities/Marca.cs`, `Core/Entities/Categoria.cs`
- **Documentación Microsoft:** [Content Negotiation in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/web-api/advanced/formatting)

---

## 🔗 Integración con Otros Sistemas

El soporte XML permite:

1. **Integración con SOAP**: Los servicios SOAP requieren XML
2. **Sistemas Legacy**: Muchos sistemas empresariales antiguos solo aceptan XML
3. **Validación Estricta**: XML Schema permite validación más estricta que JSON Schema
4. **Transformación**: XSLT permite transformar XML fácilmente
5. **Integraciones Empresariales**: EDI y otros formatos empresariales usan XML

---

<div align="center">

**Documentación generada para el Soporte de Formato XML** 📄

</div>


