CREATE PROCEDURE gettradewithpricehistory
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
        t.Trader,
        p.PriceDate AS PriceDate,
        p.ClosePrice AS HistoricalClosePrice
    FROM Trades t
    JOIN Instruments i ON t.InstrumentID = i.InstrumentID
    JOIN Prices lp ON t.InstrumentID = lp.InstrumentID 
    ORDER BY t.TradeDate DESC;
END;
