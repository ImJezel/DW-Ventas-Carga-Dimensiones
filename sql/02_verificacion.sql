USE DW_Ventas_Jezel;
GO
-- Filas por dimensión
SELECT 'DimFecha' AS Dimension, COUNT(*) AS Filas FROM dw.DimFecha
UNION ALL SELECT 'DimCliente',    COUNT(*) FROM dw.DimCliente
UNION ALL SELECT 'DimCategoria',  COUNT(*) FROM dw.DimCategoria
UNION ALL SELECT 'DimProducto',   COUNT(*) FROM dw.DimProducto
UNION ALL SELECT 'DimMetodoPago', COUNT(*) FROM dw.DimMetodoPago;

-- Muestra de cada dimensión
SELECT TOP 10 * FROM dw.DimCliente  ORDER BY ClienteKey;
SELECT TOP 10 p.ProductoKey, p.IdProductoOrigen, p.Nombre, c.Nombre AS Categoria
FROM dw.DimProducto p JOIN dw.DimCategoria c ON c.CategoriaKey = p.CategoriaKey ORDER BY p.ProductoKey;
SELECT * FROM dw.DimCategoria ORDER BY CategoriaKey;
SELECT * FROM dw.DimMetodoPago;
SELECT TOP 10 * FROM dw.DimFecha WHERE FechaKey > 0 ORDER BY FechaKey;

-- Productos por categoría
SELECT c.Nombre AS Categoria, COUNT(*) AS Productos
FROM dw.DimProducto p JOIN dw.DimCategoria c ON c.CategoriaKey = p.CategoriaKey
WHERE p.ProductoKey > 0 GROUP BY c.Nombre ORDER BY Productos DESC;

-- Bitácora de la última ejecución
SELECT * FROM dw.BitacoraCarga ORDER BY IdBitacora DESC;
