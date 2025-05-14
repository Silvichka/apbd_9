using Tutorial9.Model.DTOs;

namespace Tutorial9.Services;

public interface IDbService
{
    Task<int> AddProductToWareshouse(InputWarehouseProduct input);
    Task<int> AddProductToWarehouseUsingProcedure(InputWarehouseProduct input);
}