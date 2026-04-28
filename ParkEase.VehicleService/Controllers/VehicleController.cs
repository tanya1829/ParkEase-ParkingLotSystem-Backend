using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.VehicleService.DTOs;
using ParkEase.VehicleService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.VehicleService.Controllers;

/// <summary>
/// Manages vehicle registration and lookup for drivers.
/// A driver can register multiple vehicles and select one at booking time.
/// </summary>
[ApiController]
[Route("api/v1/vehicles")]
[Produces("application/json")]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehicleController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    // ─── DRIVER ENDPOINTS ─────────────────────────────────────────────────────

    /// <summary>Register a new vehicle for a driver</summary>
    [HttpPost]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Register a vehicle",
        Description = "Driver registers a vehicle with license plate, make, model, color, type (2W/4W/HEAVY), and EV flag. Same plate cannot be registered twice for the same owner."
    )]
    [SwaggerResponse(200, "Vehicle registered successfully")]
    [SwaggerResponse(400, "Duplicate license plate or invalid vehicle type")]
    public async Task<IActionResult> RegisterVehicle([FromBody] RegisterVehicleRequest request)
    {
        var result = await _vehicleService.RegisterVehicleAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get vehicle details by ID</summary>
    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get vehicle by ID",
        Description = "Returns full details of a specific vehicle including type, EV status, and owner."
    )]
    [SwaggerResponse(200, "Vehicle details returned")]
    [SwaggerResponse(404, "Vehicle not found")]
    public async Task<IActionResult> GetVehicleById(int id)
    {
        var result = await _vehicleService.GetVehicleByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get all vehicles registered by a driver</summary>
    [HttpGet("owner/{ownerId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get vehicles by owner",
        Description = "Returns all active vehicles registered by the specified driver (OwnerId = UserId from Auth Service)."
    )]
    [SwaggerResponse(200, "List of vehicles for the owner")]
    public async Task<IActionResult> GetVehiclesByOwner(int ownerId)
    {
        var result = await _vehicleService.GetVehiclesByOwnerAsync(ownerId);
        return Ok(result);
    }

    /// <summary>Look up a vehicle by license plate number</summary>
    [HttpGet("plate/{licensePlate}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get vehicle by license plate",
        Description = "Finds a vehicle using its license plate number. Used by Booking Service to validate vehicle at check-in."
    )]
    [SwaggerResponse(200, "Vehicle found")]
    [SwaggerResponse(404, "No vehicle with that plate number")]
    public async Task<IActionResult> GetByLicensePlate(string licensePlate)
    {
        var result = await _vehicleService.GetByLicensePlateAsync(licensePlate);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Update vehicle details</summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Update vehicle details",
        Description = "Update vehicle make, model, color, type, or EV flag. License plate cannot be changed after registration."
    )]
    [SwaggerResponse(200, "Vehicle updated successfully")]
    [SwaggerResponse(404, "Vehicle not found")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleRequest request)
    {
        var result = await _vehicleService.UpdateVehicleAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove a vehicle from the driver's account</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Delete a vehicle",
        Description = "Soft-deletes the vehicle (marks as inactive). The vehicle record is kept for booking history."
    )]
    [SwaggerResponse(200, "Vehicle removed successfully")]
    [SwaggerResponse(404, "Vehicle not found")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        var result = await _vehicleService.DeleteVehicleAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get vehicle type (2W, 4W, or HEAVY)</summary>
    [HttpGet("{id}/type")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get vehicle type",
        Description = "Returns the vehicle type: 2W (two-wheeler), 4W (four-wheeler), or HEAVY. Used by Booking Service for spot matching."
    )]
    [SwaggerResponse(200, "Vehicle type returned")]
    [SwaggerResponse(404, "Vehicle not found")]
    public async Task<IActionResult> GetVehicleType(int id)
    {
        var result = await _vehicleService.GetVehicleTypeAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Check if a vehicle is an electric vehicle (EV)</summary>
    [HttpGet("{id}/isev")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Check if vehicle is EV",
        Description = "Returns true/false indicating whether the vehicle is electric. Used by Booking Service to match EV-charging spots."
    )]
    [SwaggerResponse(200, "EV status returned")]
    [SwaggerResponse(404, "Vehicle not found")]
    public async Task<IActionResult> IsEV(int id)
    {
        var result = await _vehicleService.IsEVVehicleAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ─── ADMIN ENDPOINTS ──────────────────────────────────────────────────────

    /// <summary>Get all vehicles platform-wide (Admin only)</summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(
        Summary = "Get all vehicles (Admin only)",
        Description = "Returns every vehicle registered on the platform including inactive ones."
    )]
    [SwaggerResponse(200, "Complete list of all vehicles")]
    public async Task<IActionResult> GetAllVehicles()
    {
        var result = await _vehicleService.GetAllVehiclesAsync();
        return Ok(result);
    }
}
