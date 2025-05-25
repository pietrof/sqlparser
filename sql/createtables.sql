CREATE TABLE Trades (
    TradeID INT PRIMARY KEY IDENTITY(1,1),
    InstrumentID INT FOREIGN KEY REFERENCES Instruments(InstrumentID),
    TradeDate DATETIME NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(18,4) NOT NULL,
    TradeType VARCHAR(4) CHECK (TradeType IN ('BUY', 'SELL')),
    Trader VARCHAR(100),
    CreatedAt DATETIME DEFAULT GETDATE()
);
go
CREATE TABLE Prices (
    PriceID INT PRIMARY KEY IDENTITY(1,1),
    InstrumentID INT FOREIGN KEY REFERENCES Instruments(InstrumentID),
    PriceDate DATE NOT NULL,
    OpenPrice DECIMAL(18,4),
    ClosePrice DECIMAL(18,4),
    HighPrice DECIMAL(18,4),
    LowPrice DECIMAL(18,4),
    Volume BIGINT,
    CCY VARCHAR(50) NOT NULL,
    CONSTRAINT UQ_InstrumentDate UNIQUE (InstrumentID, PriceDate)
);

go
CREATE TABLE Instruments (
	InstrumentID INT PRIMARY KEY IDENTITY(1,1),
	Symbol VARCHAR(10) NOT NULL UNIQUE,
	Name VARCHAR(100) NOT NULL,
	Type VARCHAR(50) NOT NULL,
	CreatedAt DATETIME DEFAULT GETDATE()
);
    go
    CREATE TABLE FX (
	
	CCY VARCHAR(50) NOT NULL,
    FXRateFromCHF DECIMAL(18,4),
    
	CreatedAt DATETIME DEFAULT GETDATE()
);
    go