CREATE PROCEDURE getpricesindifferentfx
AS
BEGIN
   
   select fxFXRateFromCHF*p.ClosePrice as ClosePriceInCHF,* from Prices p join FX fx on fx.CCY = p.CCY where fx.CCY="USD"
    
END;
