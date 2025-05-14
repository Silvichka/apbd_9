using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Tutorial9.Exception;
using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }

    public async Task<int> AddProductToWareshouse(InputWarehouseProduct input)
    {
        if (input.Amount <= 0)
            throw new System.Exception("Invalid amount!");

        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.Parameters.Clear();
            command.CommandText = "select 1 from Product where IdProduct = @idProd";
            command.Parameters.AddWithValue("@idProd", input.IdProduct);

            var prodRes = await command.ExecuteScalarAsync();
            if (prodRes is null)
                throw new NotFoundException("No product with given ID");

            command.Parameters.Clear();
            command.CommandText = "select 1 from Warehouse where IdWarehouse = @idWar";
            command.Parameters.AddWithValue("@idWar", input.IdWarehouse);

            var warRes = await command.ExecuteScalarAsync();
            if (warRes is null)
                throw new NotFoundException("No warehouse with given ID");

            command.Parameters.Clear();
            command.CommandText =
                "select IdOrder from [Order] where IdProduct = @idProd and Amount = @amount and CreatedAt < @createdAt";
            command.Parameters.AddWithValue("@idProd", input.IdProduct);
            command.Parameters.AddWithValue("@amount", input.Amount);
            command.Parameters.AddWithValue("@createdAt", input.CreatedAt);

            var orderRes = await command.ExecuteScalarAsync();
            if (orderRes is null)
                throw new NotFoundException("No such order for given product for given amount");

            command.Parameters.Clear();
            command.CommandText = "select 1 from Product_Warehouse where IdOrder = @idOrder";
            command.Parameters.AddWithValue("@idOrder", orderRes);

            var isCompleted = await command.ExecuteScalarAsync();
            if (isCompleted is not null)
                throw new System.Exception("This order is already completed");

            command.Parameters.Clear();
            command.CommandText = @"update [Order]
                                    set FulfilledAt = CURRENT_DATE
                                    where IdOrder = @idOrder";
            command.Parameters.AddWithValue("@idOrder", orderRes);

            command.Parameters.Clear();
            command.CommandText = @"select price from Product where IdProduct = @IdProd";
            command.Parameters.AddWithValue("@IdProd", input.IdProduct);

            var prodPrice = (int)await command.ExecuteScalarAsync();

            command.Parameters.Clear();
            command.CommandText =
                @"insert into Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                    output INSERTED.IdProductWareshouse
                                    values (@idWareshouse, @idProduct, @idOrder, @amount, @price, @createAt)";
            command.Parameters.AddWithValue("@idWareshouse", input.IdWarehouse);
            command.Parameters.AddWithValue("@idProduct", input.IdProduct);
            command.Parameters.AddWithValue("@idOrder", prodRes);
            command.Parameters.AddWithValue("@amount", input.Amount * prodPrice);
            command.Parameters.AddWithValue("@price", input.Amount * prodPrice);
            command.Parameters.AddWithValue("@createdAt", DateTime.Now);

            var id = await command.ExecuteScalarAsync();

            await transaction.CommitAsync();

            return (int)id;
        }
        catch (System.Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task<int> AddProductToWarehouseUsingProcedure(InputWarehouseProduct input)
    {
        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand("AddProductToWarehouse", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@IdProduct", input.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", input.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", input.Amount);
        command.Parameters.AddWithValue("@CreatedAt", input.CreatedAt);

        await connection.OpenAsync();

        var result = await command.ExecuteScalarAsync();

        if (result is null || result == DBNull.Value)
            throw new System.Exception("Procedure did not return an ID");

        return Convert.ToInt32(result);
    }
}