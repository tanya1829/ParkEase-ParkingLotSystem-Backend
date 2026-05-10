using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.BookingService.DTOs;
using ParkEase.BookingService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.BookingService.Controllers;

/// <summary>
/// Manages complete parking booking lifecycle — create, check-in, check-out, cancel, extend.
/// </summary>
[ApiController]
[Route("api/v1/bookings")]
[Produces("application/json")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Create a new parking booking</summary>
    [HttpPost]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Create a booking",
        Description = "Driver reserves a parking spot for a time window. BookingType: PRE (advance) or WALK_IN (immediate). Fare is estimated at creation."
    )]
    [SwaggerResponse(200, "Booking created successfully")]
    [SwaggerResponse(400, "Spot already booked or invalid time range")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        var result = await _bookingService.CreateBookingAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get booking details by ID</summary>
    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get booking by ID",
        Description = "Returns full booking details including status, times, and fare."
    )]
    [SwaggerResponse(200, "Booking details returned")]
    [SwaggerResponse(404, "Booking not found")]
    public async Task<IActionResult> GetBookingById(int id)
    {
        var result = await _bookingService.GetBookingByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get all bookings for a driver</summary>
    [HttpGet("user/{userId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get bookings by user",
        Description = "Returns all bookings made by the specified driver, ordered by most recent first."
    )]
    [SwaggerResponse(200, "List of bookings")]
    public async Task<IActionResult> GetBookingsByUser(int userId)
    {
        var result = await _bookingService.GetBookingsByUserAsync(userId);
        return Ok(result);
    }

    /// <summary>Get all bookings for a parking lot</summary>
    [HttpGet("lot/{lotId}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Get bookings by lot",
        Description = "Lot Manager views all bookings for their lot including active, completed, and cancelled."
    )]
    [SwaggerResponse(200, "List of bookings for the lot")]
    public async Task<IActionResult> GetBookingsByLot(int lotId)
    {
        var result = await _bookingService.GetBookingsByLotAsync(lotId);
        return Ok(result);
    }

    /// <summary>Get currently active bookings for a lot</summary>
    [HttpGet("lot/{lotId}/active")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Get active bookings",
        Description = "Returns only RESERVED and ACTIVE bookings for a lot — currently occupied or upcoming."
    )]
    [SwaggerResponse(200, "List of active bookings")]
    public async Task<IActionResult> GetActiveBookings(int lotId)
    {
        var result = await _bookingService.GetActiveBookingsAsync(lotId);
        return Ok(result);
    }

    /// <summary>Get booking history for a driver</summary>
    [HttpGet("user/{userId}/history")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get booking history",
        Description = "Returns completed and cancelled bookings for a driver."
    )]
    [SwaggerResponse(200, "Booking history")]
    public async Task<IActionResult> GetBookingHistory(int userId)
    {
        var result = await _bookingService.GetBookingHistoryAsync(userId);
        return Ok(result);
    }

    /// <summary>Cancel a booking</summary>
    [HttpPut("{id}/cancel")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Cancel a booking",
        Description = "Cancels a RESERVED or ACTIVE booking. Cannot cancel completed bookings."
    )]
    [SwaggerResponse(200, "Booking cancelled successfully")]
    [SwaggerResponse(400, "Cannot cancel completed booking")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var result = await _bookingService.CancelBookingAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Check in to a spot — marks arrival</summary>
    [HttpPut("{id}/checkin")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Check in to spot",
        Description = "Driver confirms arrival. Transitions booking: RESERVED → ACTIVE. Spot status becomes OCCUPIED."
    )]
    [SwaggerResponse(200, "Check-in successful")]
    [SwaggerResponse(400, "Booking not in RESERVED status")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var result = await _bookingService.CheckInAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Check out — triggers fare calculation</summary>
    [HttpPut("{id}/checkout")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Check out of spot",
        Description = "Driver confirms departure. Fare = (CheckOut - CheckIn hours) × PricePerHour. Minimum 1 hour charge. Transitions: ACTIVE → COMPLETED."
    )]
    [SwaggerResponse(200, "Check-out successful with final fare")]
    [SwaggerResponse(400, "Booking not in ACTIVE status")]
    public async Task<IActionResult> CheckOut(int id)
    {
        var result = await _bookingService.CheckOutAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Extend booking duration</summary>
    [HttpPut("{id}/extend")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Extend booking",
        Description = "Extends the booking end time. Fare is recalculated for the new duration."
    )]
    [SwaggerResponse(200, "Booking extended successfully")]
    [SwaggerResponse(400, "New end time must be after current end time")]
    public async Task<IActionResult> ExtendBooking(int id, [FromBody] ExtendBookingRequest request)
    {
        var result = await _bookingService.ExtendBookingAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Calculate fare for a booking</summary>
    [HttpGet("{id}/fare")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Calculate fare",
        Description = "Calculates current fare for a booking. For active bookings, uses current time as checkout."
    )]
    [SwaggerResponse(200, "Fare calculation returned")]
    [SwaggerResponse(404, "Booking not found")]
    public async Task<IActionResult> CalculateFare(int id)
    {
        var result = await _bookingService.CalculateAmountAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
