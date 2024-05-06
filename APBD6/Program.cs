using APBD6;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

const string connectionString = "Server=localhost;Database=master;Trusted_Connection=True;";

var controller = await WarehouseController.Create(connectionString);

app.MapPost("/product", async ([FromBody] AddProductInfo info) => await controller.AddProductToWarehouse(info))
    .WithName("PostProduct")
    .WithOpenApi();

app.MapPost("/product/procedure", async ([FromBody] AddProductInfo info) => await controller.AddProductToWarehouseProcedure(info))
    .WithName("PostProductUsingProcedure")
    .WithOpenApi();

app.Run();