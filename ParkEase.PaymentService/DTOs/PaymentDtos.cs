namespace ParkEase.PaymentService.DTOs;

// ---------- Request DTOs ----------

/// <summary>Request to process a payment for a booking</summary>
public class ProcessPaymentRequest
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = "CASH";   // CARD | UPI | WALLET | CASH
    public string? Description { get; set; }
}

/// <summary>Request to create a Razorpay order</summary>
public class CreateOrderRequest
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

/// <summary>Request to verify Razorpay payment signature</summary>
public class VerifyPaymentRequest
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = "CARD";
    public string RazorpayOrderId { get; set; } = string.Empty;
    public string RazorpayPaymentId { get; set; } = string.Empty;
    public string RazorpaySignature { get; set; } = string.Empty;
}

/// <summary>Request to refund a payment</summary>
public class RefundRequest
{
    public int PaymentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// ---------- Response DTOs ----------

/// <summary>Payment details returned in API responses</summary>
public class PaymentDto
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string Currency { get; set; } = "INR";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}

/// <summary>Razorpay order creation response — returned to frontend to open checkout modal</summary>
public class RazorpayOrderDto
{
    public string OrderId { get; set; } = string.Empty;   // Razorpay order_id
    public string KeyId { get; set; } = string.Empty;     // Razorpay key_id (public)
    public decimal Amount { get; set; }                    // Amount in INR
    public string Currency { get; set; } = "INR";
    public string? Description { get; set; }
}

/// <summary>Revenue summary for a lot</summary>
public class RevenueDto
{
    public int LotId { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalPayments { get; set; }
}

/// <summary>Generic API response wrapper</summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}