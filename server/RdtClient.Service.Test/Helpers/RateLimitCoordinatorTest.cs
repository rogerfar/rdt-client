using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class RateLimitCoordinatorTest
{
    [Fact]
    public void UpdateCooldown_SetsCooldown()
    {
        // Arrange
        var coordinator = new RateLimitCoordinator();
        var key = "test.host";
        var delay = TimeSpan.FromMinutes(5);

        // Act
        coordinator.UpdateCooldown(key, delay);
        var remaining = coordinator.GetRemainingCooldown(key);

        // Assert
        Assert.True(remaining > TimeSpan.FromMinutes(4) && remaining <= TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetRemainingCooldown_ReturnsZero_WhenNoCooldown()
    {
        // Arrange
        var coordinator = new RateLimitCoordinator();
        var key = "test.host";

        // Act
        var remaining = coordinator.GetRemainingCooldown(key);

        // Assert
        Assert.Equal(TimeSpan.Zero, remaining);
    }

    [Fact]
    public void GetMaxNextAllowedAt_ReturnsMax()
    {
        // Arrange
        var coordinator = new RateLimitCoordinator();
        var now = DateTimeOffset.UtcNow;
        coordinator.UpdateCooldown("host1", TimeSpan.FromMinutes(10));
        coordinator.UpdateCooldown("host2", TimeSpan.FromMinutes(20));

        // Act
        var maxNext = coordinator.GetMaxNextAllowedAt();

        // Assert
        Assert.NotNull(maxNext);
        Assert.True(maxNext > now.AddMinutes(19));
        Assert.True(maxNext < now.AddMinutes(21));
    }

    [Fact]
    public void GetMaxNextAllowedAt_ReturnsNull_WhenAllExpired()
    {
        // Arrange
        var coordinator = new RateLimitCoordinator();
        coordinator.UpdateCooldown("host1", TimeSpan.FromSeconds(-10));

        // Act
        var maxNext = coordinator.GetMaxNextAllowedAt();

        // Assert
        Assert.Null(maxNext);
    }
}
