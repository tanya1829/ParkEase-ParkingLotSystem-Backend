using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.NotificationService.DTOs;
using ParkEase.NotificationService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.NotificationService.Controllers;

/// <summary>
/// Manages in-app notifications — send, read, mark as read, and delete.
/// Supports APP, EMAIL (stub), and SMS (stub) channels.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notifService;

    public NotificationController(INotificationService notifService)
    {
        _notifService = notifService;
    }

    /// <summary>Send a notification to a user</summary>
    [HttpPost]
    [Authorize(Roles = "ADMIN,MANAGER")]
    [SwaggerOperation(
        Summary = "Send a notification",
        Description = "Send a notification to a user. Type: BOOKING, CHECKIN, CHECKOUT, PAYMENT, EXPIRY, PROMO. Channel: APP, EMAIL, SMS."
    )]
    [SwaggerResponse(200, "Notification sent successfully")]
    [SwaggerResponse(400, "Invalid type or channel")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
    {
        var result = await _notifService.SendAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Send bulk notifications to multiple users (Admin broadcast)</summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(
        Summary = "Send bulk notifications (Admin only)",
        Description = "Broadcast a notification to multiple users at once. Used for platform-wide alerts."
    )]
    [SwaggerResponse(200, "Bulk notifications sent")]
    [SwaggerResponse(400, "Empty recipient list")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkNotificationRequest request)
    {
        var result = await _notifService.SendBulkAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get all notifications for a user</summary>
    [HttpGet("user/{recipientId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get notifications by user",
        Description = "Returns all notifications for a user ordered by most recent first."
    )]
    [SwaggerResponse(200, "List of notifications")]
    public async Task<IActionResult> GetByRecipient(int recipientId)
    {
        var result = await _notifService.GetByRecipientAsync(recipientId);
        return Ok(result);
    }

    /// <summary>Get unread notification count (for notification bell badge)</summary>
    [HttpGet("user/{recipientId}/unread-count")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get unread notification count",
        Description = "Returns count of unread notifications. Used to show the badge number on the notification bell icon."
    )]
    [SwaggerResponse(200, "Unread count returned")]
    public async Task<IActionResult> GetUnreadCount(int recipientId)
    {
        var result = await _notifService.GetUnreadCountAsync(recipientId);
        return Ok(result);
    }

    /// <summary>Mark a single notification as read</summary>
    [HttpPut("{id}/read")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Mark notification as read",
        Description = "Marks a specific notification as read."
    )]
    [SwaggerResponse(200, "Notification marked as read")]
    [SwaggerResponse(404, "Notification not found")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notifService.MarkAsReadAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Mark all notifications as read for a user</summary>
    [HttpPut("user/{recipientId}/read-all")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Mark all notifications as read",
        Description = "Marks all unread notifications for a user as read at once."
    )]
    [SwaggerResponse(200, "All notifications marked as read")]
    public async Task<IActionResult> MarkAllRead(int recipientId)
    {
        var result = await _notifService.MarkAllReadAsync(recipientId);
        return Ok(result);
    }

    /// <summary>Delete a notification</summary>
    [HttpDelete("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Delete a notification",
        Description = "Permanently deletes a notification."
    )]
    [SwaggerResponse(200, "Notification deleted")]
    [SwaggerResponse(404, "Notification not found")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var result = await _notifService.DeleteNotificationAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get all notifications platform-wide (Admin only)</summary>
    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(
        Summary = "Get all notifications (Admin only)",
        Description = "Returns all notifications across the platform for admin oversight."
    )]
    [SwaggerResponse(200, "All notifications returned")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _notifService.GetAllAsync();
        return Ok(result);
    }

    // ── Predefined notification trigger endpoints ──

    /// <summary>Trigger booking confirmation notification</summary>
    [HttpPost("trigger/booking-confirmation")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Trigger booking confirmation",
        Description = "Sends a booking confirmation notification to the driver."
    )]
    public async Task<IActionResult> TriggerBookingConfirmation(
        [FromQuery] int userId,
        [FromQuery] int bookingId,
        [FromQuery] string spotNumber)
    {
        await _notifService.SendBookingConfirmationAsync(userId, bookingId, spotNumber);
        return Ok(new { Success = true, Message = "Booking confirmation sent." });
    }

    /// <summary>Trigger check-in alert notification</summary>
    [HttpPost("trigger/checkin")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Trigger check-in alert",
        Description = "Sends a check-in confirmation notification to the driver."
    )]
    public async Task<IActionResult> TriggerCheckIn(
        [FromQuery] int userId,
        [FromQuery] int bookingId,
        [FromQuery] string spotNumber)
    {
        await _notifService.SendCheckInAlertAsync(userId, bookingId, spotNumber);
        return Ok(new { Success = true, Message = "Check-in alert sent." });
    }

    /// <summary>Trigger checkout confirmation notification</summary>
    [HttpPost("trigger/checkout")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Trigger checkout confirmation",
        Description = "Sends checkout confirmation with total fare to the driver."
    )]
    public async Task<IActionResult> TriggerCheckOut(
        [FromQuery] int userId,
        [FromQuery] int bookingId,
        [FromQuery] decimal totalAmount)
    {
        await _notifService.SendCheckOutConfirmationAsync(userId, bookingId, totalAmount);
        return Ok(new { Success = true, Message = "Checkout confirmation sent." });
    }

    /// <summary>Trigger payment receipt notification</summary>
    [HttpPost("trigger/payment")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Trigger payment receipt",
        Description = "Sends payment receipt notification to the driver."
    )]
    public async Task<IActionResult> TriggerPaymentReceipt(
        [FromQuery] int userId,
        [FromQuery] int paymentId,
        [FromQuery] decimal amount,
        [FromQuery] string mode)
    {
        await _notifService.SendPaymentReceiptAsync(userId, paymentId, amount, mode);
        return Ok(new { Success = true, Message = "Payment receipt sent." });
    }

    /// <summary>Trigger expiry reminder notification</summary>
    [HttpPost("trigger/expiry-reminder")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Trigger expiry reminder",
        Description = "Sends 15-minute expiry reminder to the driver."
    )]
    public async Task<IActionResult> TriggerExpiryReminder(
        [FromQuery] int userId,
        [FromQuery] int bookingId,
        [FromQuery] DateTime endTime)
    {
        await _notifService.SendExpiryReminderAsync(userId, bookingId, endTime);
        return Ok(new { Success = true, Message = "Expiry reminder sent." });
    }
}
