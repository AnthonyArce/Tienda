# 📚 Documentación del Proyecto Tienda

Bienvenido a la documentación del proyecto Tienda. Esta carpeta contiene toda la documentación técnica de las funcionalidades implementadas en la API.

---

## 📑 Índice de Documentación

### 1. 📄 [Sistema de Paginación](./DOCUMENTACION_PAGINACION.md)
Documentación completa sobre el sistema de paginación implementado en la API. Incluye:
- Clase `Pager<T>` y sus propiedades
- Clase `Params` para parámetros de paginación
- Implementación en repositorios
- Ejemplos de uso y casos especiales

### 2. 🔍 [Sistema de Búsqueda](./DOCUMENTACION_BUSQUEDA.md)
Documentación sobre el sistema de búsqueda integrado con paginación. Incluye:
- Propiedad `Search` en `Params` y `Pager<T>`
- Implementación de búsqueda en repositorios
- Búsqueda case-insensitive y normalización automática
- Ejemplos de uso con diferentes escenarios

### 3. 📄 [Soporte de Formato XML](./DOCUMENTACION_XML.md)
Documentación sobre el soporte de respuestas en formato XML. Incluye:
- Configuración de formateadores XML
- Atributos `[DataContract]` y `[DataMember]`
- Negociación de contenido mediante header `Accept`
- Comparación entre JSON y XML
- Ejemplos de respuestas en ambos formatos

### 4. 🔐 [Sistema de Autenticación y Autorización](./DOCUMENTACION_AUTENTICACION_AUTORIZACION.md)
Documentación completa sobre el sistema de autenticación JWT y autorización basada en roles. Incluye:
- Configuración JWT y tokens
- Registro y login de usuarios
- Sistema de roles (Administrador, Gerente, Empleado)
- Autorización en controladores con `[Authorize]`
- Hash de contraseñas y seguridad
- Ejemplos completos de uso

---

## 🚀 Inicio Rápido

### Para Desarrolladores Nuevos

1. Comienza leyendo la [documentación de autenticación](./DOCUMENTACION_AUTENTICACION_AUTORIZACION.md) para entender cómo funciona el sistema de seguridad
2. Revisa la [documentación de paginación](./DOCUMENTACION_PAGINACION.md) para entender cómo se manejan los datos paginados
3. Consulta la [documentación de búsqueda](./DOCUMENTACION_BUSQUEDA.md) para entender cómo filtrar resultados
4. Revisa la [documentación de XML](./DOCUMENTACION_XML.md) si necesitas trabajar con respuestas en formato XML

### Para Integración con la API

- **Autenticación**: Registra usuarios con `POST /api/Usuario/register` y obtén tokens con `POST /api/Usuario/token`
- **Autorización**: Usa el header `Authorization: Bearer {token}` en solicitudes protegidas
- **Paginación**: Usa los parámetros `PageIndex` y `PageSize` en la query string
- **Búsqueda**: Usa el parámetro `Search` en la query string
- **Formato**: Especifica el formato deseado mediante el header `Accept: application/json` o `Accept: application/xml`

---

## 📋 Estructura de Archivos

```
documentacion/
├── README.md                              # Este archivo
├── DOCUMENTACION_PAGINACION.md            # Documentación de paginación
├── DOCUMENTACION_BUSQUEDA.md              # Documentación de búsqueda
├── DOCUMENTACION_XML.md                   # Documentación de soporte XML
└── DOCUMENTACION_AUTENTICACION_AUTORIZACION.md  # Documentación de autenticación y autorización
```

---

## 🔗 Enlaces Útiles

- **Repositorio del Proyecto**: [Ver código fuente](../)
- **API Controllers**: `API/Controllers/`
- **Helpers**: `API/Helpers/`
- **Repositorios**: `Infrastruture/Repositories/`

---

## 📝 Notas

- Todas las documentaciones están escritas en español
- Los ejemplos incluyen requests HTTP y responses completas
- Las referencias a código incluyen rutas de archivos y números de línea
- Cada documentación incluye una tabla de contenidos navegable

---

<div align="center">

**Última actualización:** Enero 2025

</div>


