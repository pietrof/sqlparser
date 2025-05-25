CREATE PROCEDURE gettradewithopenprice
AS
BEGIN
   
    SELECT 
        t.TradeID,
        t.TradeDate,
        t.TradeType,
        t.Quantity,
        t.Price AS TradePrice,
        lp.ClosePrice AS CurrentPrice,
        (t.Quantity * lp.ClosePrice) AS MarketValue,
        i.Symbol,
        i.Name,
        t.Trader
    FROM Trades t
    JOIN Instruments i ON t.InstrumentID = i.InstrumentID
    JOIN Price lp ON t.InstrumentID = lp.InstrumentID AND lp.PriceDate = t.TradeDate
    WHERE lp.OpenPrice IS NOT NULL
    ORDER BY t.TradeDate DESC;
END;
