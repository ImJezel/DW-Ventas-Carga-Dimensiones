# DW de Ventas: carga de dimensiones

Worker Service en .NET 9 que carga las dimensiones del data warehouse `DW_Ventas_Jezel` (SQL Server) desde los archivos de origen `clients.csv` y `products.csv`.

| Dimensión | Origen | Estrategia |
|---|---|---|
| DimFecha | Generada (2024-01-01 a 2026-12-31) | Inserta solo fechas nuevas |
| DimCliente | `Datos/clients.csv` | Limpieza + staging + MERGE (SCD tipo 1) |
| DimCategoria | Valores distintos de `products.csv` | MERGE |
| DimProducto | `Datos/products.csv` | Staging + lookup de categoría + MERGE (SCD tipo 1) |
| DimMetodoPago | Valores fijos del modelo transaccional | MERGE |

Cada ejecución queda registrada en `dw.BitacoraCarga`. La carga es idempotente: si se ejecuta dos veces, la segunda no inserta duplicados.

## Ejecutar

```powershell
sqlcmd -S .\SQLEXPRESS -E -i sql\01_crear_dw.sql
cd src\VentasDW.Carga
dotnet run
sqlcmd -S .\SQLEXPRESS -E -i ..\..\sql\02_verificacion.sql
```

**Autor:** Jezel Pujols (2025-2067), Electiva 1, 2026-C-3.
