using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.PaymentService.Controllers;

/// <summary>
/// Handles payment processing, Razorpay integration, refunds, and transaction history.
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

    // ════════════════════════════════════════════════
    // RAZORPAY ENDPOINTS
    // ════════════════════════════════════════════════

    /// <summary>Create a Razorpay order — Step 1 of online payment</summary>
    [HttpPost("create-order")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Create Razorpay order",
        Description = "Creates an order on Razorpay servers. Returns orderId and keyId needed to open the Razorpay checkout modal on the frontend."
    )]
    [SwaggerResponse(200, "Order created — returns orderId, keyId, amount")]
    [SwaggerResponse(400, "Invalid amount or Razorpay error")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _paymentService.CreateRazorpayOrderAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Verify Razorpay payment signature — Step 2 of online payment</summary>
    [HttpPost("verify")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Verify and record Razorpay payment",
        Description = "Verifies the HMAC-SHA256 signature from Razorpay, then records the payment as PAID in the database."
    )]
    [SwaggerResponse(200, "Payment verified and recorded")]
    [SwaggerResponse(400, "Signature invalid or payment already recorded")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        var result = await _paymentService.VerifyAndRecordPaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // EXISTING ENDPOINTS (unchanged)
    // ════════════════════════════════════════════════

    /// <summary>Process a direct payment (Cash or internal)</summary>
    [HttpPost]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(
        Summary = "Process a payment",
        Description = "Process parking fee payment directly. Mode: CARD, UPI, WALLET, or CASH. Use /create-order + /verify for Razorpay online payments."
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
    [SwaggerOperation(Summary = "Get payment by booking", Description = "Returns the payment record linked to a specific booking.")]
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
    [SwaggerOperation(Summary = "Get payment by ID")]
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
    [SwaggerOperation(Summary = "Get payments by user")]
    [SwaggerResponse(200, "List of payments")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _paymentService.GetByUserIdAsync(userId);
        return Ok(result);
    }

    /// <summary>Get full transaction history for a user</summary>
    [HttpGet("user/{userId}/history")]
    [Authorize]
    [SwaggerOperation(Summary = "Get transaction history")]
    [SwaggerResponse(200, "Transaction history returned")]
    public async Task<IActionResult> GetTransactionHistory(int userId)
    {
        var result = await _paymentService.GetTransactionHistoryAsync(userId);
        return Ok(result);
    }

    /// <summary>Get payment status</summary>
    [HttpGet("{id}/status")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payment status", Description = "Returns current status: PENDING, PAID, REFUNDED, or FAILED.")]
    [SwaggerResponse(200, "Status returned")]
    [SwaggerResponse(404, "Payment not found")]
    public async Task<IActionResult> GetPaymentStatus(int id)
    {
        var result = await _paymentService.GetPaymentStatusAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Refund a payment</summary>
    [HttpPost("refund")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(Summary = "Process a refund", Description = "Refunds a PAID payment. Status changes to REFUNDED.")]
    [SwaggerResponse(200, "Refund processed successfully")]
    [SwaggerResponse(400, "Payment not in PAID status")]
    public async Task<IActionResult> RefundPayment([FromBody] RefundRequest request)
    {
        var result = await _paymentService.RefundPaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}