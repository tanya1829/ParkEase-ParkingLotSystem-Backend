using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.PaymentService.Controllers;

/// <summary>
/// Handles payment processing, refunds, and transaction history for parking bookings.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Process payment for a booking</summary>
    [HttpPost]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Process a payment",
        Description = "Process parking fee payment for a booking. Mode: CARD, UPI, WALLET, or CASH. For non-cash a TransactionId is auto-generated."
    )]
    [SwaggerResponse(200, "Payment processed successfully")]
    [SwaggerResponse(400, "Invalid payment mode or already paid")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var result = await _paymentService.ProcessPaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Get payment by booking ID</summary>
    [HttpGet("booking/{bookingId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get payment by booking",
        Description = "Returns the payment record linked to a specific booking."
    )]
    [SwaggerResponse(200, "Payment details returned")]
    [SwaggerResponse(404, "No payment found for this booking")]
    public async Task<IActionResult> GetByBookingId(int bookingId)
    {
        var result = await _paymentService.GetByBookingIdAsync(bookingId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get payment by payment ID</summary>
    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get payment by ID",
        Description = "Returns full payment details including status, mode, and timestamps."
    )]
    [SwaggerResponse(200, "Payment returned")]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> GetByPaymentId(int id)
    {
        var result = await _paymentService.GetByPaymentIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Get all payments for a user</summary>
    [HttpGet("user/{userId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get payments by user",
        Description = "Returns all payment transactions for a driver ordered by most recent first."
    )]
    [SwaggerResponse(200, "List of payments")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _paymentService.GetByUserIdAsync(userId);
        return Ok(result);
    }

    /// <summary>Get full transaction history for a user</summary>
    [HttpGet("user/{userId}/history")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get transaction history",
        Description = "Returns complete payment history for a driver including paid, refunded, and failed transactions."
    )]
    [SwaggerResponse(200, "Transaction history returned")]
    public async Task<IActionResult> GetTransactionHistory(int userId)
    {
        var result = await _paymentService.GetTransactionHistoryAsync(userId);
        return Ok(result);
    }

    /// <summary>Get payment status</summary>
    [HttpGet("{id}/status")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get payment status",
        Description = "Returns current status: PENDING, PAID, REFUNDED, or FAILED."
    )]
    [SwaggerResponse(200, "Status returned")]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> GetPaymentStatus(int id)
    {
        var result = await _paymentService.GetPaymentStatusAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Refund a payment for a cancelled booking</summary>
    [HttpPost("refund")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Process a refund",
        Description = "Refunds a PAID payment. Only applicable for eligible cancellations. Status changes to REFUNDED."
    )]
    [SwaggerResponse(200, "Refund processed successfully")]
    [SwaggerResponse(400, "Payment not in PAID status")]
    public async Task<IActionResult> RefundPayment([FromBody] RefundRequest request)
    {
        var result = await _paymentService.RefundPaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
