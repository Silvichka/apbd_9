using Microsoft.AspNetCore.Mvc;
using Tutorial9.Exception;
using Tutorial9.Model.DTOs;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{

    private readonly IDbService _dbService;

    public WarehouseController(IDbService service)
    {
        _dbService = service;
    }

    [HttpPost("order")]
    public async Task<IActionResult> addOrder([FromBody] InputWarehouseProduct input)
    {
        if (input.Amount <= 0)
            return BadRequest("Invalid amount, must be greater than 0");
        
        int? res = null;

        try
        {
            res = await _dbService.AddProductToWareshouse(input);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (System.Exception e)
        {
            return Conflict(e.Message);
        }

        return CreatedAtAction(string.Empty, $"{res}");
    }

    [HttpPost("order/procedure")]
    public async Task<IActionResult> addOrderWithProcedure([FromBody] InputWarehouseProduct input)
    {
        if (input.Amount <= 0)
            return BadRequest("Invalid amount, must be greater than 0");
        
        int? res = null;

        try
        {
            res = await _dbService.AddProductToWarehouseUsingProcedure(input);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (System.Exception e)
        {
            return Conflict(e.Message);
        }

        return CreatedAtAction(string.Empty, $"{res}");
    }
    
}