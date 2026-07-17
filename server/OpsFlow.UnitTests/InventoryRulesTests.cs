using FluentAssertions;
using OpsFlow.Application.Rules;
using OpsFlow.Domain.Enums;

namespace OpsFlow.UnitTests;

public sealed class InventoryRulesTests
{
    [Theory]
    [InlineData(3, 5, AssetStatus.InStock, true)]
    [InlineData(5, 5, AssetStatus.InStock, true)]
    [InlineData(6, 5, AssetStatus.InStock, false)]
    [InlineData(0, 0, AssetStatus.Maintenance, true)]
    [InlineData(0, 5, AssetStatus.Retired, false)]
    public void IsLowStock_returns_expected_result(int quantity, int threshold, AssetStatus status, bool expected) =>
        InventoryRules.IsLowStock(quantity, threshold, status).Should().Be(expected);

    [Theory]
    [InlineData(0, true)]
    [InlineData(30, true)]
    [InlineData(60, true)]
    [InlineData(61, false)]
    [InlineData(-1, false)]
    public void WarrantyExpiresWithin_respects_window(int offsetDays, bool expected)
    {
        var today = new DateOnly(2026, 7, 17);
        InventoryRules.WarrantyExpiresWithin(today.AddDays(offsetDays), 60, today).Should().Be(expected);
    }

    [Fact]
    public void WarrantyExpiresWithin_returns_false_when_missing() =>
        InventoryRules.WarrantyExpiresWithin(null, 60, new DateOnly(2026, 7, 17)).Should().BeFalse();

    [Theory]
    [InlineData(5, 3, InventoryTransactionType.CheckIn, 8)]
    [InlineData(5, -3, InventoryTransactionType.CheckIn, 8)]
    [InlineData(5, 3, InventoryTransactionType.CheckOut, 2)]
    [InlineData(5, -3, InventoryTransactionType.CheckOut, 2)]
    [InlineData(5, 2, InventoryTransactionType.Adjustment, 7)]
    [InlineData(5, -2, InventoryTransactionType.Adjustment, 3)]
    [InlineData(5, 4, InventoryTransactionType.Transfer, 5)]
    [InlineData(5, 5, InventoryTransactionType.Retirement, 0)]
    public void ApplyTransaction_calculates_quantity(int current, int change, InventoryTransactionType type, int expected) =>
        InventoryRules.ApplyTransaction(current, change, type).Should().Be(expected);

    [Theory]
    [InlineData(2, 3, InventoryTransactionType.CheckOut)]
    [InlineData(0, 1, InventoryTransactionType.Retirement)]
    [InlineData(1, -2, InventoryTransactionType.Adjustment)]
    public void ApplyTransaction_rejects_negative_inventory(int current, int change, InventoryTransactionType type) =>
        FluentActions.Invoking(() => InventoryRules.ApplyTransaction(current, change, type))
            .Should().Throw<InvalidOperationException>().WithMessage("*cannot be negative*");
}
