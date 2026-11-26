using VentasDW.Carga;
using VentasDW.Carga.Cargadores;
using VentasDW.Carga.Infraestructura;
using VentasDW.Carga.Modelos;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<OpcionesCarga>(builder.Configuration.GetSection("Carga"));
builder.Services.AddSingleton<BaseDatos>();
builder.Services.AddSingleton<ICargadorDimension, CargadorDimFecha>();
builder.Services.AddSingleton<ICargadorDimension, CargadorDimCliente>();
builder.Services.AddSingleton<ICargadorDimension, CargadorDimCategoria>();
builder.Services.AddSingleton<ICargadorDimension, CargadorDimProducto>();
builder.Services.AddSingleton<ICargadorDimension, CargadorDimMetodoPago>();
builder.Services.AddHostedService<Worker>();
builder.Build().Run();
