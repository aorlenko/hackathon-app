USE [TradingPlatformMarket];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

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

COMMIT TRANSACTION;
GO

USE [TradingPlatformTrade];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE FROM dbo.Trades;

COMMIT TRANSACTION;
GO

USE [TradingPlatformSettlement];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DELETE FROM dbo.Settlements;

COMMIT TRANSACTION;
GO
