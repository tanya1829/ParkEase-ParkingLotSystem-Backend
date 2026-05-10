using FluentAssertions;
using Moq;
using NUnit.Framework;
using ParkEase.SpotService.DTOs;
using ParkEase.SpotService.Entities;
using ParkEase.SpotService.Interfaces;
using ParkEase.SpotService.Services;

namespace ParkEase.Tests.SpotServiceTests;

[TestFixture]
public class SpotServiceTests
{
    private Mock<ISpotRepository> _repoMock = null!;
    private ParkEase.SpotService.Services.SpotService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<ISpotRepository>();
        _service  = new ParkEase.SpotService.Services.SpotService(_repoMock.Object);
    }

    // ════════════════════════════════════════════════
    // ADD SPOT
    // ════════════════════════════════════════════════

    [Test]
    public async Task AddSpot_ValidRequest_ReturnsSuccess()
    {
        var request = ValidAddSpotRequest();
        _repoMock.Setup(r => r.ExistsByLotIdAndSpotNumberAsync(request.LotId, request.SpotNumber))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingSpot>()))
                 .ReturnsAsync((ParkingSpot s) => { s.SpotId = 1; return s; });

        var result = await _service.AddSpotAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("AVAILABLE");
        result.Data.SpotType.Should().Be("STANDARD");
    }

    [Test]
    public async Task AddSpot_InvalidSpotType_ReturnsFail()
    {
        var request = ValidAddSpotRequest();
        request.SpotType = "HELICOPTER";

        var result = await _service.AddSpotAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid SpotType");
    }

    [Test]
    public async Task AddSpot_InvalidVehicleType_ReturnsFail()
    {
        var request = ValidAddSpotRequest();
        request.VehicleType = "BOAT";

        var result = await _service.AddSpotAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid VehicleType");
    }

    [Test]
    public async Task AddSpot_ZeroPrice_ReturnsFail()
    {
        var request = ValidAddSpotRequest();
        request.PricePerHour = 0;

        var result = await _service.AddSpotAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("PricePerHour must be greater than 0");
    }

    [Test]
    public async Task AddSpot_DuplicateSpotNumber_ReturnsFail()
    {
        var request = ValidAddSpotRequest();
        _repoMock.Setup(r => r.ExistsByLotIdAndSpotNumberAsync(request.LotId, request.SpotNumber))
                 .ReturnsAsync(true);

        var result = await _service.AddSpotAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Test]
    [TestCase("COMPACT")]
    [TestCase("STANDARD")]
    [TestCase("LARGE")]
    [TestCase("MOTORBIKE")]
    [TestCase("EV")]
    public async Task AddSpot_AllValidSpotTypes_ReturnsSuccess(string spotType)
    {
        var request = ValidAddSpotRequest();
        request.SpotType = spotType;
        _repoMock.Setup(r => r.ExistsByLotIdAndSpotNumberAsync(It.IsAny<int>(), It.IsAny<string>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingSpot>()))
                 .ReturnsAsync((ParkingSpot s) => s);

        var result = await _service.AddSpotAsync(request);

        result.Success.Should().BeTrue();
    }

    // ════════════════════════════════════════════════
    // BULK ADD
    // ════════════════════════════════════════════════

    [Test]
    public async Task AddBulkSpots_ValidRequest_CreatesCorrectCount()
    {
        var request = new AddBulkSpotsRequest
        {
            LotId = 1, Count = 5, SpotNumberPrefix = "A",
            SpotType = "STANDARD", VehicleType = "4W",
            PricePerHour = 50m, Floor = 0
        };
        _repoMock.Setup(r => r.ExistsByLotIdAndSpotNumberAsync(It.IsAny<int>(), It.IsAny<string>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateBulkAsync(It.IsAny<List<ParkingSpot>>()))
                 .ReturnsAsync((List<ParkingSpot> spots) => spots);

        var result = await _service.AddBulkSpotsAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(5);
    }

    [Test]
    public async Task AddBulkSpots_CountZero_ReturnsFail()
    {
        var request = new AddBulkSpotsRequest { LotId = 1, Count = 0, PricePerHour = 50m, SpotType = "STANDARD", VehicleType = "4W" };

        var result = await _service.AddBulkSpotsAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Count must be between 1 and 500");
    }

    [Test]
    public async Task AddBulkSpots_CountOver500_ReturnsFail()
    {
        var request = new AddBulkSpotsRequest { LotId = 1, Count = 501, PricePerHour = 50m, SpotType = "STANDARD", VehicleType = "4W" };

        var result = await _service.AddBulkSpotsAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Count must be between 1 and 500");
    }

    [Test]
    public async Task AddBulkSpots_AllSpotsAlreadyExist_ReturnsFail()
    {
        var request = new AddBulkSpotsRequest
        {
            LotId = 1, Count = 3, SpotNumberPrefix = "A",
            SpotType = "STANDARD", VehicleType = "4W", PricePerHour = 50m
        };
        _repoMock.Setup(r => r.ExistsByLotIdAndSpotNumberAsync(It.IsAny<int>(), It.IsAny<string>()))
                 .ReturnsAsync(true); // all exist

        var result = await _service.AddBulkSpotsAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("All spot numbers already exist");
    }

    // ════════════════════════════════════════════════
    // STATUS TRANSITIONS
    // ════════════════════════════════════════════════

    [Test]
    public async Task ReserveSpot_AvailableSpot_ReturnsReserved()
    {
        var spot = AvailableSpot();
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingSpot>())).ReturnsAsync((ParkingSpot s) => s);

        var result = await _service.ReserveSpotAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("RESERVED");
    }

    [Test]
    public async Task ReserveSpot_AlreadyReserved_ReturnsFail()
    {
        var spot = AvailableSpot();
        spot.Status = "RESERVED";
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);

        var result = await _service.ReserveSpotAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not available");
    }

    [Test]
    public async Task OccupySpot_ReservedSpot_ReturnsOccupied()
    {
        var spot = AvailableSpot();
        spot.Status = "RESERVED";
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingSpot>())).ReturnsAsync((ParkingSpot s) => s);

        var result = await _service.OccupySpotAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("OCCUPIED");
    }

    [Test]
    public async Task OccupySpot_AlreadyOccupied_ReturnsFail()
    {
        var spot = AvailableSpot();
        spot.Status = "OCCUPIED";
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);

        var result = await _service.OccupySpotAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already occupied");
    }

    [Test]
    public async Task ReleaseSpot_OccupiedSpot_ReturnsAvailable()
    {
        var spot = AvailableSpot();
        spot.Status = "OCCUPIED";
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingSpot>())).ReturnsAsync((ParkingSpot s) => s);

        var result = await _service.ReleaseSpotAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("AVAILABLE");
    }

    [Test]
    public async Task ReleaseSpot_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindBySpotIdAsync(99)).ReturnsAsync((ParkingSpot?)null);

        var result = await _service.ReleaseSpotAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Spot not found");
    }

    // ════════════════════════════════════════════════
    // DELETE
    // ════════════════════════════════════════════════

    [Test]
    public async Task DeleteSpot_AvailableSpot_ReturnsSuccess()
    {
        var spot = AvailableSpot();
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);
        _repoMock.Setup(r => r.DeleteBySpotIdAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteSpotAsync(1);

        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task DeleteSpot_OccupiedSpot_ReturnsFail()
    {
        var spot = AvailableSpot();
        spot.Status = "OCCUPIED";
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);

        var result = await _service.DeleteSpotAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot delete a spot that is currently reserved or occupied");
    }

    [Test]
    public async Task DeleteSpot_ReservedSpot_ReturnsFail()
    {
        var spot = AvailableSpot();
        spot.Status = "RESERVED";
        _repoMock.Setup(r => r.FindBySpotIdAsync(1)).ReturnsAsync(spot);

        var result = await _service.DeleteSpotAsync(1);

        result.Success.Should().BeFalse();
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static AddSpotRequest ValidAddSpotRequest() => new()
    {
        LotId        = 1,
        SpotNumber   = "A1",
        Floor        = 0,
        SpotType     = "STANDARD",
        VehicleType  = "4W",
        PricePerHour = 50m,
        IsHandicapped = false,
        IsEVCharging  = false
    };

    private static ParkingSpot AvailableSpot() => new()
    {
        SpotId       = 1,
        LotId        = 1,
        SpotNumber   = "A1",
        Floor        = 0,
        SpotType     = "STANDARD",
        VehicleType  = "4W",
        Status       = "AVAILABLE",
        PricePerHour = 50m,
        CreatedAt    = DateTime.UtcNow
    };
}
