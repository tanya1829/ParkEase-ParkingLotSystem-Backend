using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace ParkEase.PaymentService.Controllers;

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

    [HttpPost("create-order")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(Summary = "Create Razorpay order", Description = "Step 1 of payment. Creates order and returns orderId for frontend modal.")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _paymentService.CreateRazorpayOrderAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("verify")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(Summary = "Verify Razorpay payment", Description = "Step 2 of payment. Verifies signature and saves payment.")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
    {
        var result = await _paymentService.VerifyAndSavePaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(Summary = "Process cash payment")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var result = await _paymentService.ProcessPaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("booking/{bookingId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payment by booking")]
    public async Task<IActionResult> GetByBookingId(int bookingId)
    {
        var result = await _paymentService.GetByBookingIdAsync(bookingId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payment by ID")]
    public async Task<IActionResult> GetByPaymentId(int id)
    {
        var result = await _paymentService.GetByPaymentIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("user/{userId}")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payments by user")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _paymentService.GetByUserIdAsync(userId);
        return Ok(result);
    }

    [HttpGet("user/{userId}/history")]
    [Authorize]
    [SwaggerOperation(Summary = "Get transaction history")]
    public async Task<IActionResult> GetTransactionHistory(int userId)
    {
        var result = await _paymentService.GetTransactionHistoryAsync(userId);
        return Ok(result);
    }

    [HttpGet("{id}/status")]
    [Authorize]
    [SwaggerOperation(Summary = "Get payment status")]
    public async Task<IActionResult> GetPaymentStatus(int id)
    {
        var result = await _paymentService.GetPaymentStatusAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("refund")]
    [Authorize(Roles = "DRIVER,ADMIN")]
    [SwaggerOperation(Summary = "Process refund")]
    public async Task<IActionResult> RefundPayment([FromBody] RefundRequest request)
    {
        var result = await _paymentService.RefundPaymentAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}