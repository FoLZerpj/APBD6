using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace APBD6;

public class WarehouseController(string connectionString)
{
    private readonly SqlConnection _connection = new SqlConnection(connectionString);

    public static async Task<WarehouseController> Create(string connectionString)
    {
        WarehouseController controller = new WarehouseController(connectionString);
        await controller._connection.OpenAsync();
        return controller;
    }

    public async Task<IResult> AddProductToWarehouse(AddProductInfo info)
    {
        if (info.Amount <= 0)
        {
            return TypedResults.BadRequest("Provided Amount should be greater than 0");
        }

        decimal productPrice;
        await using (SqlCommand command = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @idProduct", this._connection))
        {
            command.Parameters.AddWithValue("@idProduct", info.IdProduct);
            decimal? price = (decimal?)await command.ExecuteScalarAsync();
            if (!price.HasValue)
            {
                return TypedResults.NotFound("Provided IdProduct does not exist");
            }

            productPrice = price.Value;
        }
        await using (SqlCommand command = new SqlCommand("SELECT IdWarehouse FROM Warehouse WHERE IdWarehouse = @idWarehouse", this._connection))
        {
            command.Parameters.AddWithValue("@idWarehouse", info.IdWarehouse);
            int? warehouseId = (int?)await command.ExecuteScalarAsync();
            if (!warehouseId.HasValue)
            {
                return TypedResults.NotFound("Provided IdWarehouse does not exist");
            }
        }

        int orderId;
        await using (SqlCommand command = new SqlCommand("SELECT o.IdOrder FROM \"Order\" o LEFT JOIN Product_Warehouse p ON o.IdOrder=p.IdOrder WHERE o.IdProduct = @idProduct AND o.Amount = @amount AND o.CreatedAt < @createdAt AND p.IdProductWarehouse IS NULL", this._connection))
        {
            command.Parameters.AddWithValue("@idProduct", info.IdProduct);
            command.Parameters.AddWithValue("@amount", info.Amount);
            command.Parameters.AddWithValue("@createdAt", info.CreatedAt);
            int? warehouseId = (int?)await command.ExecuteScalarAsync();
            if (!warehouseId.HasValue)
            {
                return TypedResults.NotFound("There is no order to fulfill");
            }
            orderId = warehouseId.Value;
        }
        
        await using (SqlCommand command = new SqlCommand("UPDATE \"Order\" SET FulfilledAt = @createdAt WHERE IdOrder = @idOrder", this._connection))
        {
            command.Parameters.AddWithValue("@createdAt", info.CreatedAt);
            command.Parameters.AddWithValue("@idOrder", orderId);
            await command.ExecuteNonQueryAsync();
        }
        
        await using (SqlCommand command = new SqlCommand("INSERT INTO Product_Warehouse(IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt) VALUES(@idWarehouse, @idProduct, @idOrder, @amount, @price, @createdAt)", this._connection))
        {
            command.Parameters.AddWithValue("@idWarehouse", info.IdWarehouse);
            command.Parameters.AddWithValue("@idProduct", info.IdProduct);
            command.Parameters.AddWithValue("@idOrder", orderId);
            command.Parameters.AddWithValue("@amount", info.Amount);
            command.Parameters.AddWithValue("@price", info.Amount * productPrice);
            command.Parameters.AddWithValue("@createdAt", info.CreatedAt);
            await command.ExecuteNonQueryAsync();
        }

        await using (SqlCommand command = new SqlCommand("SELECT @@IDENTITY AS NewId", this._connection))
        {
            decimal? newId = (decimal?)await command.ExecuteScalarAsync();
            if (!newId.HasValue)
            {
                throw new UnreachableException("Database returned no id for an inserted record");
            }
            return TypedResults.Ok(newId.Value);
        }
    }

    public async Task<IResult> AddProductToWarehouseProcedure(AddProductInfo info)
    {
        await using (var command = new SqlCommand("AddProductToWarehouse", this._connection))
        {
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@IdProduct", info.IdProduct);
            command.Parameters.AddWithValue("@IdWarehouse", info.IdWarehouse);
            command.Parameters.AddWithValue("@Amount", info.Amount);
            command.Parameters.AddWithValue("@CreatedAt", info.CreatedAt);
            decimal? newId = (decimal?)await command.ExecuteScalarAsync();
            if (!newId.HasValue)
            {
                throw new UnreachableException("Database returned no id for an inserted record");
            }
            return TypedResults.Ok(newId.Value);
        }
    }
}

public record AddProductInfo(int IdProduct, int IdWarehouse, int Amount, DateTime CreatedAt);