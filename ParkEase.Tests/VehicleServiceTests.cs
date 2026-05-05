using FluentAssertions;
using Moq;
using NUnit.Framework;
using ParkEase.VehicleService.DTOs;
using ParkEase.VehicleService.Entities;
using ParkEase.VehicleService.Interfaces;
using ParkEase.VehicleService.Services;

namespace ParkEase.Tests.VehicleServiceTests;

[TestFixture]
public class VehicleServiceTests
{
    private Mock<IVehicleRepository> _repoMock = null!;
    private ParkEase.VehicleService.Services.VehicleService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IVehicleRepository>();
        _service  = new ParkEase.VehicleService.Services.VehicleService(_repoMock.Object);
    }

    // ════════════════════════════════════════════════
    // REGISTER VEHICLE
    // ════════════════════════════════════════════════

    [Test]
    public async Task RegisterVehicle_ValidRequest_ReturnsSuccess()
    {
        var request = ValidRegisterRequest();
        _repoMock.Setup(r => r.ExistsByLicensePlateAsync(request.LicensePlate, request.OwnerId))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Vehicle>()))
                 .ReturnsAsync((Vehicle v) => { v.VehicleId = 1; return v; });

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.LicensePlate.Should().Be("MH12AB1234");
        result.Data.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task RegisterVehicle_InvalidVehicleType_ReturnsFail()
    {
        var request = ValidRegisterRequest();
        request.VehicleType = "TRUCK";

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid VehicleType");
    }

    [Test]
    public async Task RegisterVehicle_EmptyLicensePlate_ReturnsFail()
    {
        var request = ValidRegisterRequest();
        request.LicensePlate = "";

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("License plate is required");
    }

    [Test]
    public async Task RegisterVehicle_EmptyMake_ReturnsFail()
    {
        var request = ValidRegisterRequest();
        request.Make = "";

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Vehicle make is required");
    }

    [Test]
    public async Task RegisterVehicle_EmptyModel_ReturnsFail()
    {
        var request = ValidRegisterRequest();
        request.Model = "";

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Vehicle model is required");
    }

    [Test]
    public async Task RegisterVehicle_DuplicatePlate_ReturnsFail()
    {
        var request = ValidRegisterRequest();
        _repoMock.Setup(r => r.ExistsByLicensePlateAsync(request.LicensePlate, request.OwnerId))
                 .ReturnsAsync(true);

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already registered");
    }

    [Test]
    public async Task RegisterVehicle_PlateIsUppercased()
    {
        var request = ValidRegisterRequest();
        request.LicensePlate = "mh12ab1234";
        _repoMock.Setup(r => r.ExistsByLicensePlateAsync(It.IsAny<string>(), It.IsAny<int>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Vehicle>()))
                 .ReturnsAsync((Vehicle v) => v);

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.LicensePlate.Should().Be("MH12AB1234");
    }

    [Test]
    [TestCase("2W")]
    [TestCase("4W")]
    [TestCase("HEAVY")]
    public async Task RegisterVehicle_AllValidTypes_ReturnsSuccess(string vehicleType)
    {
        var request = ValidRegisterRequest();
        request.VehicleType = vehicleType;
        _repoMock.Setup(r => r.ExistsByLicensePlateAsync(It.IsAny<string>(), It.IsAny<int>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Vehicle>()))
                 .ReturnsAsync((Vehicle v) => v);

        var result = await _service.RegisterVehicleAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.VehicleType.Should().Be(vehicleType);
    }

    // ════════════════════════════════════════════════
    // GET VEHICLE
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetVehicleById_ExistingVehicle_ReturnsVehicle()
    {
        _repoMock.Setup(r => r.FindByVehicleIdAsync(1)).ReturnsAsync(SampleVehicle());

        var result = await _service.GetVehicleByIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.VehicleId.Should().Be(1);
    }

    [Test]
    public async Task GetVehicleById_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByVehicleIdAsync(99)).ReturnsAsync((Vehicle?)null);

        var result = await _service.GetVehicleByIdAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Vehicle not found");
    }

    [Test]
    public async Task GetByLicensePlate_ExistingPlate_ReturnsVehicle()
    {
        _repoMock.Setup(r => r.FindByLicensePlateAsync("MH12AB1234")).ReturnsAsync(SampleVehicle());

        var result = await _service.GetByLicensePlateAsync("MH12AB1234");

        result.Success.Should().BeTrue();
        result.Data!.LicensePlate.Should().Be("MH12AB1234");
    }

    [Test]
    public async Task GetByLicensePlate_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByLicensePlateAsync("XX00XX0000")).ReturnsAsync((Vehicle?)null);

        var result = await _service.GetByLicensePlateAsync("XX00XX0000");

        result.Success.Should().BeFalse();
    }

    [Test]
    public async Task GetVehiclesByOwner_ReturnsOwnerVehicles()
    {
        var vehicles = new List<Vehicle> { SampleVehicle(), SampleVehicle() };
        _repoMock.Setup(r => r.FindByOwnerIdAsync(1)).ReturnsAsync(vehicles);

        var result = await _service.GetVehiclesByOwnerAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(2);
    }

    // ════════════════════════════════════════════════
    // EV CHECK
    // ════════════════════════════════════════════════

    [Test]
    public async Task IsEVVehicle_EVVehicle_ReturnsTrue()
    {
        var vehicle = SampleVehicle();
        vehicle.IsEV = true;
        _repoMock.Setup(r => r.FindByVehicleIdAsync(1)).ReturnsAsync(vehicle);

        var result = await _service.IsEVVehicleAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Test]
    public async Task IsEVVehicle_NonEVVehicle_ReturnsFalse()
    {
        var vehicle = SampleVehicle();
        vehicle.IsEV = false;
        _repoMock.Setup(r => r.FindByVehicleIdAsync(1)).ReturnsAsync(vehicle);

        var result = await _service.IsEVVehicleAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Test]
    public async Task IsEVVehicle_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByVehicleIdAsync(99)).ReturnsAsync((Vehicle?)null);

        var result = await _service.IsEVVehicleAsync(99);

        result.Success.Should().BeFalse();
    }

    // ════════════════════════════════════════════════
    // DELETE
    // ════════════════════════════════════════════════

    [Test]
    public async Task DeleteVehicle_ExistingVehicle_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.FindByVehicleIdAsync(1)).ReturnsAsync(SampleVehicle());
        _repoMock.Setup(r => r.DeleteByVehicleIdAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteVehicleAsync(1);

        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task DeleteVehicle_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByVehicleIdAsync(99)).ReturnsAsync((Vehicle?)null);

        var result = await _service.DeleteVehicleAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Vehicle not found");
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static RegisterVehicleRequest ValidRegisterRequest() => new()
    {
        OwnerId      = 1,
        LicensePlate = "MH12AB1234",
        Make         = "Toyota",
        Model        = "Camry",
        Color        = "White",
        VehicleType  = "4W",
        IsEV         = false
    };

    private static Vehicle SampleVehicle() => new()
    {
        VehicleId    = 1,
        OwnerId      = 1,
        LicensePlate = "MH12AB1234",
        Make         = "Toyota",
        Model        = "Camry",
        Color        = "White",
        VehicleType  = "4W",
        IsEV         = false,
        IsActive     = true,
        RegisteredAt = DateTime.UtcNow
    };
}
