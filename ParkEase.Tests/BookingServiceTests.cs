using FluentAssertions;
using Moq;
using NUnit.Framework;
using ParkEase.BookingService.DTOs;
using ParkEase.BookingService.Entities;
using ParkEase.BookingService.Interfaces;
using ParkEase.BookingService.Services;

namespace ParkEase.Tests.BookingServiceTests;

[TestFixture]
public class BookingServiceTests
{
    private Mock<IBookingRepository> _repoMock = null!;
    private ParkEase.BookingService.Services.BookingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IBookingRepository>();
        _service  = new ParkEase.BookingService.Services.BookingService(_repoMock.Object);
    }

    // ════════════════════════════════════════════════
    // CREATE BOOKING
    // ════════════════════════════════════════════════

    [Test]
    public async Task CreateBooking_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = ValidCreateRequest();
        _repoMock.Setup(r => r.FindActiveBySpotIdAsync(request.SpotId))
                 .ReturnsAsync((Booking?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Booking>()))
                 .ReturnsAsync((Booking b) => { b.BookingId = 1; return b; });

        // Act
        var result = await _service.CreateBookingAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("RESERVED");
        result.Data.VehiclePlate.Should().Be(request.VehiclePlate.ToUpper());
    }

    [Test]
    public async Task CreateBooking_EndTimeBeforeStartTime_ReturnsFail()
    {
        var request = ValidCreateRequest();
        request.EndTime = request.StartTime.AddHours(-1);

        var result = await _service.CreateBookingAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("EndTime must be after StartTime");
    }

    [Test]
    public async Task CreateBooking_StartTimeInPast_ReturnsFail()
    {
        var request = ValidCreateRequest();
        request.StartTime = DateTime.UtcNow.AddHours(-2);
        request.EndTime   = DateTime.UtcNow.AddHours(-1);

        var result = await _service.CreateBookingAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("StartTime cannot be in the past");
    }

    [Test]
    public async Task CreateBooking_ZeroPricePerHour_ReturnsFail()
    {
        var request = ValidCreateRequest();
        request.PricePerHour = 0;

        var result = await _service.CreateBookingAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("PricePerHour must be greater than 0");
    }

    [Test]
    public async Task CreateBooking_SpotAlreadyBooked_ReturnsFail()
    {
        var request = ValidCreateRequest();
        _repoMock.Setup(r => r.FindActiveBySpotIdAsync(request.SpotId))
                 .ReturnsAsync(new Booking { SpotId = request.SpotId, Status = "RESERVED" });

        var result = await _service.CreateBookingAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already reserved or occupied");
    }

    [Test]
    public async Task CreateBooking_FareCalculation_MinimumOneHour()
    {
        // 30 minutes duration — should still charge 1 hour minimum
        var request = ValidCreateRequest();
        request.StartTime    = DateTime.UtcNow.AddMinutes(5);
        request.EndTime      = DateTime.UtcNow.AddMinutes(35); // 30 min
        request.PricePerHour = 100m;

        _repoMock.Setup(r => r.FindActiveBySpotIdAsync(request.SpotId))
                 .ReturnsAsync((Booking?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Booking>()))
                 .ReturnsAsync((Booking b) => b);

        var result = await _service.CreateBookingAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(100m); // minimum 1 hour
    }

    [Test]
    public async Task CreateBooking_FareCalculation_TwoHours()
    {
        var request = ValidCreateRequest();
        request.StartTime    = DateTime.UtcNow.AddMinutes(5);
        request.EndTime      = DateTime.UtcNow.AddHours(2).AddMinutes(5);
        request.PricePerHour = 50m;

        _repoMock.Setup(r => r.FindActiveBySpotIdAsync(request.SpotId))
                 .ReturnsAsync((Booking?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Booking>()))
                 .ReturnsAsync((Booking b) => b);

        var result = await _service.CreateBookingAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.TotalAmount.Should().BeApproximately(100m, 1m); // 2 hours x ₹50
    }

    // ════════════════════════════════════════════════
    // CHECK IN
    // ════════════════════════════════════════════════

    [Test]
    public async Task CheckIn_ReservedBooking_ReturnsActive()
    {
        var booking = ReservedBooking();
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);
        _repoMock.Setup(r => r.CountByLotIdAndStatusAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(1);
        _repoMock.Setup(r => r.CreateOccupancyLogAsync(It.IsAny<OccupancyLog>())).Returns(Task.CompletedTask);

        var result = await _service.CheckInAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("ACTIVE");
        result.Data.CheckInTime.Should().NotBeNull();
    }

    [Test]
    public async Task CheckIn_NonExistentBooking_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByBookingIdAsync(99)).ReturnsAsync((Booking?)null);

        var result = await _service.CheckInAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Booking not found");
    }

    [Test]
    public async Task CheckIn_AlreadyActiveBooking_ReturnsFail()
    {
        var booking = ReservedBooking();
        booking.Status = "ACTIVE";
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.CheckInAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot check in");
    }

    [Test]
    public async Task CheckIn_CancelledBooking_ReturnsFail()
    {
        var booking = ReservedBooking();
        booking.Status = "CANCELLED";
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.CheckInAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot check in");
    }

    // ════════════════════════════════════════════════
    // CHECK OUT
    // ════════════════════════════════════════════════

    [Test]
    public async Task CheckOut_ActiveBooking_ReturnsCompleted()
    {
        var booking = ActiveBooking();
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);
        _repoMock.Setup(r => r.CountByLotIdAndStatusAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(0);
        _repoMock.Setup(r => r.CreateOccupancyLogAsync(It.IsAny<OccupancyLog>())).Returns(Task.CompletedTask);

        var result = await _service.CheckOutAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("COMPLETED");
        result.Data.TotalAmount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task CheckOut_NotActiveBooking_ReturnsFail()
    {
        var booking = ReservedBooking(); // still RESERVED, not checked in
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.CheckOutAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot check out");
    }

    [Test]
    public async Task CheckOut_FareAppliesMinimumOneHour()
    {
        var booking = ActiveBooking();
        booking.CheckInTime  = DateTime.UtcNow.AddMinutes(-20); // only 20 min parked
        booking.PricePerHour = 80m;

        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);
        _repoMock.Setup(r => r.CountByLotIdAndStatusAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(0);
       _repoMock.Setup(r => r.CreateOccupancyLogAsync(It.IsAny<OccupancyLog>())).Returns(Task.CompletedTask);

        var result = await _service.CheckOutAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.TotalAmount.Should().Be(80m); // minimum 1 hour = ₹80
    }

    // ════════════════════════════════════════════════
    // CANCEL
    // ════════════════════════════════════════════════

    [Test]
    public async Task Cancel_ReservedBooking_ReturnsCancelled()
    {
        var booking = ReservedBooking();
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);

        var result = await _service.CancelBookingAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("CANCELLED");
        result.Data.TotalAmount.Should().Be(0);
    }

    [Test]
    public async Task Cancel_CompletedBooking_ReturnsFail()
    {
        var booking = ActiveBooking();
        booking.Status = "COMPLETED";
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.CancelBookingAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot cancel a completed booking");
    }

    [Test]
    public async Task Cancel_AlreadyCancelledBooking_ReturnsFail()
    {
        var booking = ReservedBooking();
        booking.Status = "CANCELLED";
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.CancelBookingAsync(1);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already cancelled");
    }

    // ════════════════════════════════════════════════
    // EXTEND
    // ════════════════════════════════════════════════

    [Test]
    public async Task Extend_ValidNewTime_ReturnsSuccess()
    {
        var booking = ReservedBooking();
        var newEndTime = booking.EndTime.AddHours(2);
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Booking>())).ReturnsAsync((Booking b) => b);

        var result = await _service.ExtendBookingAsync(1, new ExtendBookingRequest { NewEndTime = newEndTime });

        result.Success.Should().BeTrue();
        result.Data!.EndTime.Should().Be(newEndTime);
    }

    [Test]
    public async Task Extend_NewTimeBeforeCurrentEndTime_ReturnsFail()
    {
        var booking = ReservedBooking();
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.ExtendBookingAsync(1,
            new ExtendBookingRequest { NewEndTime = booking.EndTime.AddHours(-1) });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("New end time must be after current end time");
    }

    [Test]
    public async Task Extend_CompletedBooking_ReturnsFail()
    {
        var booking = ReservedBooking();
        booking.Status = "COMPLETED";
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(booking);

        var result = await _service.ExtendBookingAsync(1,
            new ExtendBookingRequest { NewEndTime = booking.EndTime.AddHours(2) });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot extend a completed or cancelled booking");
    }

    // ════════════════════════════════════════════════
    // OCCUPANCY
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetOccupancyRate_CalculatesCorrectly()
    {
        _repoMock.Setup(r => r.CountByLotIdAndStatusAsync(1, "ACTIVE")).ReturnsAsync(3);
        _repoMock.Setup(r => r.CountByLotIdAndStatusAsync(1, "RESERVED")).ReturnsAsync(2);

        var result = await _service.GetOccupancyRateAsync(1, totalSpots: 10);

        result.Success.Should().BeTrue();
        result.Data!.OccupiedSpots.Should().Be(5);
        result.Data.OccupancyRate.Should().Be(50.0);
    }

    [Test]
    public async Task GetOccupancyRate_ZeroTotalSpots_ReturnsZeroRate()
    {
        _repoMock.Setup(r => r.CountByLotIdAndStatusAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(0);

        var result = await _service.GetOccupancyRateAsync(1, totalSpots: 0);

        result.Success.Should().BeTrue();
        result.Data!.OccupancyRate.Should().Be(0);
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static CreateBookingRequest ValidCreateRequest() => new()
    {
        UserId       = 1,
        LotId        = 1,
        SpotId       = 1,
        VehiclePlate = "MH12AB1234",
        VehicleType  = "4W",
        BookingType  = "PRE",
        StartTime    = DateTime.UtcNow.AddMinutes(5),
        EndTime      = DateTime.UtcNow.AddHours(2),
        PricePerHour = 50m
    };

    private static Booking ReservedBooking() => new()
    {
        BookingId    = 1,
        UserId       = 1,
        LotId        = 1,
        SpotId       = 1,
        VehiclePlate = "MH12AB1234",
        VehicleType  = "4W",
        BookingType  = "PRE",
        StartTime    = DateTime.UtcNow.AddMinutes(-30),
        EndTime      = DateTime.UtcNow.AddHours(2),
        Status       = "RESERVED",
        PricePerHour = 50m,
        TotalAmount  = 100m,
        CreatedAt    = DateTime.UtcNow.AddMinutes(-30)
    };

    private static Booking ActiveBooking()
    {
        var b = ReservedBooking();
        b.Status      = "ACTIVE";
        b.CheckInTime = DateTime.UtcNow.AddHours(-1);
        return b;
    }
}
