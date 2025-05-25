CREATE PROCEDURE GetTradeMarketValues
AS
BEGIN
    -- Get the latest price per instrument
    WITH LatestPrices AS (
        SELECT p.InstrumentID, p.ClosePrice,
               ROW_NUMBER() OVER (PARTITION BY p.InstrumentID ORDER BY p.PriceDate DESC) AS rn
        FROM Prices p
    )
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
    JOIN LatestPrices lp ON t.InstrumentID = lp.InstrumentID AND lp.rn = 1
    ORDER BY t.TradeDate DESC;
END;
