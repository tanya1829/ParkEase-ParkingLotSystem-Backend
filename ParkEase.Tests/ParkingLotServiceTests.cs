using FluentAssertions;
using Moq;
using NUnit.Framework;
using ParkEase.ParkingLotService.DTOs;
using ParkEase.ParkingLotService.Entities;
using ParkEase.ParkingLotService.Interfaces;

namespace ParkEase.Tests.ParkingLotServiceTests;

[TestFixture]
public class ParkingLotServiceTests
{
    private Mock<IParkingLotRepository> _repoMock = null!;
    private ParkEase.ParkingLotService.Services.ParkingLotService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IParkingLotRepository>();
        _service  = new ParkEase.ParkingLotService.Services.ParkingLotService(_repoMock.Object);
    }

    // ════════════════════════════════════════════════
    // CREATE LOT
    // ════════════════════════════════════════════════

    [Test]
    public async Task CreateLot_ValidRequest_ReturnsSuccess()
    {
        var request = ValidCreateRequest();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ParkingLot>()))
                 .ReturnsAsync((ParkingLot l) => { l.LotId = 1; return l; });

        var result = await _service.CreateLotAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.IsApproved.Should().BeFalse();
        result.Data.IsOpen.Should().BeFalse();
        result.Data.AvailableSpots.Should().Be(request.TotalSpots);
    }

    [Test]
    public async Task CreateLot_ZeroTotalSpots_ReturnsFail()
    {
        var request = ValidCreateRequest();
        request.TotalSpots = 0;

        var result = await _service.CreateLotAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Total spots must be greater than 0");
    }

    // ════════════════════════════════════════════════
    // APPROVE / REJECT
    // ════════════════════════════════════════════════

    [Test]
    public async Task ApproveLot_ExistingLot_SetsApproved()
    {
        var lot = UnapprovedLot();
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingLot>())).ReturnsAsync((ParkingLot l) => l);

        var result = await _service.ApproveLotAsync(1);

        result.Success.Should().BeTrue();
        lot.IsApproved.Should().BeTrue();
    }

    [Test]
    public async Task ApproveLot_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByLotIdAsync(99)).ReturnsAsync((ParkingLot?)null);

        var result = await _service.ApproveLotAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Test]
    public async Task RejectLot_ExistingLot_DeletesLot()
    {
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(UnapprovedLot());
        _repoMock.Setup(r => r.DeleteByLotIdAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.RejectLotAsync(1);

        result.Success.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteByLotIdAsync(1), Times.Once);
    }

    // ════════════════════════════════════════════════
    // TOGGLE OPEN
    // ════════════════════════════════════════════════

    [Test]
    public async Task ToggleOpen_ApprovedLot_TogglesIsOpen()
    {
        var lot = ApprovedLot();
        lot.IsOpen = false;
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingLot>())).ReturnsAsync((ParkingLot l) => l);

        var result = await _service.ToggleOpenAsync(1);

        result.Success.Should().BeTrue();
        lot.IsOpen.Should().BeTrue();
        result.Data.Should().Contain("opened");
    }

    [Test]
    public async Task ToggleOpen_UnapprovedLot_ReturnsFail()
    {
        var lot = UnapprovedLot();
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);

        var result = await _service.ToggleOpenAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("must be approved by admin");
    }

    [Test]
    public async Task ToggleOpen_OpenLot_ClosesIt()
    {
        var lot = ApprovedLot();
        lot.IsOpen = true;
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingLot>())).ReturnsAsync((ParkingLot l) => l);

        var result = await _service.ToggleOpenAsync(1);

        result.Success.Should().BeTrue();
        lot.IsOpen.Should().BeFalse();
        result.Data.Should().Contain("closed");
    }

    // ════════════════════════════════════════════════
    // AVAILABLE SPOTS COUNTER
    // ════════════════════════════════════════════════

    [Test]
    public async Task DecrementAvailable_HasSpots_Decrements()
    {
        var lot = ApprovedLot();
        lot.AvailableSpots = 5;
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingLot>())).ReturnsAsync((ParkingLot l) => l);

        var result = await _service.DecrementAvailableAsync(1);

        result.Success.Should().BeTrue();
        lot.AvailableSpots.Should().Be(4);
    }

    [Test]
    public async Task DecrementAvailable_NoSpotsLeft_ReturnsFail()
    {
        var lot = ApprovedLot();
        lot.AvailableSpots = 0;
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);

        var result = await _service.DecrementAvailableAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No available spots remaining");
    }

    [Test]
    public async Task IncrementAvailable_BelowMax_Increments()
    {
        var lot = ApprovedLot();
        lot.TotalSpots     = 10;
        lot.AvailableSpots = 8;
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingLot>())).ReturnsAsync((ParkingLot l) => l);

        var result = await _service.IncrementAvailableAsync(1);

        result.Success.Should().BeTrue();
        lot.AvailableSpots.Should().Be(9);
    }

    [Test]
    public async Task IncrementAvailable_AlreadyAtMax_ReturnsFail()
    {
        var lot = ApprovedLot();
        lot.TotalSpots     = 10;
        lot.AvailableSpots = 10;
        _repoMock.Setup(r => r.FindByLotIdAsync(1)).ReturnsAsync(lot);

        var result = await _service.IncrementAvailableAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already at maximum");
    }

    // ════════════════════════════════════════════════
    // HAVERSINE DISTANCE
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetNearbyLots_ReturnsLotsWithDistance()
    {
        var lots = new List<ParkingLot> { ApprovedLot() };
        _repoMock.Setup(r => r.FindNearbyAsync(19.076, 72.877, 5.0)).ReturnsAsync(lots);

        var result = await _service.GetNearbyLotsAsync(19.076, 72.877, 5.0);

        result.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(1);
        result.Data[0].DistanceKm.Should().BeGreaterThanOrEqualTo(0);
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static CreateLotRequest ValidCreateRequest() => new()
    {
        Name        = "Test Lot",
        Address     = "123 Main St",
        City        = "Mumbai",
        Latitude    = 19.076,
        Longitude   = 72.877,
        TotalSpots  = 50,
        ManagerId   = 1,
        OpenTime    = "08:00",
        CloseTime   = "22:00",
        Description = "Test parking lot"
    };

    private static ParkingLot UnapprovedLot() => new()
    {
        LotId          = 1,
        Name           = "Test Lot",
        Address        = "123 Main St",
        City           = "Mumbai",
        Latitude       = 19.076,
        Longitude      = 72.877,
        TotalSpots     = 50,
        AvailableSpots = 50,
        ManagerId      = 1,
        IsOpen         = false,
        IsApproved     = false,
        OpenTime       = new TimeOnly(8, 0),
        CloseTime      = new TimeOnly(22, 0),
        CreatedAt      = DateTime.UtcNow
    };

    private static ParkingLot ApprovedLot()
    {
        var lot = UnapprovedLot();
        lot.IsApproved = true;
        return lot;
    }
}