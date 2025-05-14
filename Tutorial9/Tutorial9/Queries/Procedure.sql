CREATE PROCEDURE AddProductToWarehouse 
    @IdProduct INT, 
    @IdWarehouse INT, 
    @Amount INT,  
    @CreatedAt DATETIME
AS  
BEGIN  
    SET XACT_ABORT ON;
    DECLARE @IdOrder INT, @Price DECIMAL(18,2), @IdProductFromDb INT;

    -- Check if the order exists and is unfulfilled
    SELECT TOP 1 @IdOrder = o.IdOrder
    FROM [Order] o
    WHERE o.IdProduct = @IdProduct
      AND o.Amount = @Amount
      AND o.CreatedAt < @CreatedAt
      AND NOT EXISTS (
          SELECT 1 
          FROM Product_Warehouse pw 
          WHERE pw.IdOrder = o.IdOrder
      );

    -- Get product details
    SELECT @IdProductFromDb = IdProduct, @Price = Price 
    FROM Product 
    WHERE IdProduct = @IdProduct;

    -- Validate product
    IF @IdProductFromDb IS NULL  
    BEGIN  
        RAISERROR('Invalid parameter: Provided IdProduct does not exist', 18, 0);  
        RETURN;  
    END;  

    -- Validate order
    IF @IdOrder IS NULL  
    BEGIN  
        RAISERROR('Invalid parameter: There is no matching unfulfilled order', 18, 0);  
        RETURN;  
    END;  

    -- Validate warehouse
    IF NOT EXISTS (SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse)  
    BEGIN  
        RAISERROR('Invalid parameter: Provided IdWarehouse does not exist', 18, 0);  
        RETURN;  
    END;  

    BEGIN TRAN;

    -- Mark order as fulfilled
    UPDATE [Order]
    SET FulfilledAt = @CreatedAt
    WHERE IdOrder = @IdOrder;

    -- Insert into Product_Warehouse
    INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
    VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Amount * @Price, @CreatedAt);

    -- Return the new primary key
    SELECT SCOPE_IDENTITY() AS NewId;

    COMMIT;
END;