-- TradingPlatformMarket

DELETE FROM dbo.PetNotifications;
DELETE FROM dbo.PetListingBids;
DELETE FROM dbo.PetTrades;
DELETE FROM dbo.PetListings;
DELETE FROM dbo.Pets;

UPDATE dbo.PetBreedSupply
SET RemainingCount = 3;

UPDATE dbo.PetTraders
SET AvailableCash = 1000,
    LockedCash = 0;

DELETE FROM dbo.OrderMatchAudits;
DELETE FROM dbo.Orders;
GO

-- TradingPlatformTrade

DELETE FROM dbo.Trades;
GO

-- TradingPlatformSettlement

DELETE FROM dbo.Settlements;
GO
