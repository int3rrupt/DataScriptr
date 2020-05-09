using DataScriptr.Library.Parsing;
using Xunit;

namespace DataScriptr.Library.Tests.UnitTests
{
    public class RegexSqlParseHelperTests
    {
        [Fact]
        public void Test1()
        {
            string sql = @"
    [ProductID]             INT              IDENTITY (1, 1) NOT NULL,
    [Name]                  [dbo].[Name]     NOT NULL,
    [ProductNumber]         NVARCHAR (25)    NOT NULL,
    [MakeFlag]              [dbo].[Flag]     CONSTRAINT [DF_Product_MakeFlag] DEFAULT ((1)) NOT NULL,
    [FinishedGoodsFlag]     [dbo].[Flag]     CONSTRAINT [DF_Product_FinishedGoodsFlag] DEFAULT ((1)) NOT NULL,
    [Color]                 NVARCHAR (15)    NULL,
    [SafetyStockLevel]      SMALLINT         NOT NULL,
    [ReorderPoint]          SMALLINT         NOT NULL,
    [StandardCost]          MONEY            NOT NULL,
    [ListPrice]             MONEY            NOT NULL,
    [Size]                  NVARCHAR (5)     NULL,
    [SizeUnitMeasureCode]   NCHAR (3)        NULL,
    [WeightUnitMeasureCode] NCHAR (3)        NULL,
    [Weight]                DECIMAL (8, 2)   NULL,
    [DaysToManufacture]     INT              NOT NULL,
    [ProductLine]           NCHAR (2)        NULL,
    [Class]                 NCHAR (2)        NULL,
    [Style]                 NCHAR (2)        NULL,
    [ProductSubcategoryID]  INT              NULL,
    [ProductModelID]        INT              NULL,
    [SellStartDate]         DATETIME         NOT NULL,
    [SellEndDate]           DATETIME         NULL,
    [DiscontinuedDate]      DATETIME         NULL,
    [rowguid]               UNIQUEIDENTIFIER CONSTRAINT [DF_Product_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]          DATETIME         CONSTRAINT [DF_Product_ModifiedDate] DEFAULT (getdate()) NOT NULL,
";
            //var reg = new RegexSqlParseHelper();
            //var result = reg.GetColumnMatches(sql);
        }
    }
}