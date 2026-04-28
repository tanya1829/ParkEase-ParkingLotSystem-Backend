using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.SpotService.DTOs;
using ParkEase.SpotService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.SpotService.Controllers;

/// <summary>
/// Manages parking spots within lots — add, update, filter, and control spot status.
/// </summary>
[ApiController]
[Route("api/v1/spots")]
[Produces("application/json")]
public class SpotController : ControllerBase
{
    private readonly ISpotService _spotService;

    public SpotController(ISpotService spotService)
    {
        _spotService = spotService;
    }

    // ─── LOT MANAGER ENDPOINTS ────────────────────────────────────────────────

    /// <summary>Add a single parking spot to a lot</summary>
    [HttpPost]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Add a single spot",
        Description = "Lot Manager adds one parking spot to a lot. SpotType: COMPACT, STANDARD, LARGE, MOTORBIKE, EV. VehicleType: 2W, 4W, HEAVY."
    )]
    [SwaggerResponse(200, "Spot added successfully")]
    [SwaggerResponse(400, "Invalid spot type, vehicle type, or duplicate spot number")]
    public async Task<IActionResult> AddSpot([FromBody] AddSpotRequest request)
    {
        var result = await _spotService.AddSpotAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Bulk-create multiple spots for a lot at once</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Bulk add spots",
        Description = "Creates multiple spots at once. E.g. prefix='A', count=10 creates A1 to A10. Max 500 spots per request."
    )]
    [SwaggerResponse(200, "Spots created successfully")]
    [SwaggerResponse(400, "Invalid request or all spot numbers already exist")]
    public async Task<IActionResult> AddBulkSpots([FromBody] AddBulkSpotsRequest request)
    {
        var result = await _spotService.AddBulkSpotsAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Update spot details like price, type, or EV/handicapped flags</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Update spot details",
        Description = "Update spot type, vehicle type, price per hour, EV charging flag, or handicapped flag."
    )]
    [SwaggerResponse(200, "Spot updated successfully")]
    [SwaggerResponse(404, "Spot not found")]
    public async Task<IActionResult> UpdateSpot(int id, [FromBody] UpdateSpotRequest request)
    {
        var result = await _spotService.UpdateSpotAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete a spot (only if currently available)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    [SwaggerOperation(
        Summary = "Delete a spot",
        Description = "Permanently deletes a spot. Cannot delete if spot is currently Reserved or Occupied."
    )]
    [SwaggerResponse(200, "Spot deleted successfully")]
    [SwaggerResponse(400, "Cannot delete — spot is reserved or occupied")]
    public async Task<IActionResult> DeleteSpot(int id)
    {
        var result = await _spotService.DeleteSpotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ─── DRIVER / PUBLIC ENDPOINTS ────────────────────────────────────────────

    /// <summary>Get spot details by ID</summary>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get spot by ID",
        Description = "Returns full details of a specific parking spot including status and pricing."
    )]
    [SwaggerResponse(200, "Spot details returned")]
    [SwaggerResponse(404, "Spot not found")]
    public async Task<IActionResult> GetSpotById(int id)
    {
        var result = await _spotService.GetSpotByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get all spots in a lot with floor-level breakdown</summary>
    [HttpGet("lot/{lotId}")]
    [SwaggerOperation(
        Summary = "Get all spots by lot",
        Description = "Returns all parking spots in a lot ordered by floor and spot number."
    )]
    [SwaggerResponse(200, "List of all spots in the lot")]
    public async Task<IActionResult> GetSpotsByLot(int lotId)
    {
        var result = await _spotService.GetSpotsByLotAsync(lotId);
        return Ok(result);
    }

    /// <summary>Get only available spots in a lot</summary>
    [HttpGet("lot/{lotId}/available")]
    [SwaggerOperation(
        Summary = "Get available spots",
        Description = "Returns only spots with AVAILABLE status in the specified lot."
    )]
    [SwaggerResponse(200, "List of available spots")]
    public async Task<IActionResult> GetAvailableSpots(int lotId)
    {
        var result = await _spotService.GetAvailableSpotsByLotAsync(lotId);
        return Ok(result);
    }

    /// <summary>Filter spots by type (COMPACT, STANDARD, LARGE, MOTORBIKE, EV)</summary>
    [HttpGet("lot/{lotId}/type/{spotType}")]
    [SwaggerOperation(
        Summary = "Get spots by type",
        Description = "Filter spots in a lot by SpotType: COMPACT, STANDARD, LARGE, MOTORBIKE, or EV."
    )]
    [SwaggerResponse(200, "Filtered list of spots")]
    public async Task<IActionResult> GetByType(int lotId, string spotType)
    {
        var result = await _spotService.GetByTypeAndLotAsync(lotId, spotType);
        return Ok(result);
    }

    /// <summary>Filter spots by vehicle type (2W, 4W, HEAVY)</summary>
    [HttpGet("lot/{lotId}/vehicle/{vehicleType}")]
    [SwaggerOperation(
        Summary = "Get spots by vehicle type",
        Description = "Filter spots compatible with a specific vehicle type: 2W (two-wheeler), 4W (four-wheeler), HEAVY."
    )]
    [SwaggerResponse(200, "Filtered list of spots")]
    public async Task<IActionResult> GetByVehicleType(int lotId, string vehicleType)
    {
        var result = await _spotService.GetByVehicleTypeAsync(lotId, vehicleType);
        return Ok(result);
    }

    /// <summary>Get count of available spots in a lot</summary>
    [HttpGet("lot/{lotId}/count")]
    [SwaggerOperation(
        Summary = "Count available spots",
        Description = "Returns the number of currently available spots in a lot."
    )]
    [SwaggerResponse(200, "Available spot count")]
    public async Task<IActionResult> CountAvailable(int lotId)
    {
        var result = await _spotService.CountAvailableAsync(lotId);
        return Ok(result);
    }

    // ─── INTERNAL ENDPOINTS (called by Booking Service) ──────────────────────

    /// <summary>Reserve a spot — called by Booking Service on booking creation</summary>
    [HttpPut("{id}/reserve")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Reserve a spot (internal)",
        Description = "Transitions spot from AVAILABLE → RESERVED. Called by Booking Service when a booking is created."
    )]
    [SwaggerResponse(200, "Spot reserved successfully")]
    [SwaggerResponse(400, "Spot not available")]
    public async Task<IActionResult> ReserveSpot(int id)
    {
        var result = await _spotService.ReserveSpotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Occupy a spot — called by Booking Service on check-in</summary>
    [HttpPut("{id}/occupy")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Occupy a spot (internal)",
        Description = "Transitions spot from RESERVED → OCCUPIED. Called by Booking Service when driver checks in."
    )]
    [SwaggerResponse(200, "Spot occupied successfully")]
    [SwaggerResponse(400, "Spot already occupied")]
    public async Task<IActionResult> OccupySpot(int id)
    {
        var result = await _spotService.OccupySpotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Release a spot — called by Booking Service on checkout or cancellation</summary>
    [HttpPut("{id}/release")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Release a spot (internal)",
        Description = "Transitions spot back to AVAILABLE. Called by Booking Service on checkout or cancellation."
    )]
    [SwaggerResponse(200, "Spot released and available")]
    public async Task<IActionResult> ReleaseSpot(int id)
    {
        var result = await _spotService.ReleaseSpotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
