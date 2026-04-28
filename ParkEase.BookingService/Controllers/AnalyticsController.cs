using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.BookingService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.BookingService.Controllers;

/// <summary>
/// Analytics and reporting — occupancy, revenue, peak hours, platform summary.
/// Included inside Booking Service since all data lives here.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public AnalyticsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>Get real-time occupancy rate for a lot</summary>
    [HttpGet("occupancy/{lotId}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Get occupancy rate",
        Description = "Returns real-time occupancy % = (occupied + reserved spots / totalSpots) × 100."
    )]
    [SwaggerResponse(200, "Occupancy data returned")]
    public async Task<IActionResult> GetOccupancyRate(int lotId, [FromQuery] int totalSpots)
    {
        var result = await _bookingService.GetOccupancyRateAsync(lotId, totalSpots);
        return Ok(result);
    }

    /// <summary>Get revenue report for a lot over a date range</summary>
    [HttpGet("revenue/{lotId}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Get revenue by lot",
        Description = "Returns total revenue and daily breakdown for a lot between from and to dates."
    )]
    [SwaggerResponse(200, "Revenue report returned")]
    public async Task<IActionResult> GetRevenue(
        int lotId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var result = await _bookingService.GetRevenueAsync(lotId, from, to);
        return Ok(result);
    }

    /// <summary>Get peak hours analysis for a lot</summary>
    [HttpGet("peak-hours/{lotId}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Get peak hours",
        Description = "Identifies busiest check-in hours for a lot based on completed bookings."
    )]
    [SwaggerResponse(200, "Peak hours data returned")]
    public async Task<IActionResult> GetPeakHours(int lotId)
    {
        var result = await _bookingService.GetPeakHoursAsync(lotId);
        return Ok(result);
    }

    /// <summary>Get platform-wide summary for admin dashboard</summary>
    [HttpGet("platform-summary")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(
        Summary = "Get platform summary (Admin only)",
        Description = "Returns total bookings, revenue, active bookings, and average parking duration across all lots."
    )]
    [SwaggerResponse(200, "Platform summary returned")]
    public async Task<IActionResult> GetPlatformSummary()
    {
        var result = await _bookingService.GetPlatformSummaryAsync();
        return Ok(result);
    }
}
