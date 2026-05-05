using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ParkEase.PaymentService.DTOs;
using ParkEase.PaymentService.Entities;
using ParkEase.PaymentService.Interfaces;
using ParkEase.PaymentService.Services;

namespace ParkEase.Tests.PaymentServiceTests;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentRepository> _repoMock = null!;
    private Mock<IConfiguration>     _configMock = null!;
    private Mock<IHttpClientFactory> _httpFactoryMock = null!;
    private Mock<ILogger<ParkEase.PaymentService.Services.PaymentService>> _loggerMock = null!;
    private ParkEase.PaymentService.Services.PaymentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock        = new Mock<IPaymentRepository>();
        _configMock      = new Mock<IConfiguration>();
        _httpFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock      = new Mock<ILogger<ParkEase.PaymentService.Services.PaymentService>>();

        // Setup Razorpay config
        _configMock.Setup(c => c["Razorpay:KeyId"]).Returns("rzp_test_key");
        _configMock.Setup(c => c["Razorpay:KeySecret"]).Returns("test_secret");

        _service = new ParkEase.PaymentService.Services.PaymentService(
            _repoMock.Object,
            _configMock.Object,
            _httpFactoryMock.Object,
            _loggerMock.Object);
    }

    // ════════════════════════════════════════════════
    // PROCESS PAYMENT
    // ════════════════════════════════════════════════

    [Test]
    public async Task ProcessPayment_ValidCashPayment_ReturnsSuccess()
    {
        var request = ValidPaymentRequest("CASH");
        _repoMock.Setup(r => r.FindByBookingIdAsync(request.BookingId)).ReturnsAsync((Payment?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => { p.PaymentId = 1; return p; });

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("PAID");
        result.Data.Mode.Should().Be("CASH");
        result.Data.TransactionId.Should().BeNull(); // cash has no txn ID
    }

    [Test]
    [TestCase("CARD")]
    [TestCase("UPI")]
    [TestCase("WALLET")]
    public async Task ProcessPayment_OnlineMode_GeneratesTransactionId(string mode)
    {
        var request = ValidPaymentRequest(mode);
        _repoMock.Setup(r => r.FindByBookingIdAsync(request.BookingId)).ReturnsAsync((Payment?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.TransactionId.Should().NotBeNullOrEmpty();
        result.Data.TransactionId.Should().StartWith("TXN-");
    }

    [Test]
    public async Task ProcessPayment_InvalidMode_ReturnsFail()
    {
        var request = ValidPaymentRequest("BITCOIN");

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid payment mode");
    }

    [Test]
    public async Task ProcessPayment_ZeroAmount_ReturnsFail()
    {
        var request = ValidPaymentRequest("CASH");
        request.Amount = 0;

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Amount must be greater than 0");
    }

    [Test]
    public async Task ProcessPayment_NegativeAmount_ReturnsFail()
    {
        var request = ValidPaymentRequest("CARD");
        request.Amount = -100m;

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Amount must be greater than 0");
    }

    [Test]
    public async Task ProcessPayment_AlreadyPaid_ReturnsFail()
    {
        var request = ValidPaymentRequest("CASH");
        var existingPayment = new Payment
        {
            PaymentId = 1,
            BookingId = request.BookingId,
            Status    = "PAID",
            Amount    = request.Amount
        };
        _repoMock.Setup(r => r.FindByBookingIdAsync(request.BookingId)).ReturnsAsync(existingPayment);

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already processed");
    }

    [Test]
    [TestCase("card")]
    [TestCase("upi")]
    [TestCase("Cash")]
    [TestCase("WALLET")]
    public async Task ProcessPayment_ModeIsCaseInsensitive(string mode)
    {
        var request = ValidPaymentRequest(mode);
        _repoMock.Setup(r => r.FindByBookingIdAsync(request.BookingId)).ReturnsAsync((Payment?)null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

        var result = await _service.ProcessPaymentAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Mode.Should().Be(mode.ToUpper());
    }

    // ════════════════════════════════════════════════
    // GET PAYMENT
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetByBookingId_ExistingPayment_ReturnsPayment()
    {
        var payment = PaidPayment();
        _repoMock.Setup(r => r.FindByBookingIdAsync(1)).ReturnsAsync(payment);

        var result = await _service.GetByBookingIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.BookingId.Should().Be(1);
        result.Data.Status.Should().Be("PAID");
    }

    [Test]
    public async Task GetByBookingId_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByBookingIdAsync(99)).ReturnsAsync((Payment?)null);

        var result = await _service.GetByBookingIdAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No payment found");
    }

    [Test]
    public async Task GetByPaymentId_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByPaymentIdAsync(99)).ReturnsAsync((Payment?)null);

        var result = await _service.GetByPaymentIdAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Payment not found");
    }

    // ════════════════════════════════════════════════
    // REFUND
    // ════════════════════════════════════════════════

    [Test]
    public async Task Refund_PaidPayment_ReturnsRefunded()
    {
        var payment = PaidPayment();
        _repoMock.Setup(r => r.FindByPaymentIdAsync(1)).ReturnsAsync(payment);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Payment>())).ReturnsAsync((Payment p) => p);

        var result = await _service.RefundPaymentAsync(new RefundRequest
        {
            PaymentId = 1,
            Reason    = "Customer request"
        });

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be("REFUNDED");
        result.Data.RefundedAt.Should().NotBeNull();
    }

    [Test]
    public async Task Refund_NotPaidPayment_ReturnsFail()
    {
        var payment = PaidPayment();
        payment.Status = "REFUNDED"; // already refunded
        _repoMock.Setup(r => r.FindByPaymentIdAsync(1)).ReturnsAsync(payment);

        var result = await _service.RefundPaymentAsync(new RefundRequest { PaymentId = 1, Reason = "test" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot refund");
    }

    [Test]
    public async Task Refund_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByPaymentIdAsync(99)).ReturnsAsync((Payment?)null);

        var result = await _service.RefundPaymentAsync(new RefundRequest { PaymentId = 99, Reason = "test" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Payment not found");
    }

    // ════════════════════════════════════════════════
    // PAYMENT STATUS
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetPaymentStatus_ExistingPayment_ReturnsStatus()
    {
        _repoMock.Setup(r => r.FindByPaymentIdAsync(1)).ReturnsAsync(PaidPayment());

        var result = await _service.GetPaymentStatusAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().Be("PAID");
    }

    [Test]
    public async Task GetPaymentStatus_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByPaymentIdAsync(99)).ReturnsAsync((Payment?)null);

        var result = await _service.GetPaymentStatusAsync(99);

        result.Success.Should().BeFalse();
    }

    // ════════════════════════════════════════════════
    // TRANSACTION HISTORY
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetTransactionHistory_ReturnsUserPayments()
    {
        var payments = new List<Payment> { PaidPayment(), PaidPayment() };
        _repoMock.Setup(r => r.FindByUserIdAsync(1)).ReturnsAsync(payments);

        var result = await _service.GetTransactionHistoryAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(2);
    }

    [Test]
    public async Task GetTransactionHistory_NoPayments_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.FindByUserIdAsync(99)).ReturnsAsync(new List<Payment>());

        var result = await _service.GetTransactionHistoryAsync(99);

        result.Success.Should().BeTrue();
        result.Data!.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static ProcessPaymentRequest ValidPaymentRequest(string mode) => new()
    {
        BookingId   = 1,
        UserId      = 1,
        Amount      = 100m,
        Mode        = mode,
        Description = "Test payment"
    };

    private static Payment PaidPayment() => new()
    {
        PaymentId     = 1,
        BookingId     = 1,
        UserId        = 1,
        Amount        = 100m,
        Mode          = "CASH",
        Status        = "PAID",
        Currency      = "INR",
        TransactionId = null,
        CreatedAt     = DateTime.UtcNow.AddMinutes(-10),
        PaidAt        = DateTime.UtcNow.AddMinutes(-10)
    };
}
