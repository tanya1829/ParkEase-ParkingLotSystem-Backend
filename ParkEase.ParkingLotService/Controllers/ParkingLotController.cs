using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.ParkingLotService.DTOs;
using ParkEase.ParkingLotService.Interfaces;

namespace ParkEase.ParkingLotService.Controllers;

[ApiController]
[Route("api/v1/lots")]
public class ParkingLotController : ControllerBase
{
    private readonly IParkingLotService _lotService;

    public ParkingLotController(IParkingLotService lotService)
    {
        _lotService = lotService;
    }

    // POST /api/v1/lots
    // Lot Manager registers a new parking lot
    [HttpPost]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> CreateLot([FromBody] CreateLotRequest request)
    {
        var result = await _lotService.CreateLotAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/v1/lots/{id}
    // Anyone can view lot details
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLotById(int id)
    {
        var result = await _lotService.GetLotByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // GET /api/v1/lots/city/{city}
    // Search lots by city — available to guests too
    [HttpGet("city/{city}")]
    public async Task<IActionResult> GetLotsByCity(string city)
    {
        var result = await _lotService.GetLotsByCityAsync(city);
        return Ok(result);
    }

    // GET /api/v1/lots/nearby?lat=28.6&lng=77.2&radiusKm=5
    // GPS-based nearby lot search
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyLots(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 5.0)
    {
        var result = await _lotService.GetNearbyLotsAsync(lat, lng, radiusKm);
        return Ok(result);
    }

    // GET /api/v1/lots/manager/{managerId}
    // Lot Manager views their own lots
    [HttpGet("manager/{managerId}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> GetLotsByManager(int managerId)
    {
        var result = await _lotService.GetLotsByManagerAsync(managerId);
        return Ok(result);
    }

    // GET /api/v1/lots/search?keyword=mall
    // Search by name, city, or address
    [HttpGet("search")]
    public async Task<IActionResult> SearchLots([FromQuery] string keyword)
    {
        var result = await _lotService.SearchLotsAsync(keyword);
        return Ok(result);
    }

    // GET /api/v1/lots/pending
    // Admin views lots pending approval
    [HttpGet("pending")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetPendingApproval()
    {
        var result = await _lotService.GetPendingApprovalAsync();
        return Ok(result);
    }

    // GET /api/v1/lots
    // Admin views all lots
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAllLots()
    {
        var result = await _lotService.GetAllLotsAsync();
        return Ok(result);
    }

    // PUT /api/v1/lots/{id}
    // Lot Manager updates lot details
    [HttpPut("{id}")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateLot(int id, [FromBody] UpdateLotRequest request)
    {
        var result = await _lotService.UpdateLotAsync(id, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/lots/{id}/toggle
    // Lot Manager toggles lot open/closed
    [HttpPut("{id}/toggle")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> ToggleOpen(int id)
    {
        var result = await _lotService.ToggleOpenAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/lots/{id}/approve
    // Admin approves a lot registration
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ApproveLot(int id)
    {
        var result = await _lotService.ApproveLotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/lots/{id}/reject
    // Admin rejects a lot registration
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> RejectLot(int id)
    {
        var result = await _lotService.RejectLotAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/lots/{id}/decrement
    // Called by Booking Service when a spot is reserved
    [HttpPut("{id}/decrement")]
    [Authorize]
    public async Task<IActionResult> DecrementAvailable(int id)
    {
        var result = await _lotService.DecrementAvailableAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/v1/lots/{id}/increment
    // Called by Booking Service when a booking is cancelled/checked out
    [HttpPut("{id}/increment")]
    [Authorize]
    public async Task<IActionResult> IncrementAvailable(int id)
    {
        var result = await _lotService.IncrementAvailableAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/v1/lots/{id}
    // Admin deletes a lot
    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> DeleteLot(int id)
    {
        var result = await _lotService.DeleteLotAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
