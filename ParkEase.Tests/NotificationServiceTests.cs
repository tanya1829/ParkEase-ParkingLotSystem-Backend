using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using ParkEase.NotificationService.DTOs;
using ParkEase.NotificationService.Entities;
using ParkEase.NotificationService.Interfaces;
using ParkEase.NotificationService.Services;

namespace ParkEase.Tests.NotificationServiceTests;

[TestFixture]
public class NotificationServiceTests
{
    private Mock<INotificationRepository> _repoMock = null!;
    private Mock<ILogger<ParkEase.NotificationService.Services.NotificationService>> _loggerMock = null!;
    private ParkEase.NotificationService.Services.NotificationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock   = new Mock<INotificationRepository>();
        _loggerMock = new Mock<ILogger<ParkEase.NotificationService.Services.NotificationService>>();
        _service    = new ParkEase.NotificationService.Services.NotificationService(_repoMock.Object, _loggerMock.Object);
    }

    // ════════════════════════════════════════════════
    // SEND NOTIFICATION
    // ════════════════════════════════════════════════

    [Test]
    public async Task Send_ValidAppNotification_ReturnsSuccess()
    {
        var request = ValidSendRequest();
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => { n.NotificationId = 1; return n; });

        var result = await _service.SendAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Type.Should().Be("BOOKING");
        result.Data.Channel.Should().Be("APP");
        result.Data.IsRead.Should().BeFalse();
    }

    [Test]
    public async Task Send_InvalidType_ReturnsFail()
    {
        var request = ValidSendRequest();
        request.Type = "ALERT"; // invalid

        var result = await _service.SendAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid notification type");
    }

    [Test]
    public async Task Send_InvalidChannel_ReturnsFail()
    {
        var request = ValidSendRequest();
        request.Channel = "WHATSAPP"; // invalid

        var result = await _service.SendAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid channel");
    }

    [Test]
    [TestCase("BOOKING")]
    [TestCase("CHECKIN")]
    [TestCase("CHECKOUT")]
    [TestCase("PAYMENT")]
    [TestCase("EXPIRY")]
    [TestCase("PROMO")]
    public async Task Send_AllValidTypes_ReturnsSuccess(string type)
    {
        var request = ValidSendRequest();
        request.Type = type;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => n);

        var result = await _service.SendAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Type.Should().Be(type);
    }

    [Test]
    [TestCase("APP")]
    [TestCase("EMAIL")]
    [TestCase("SMS")]
    public async Task Send_AllValidChannels_ReturnsSuccess(string channel)
    {
        var request = ValidSendRequest();
        request.Channel = channel;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => n);

        var result = await _service.SendAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Channel.Should().Be(channel);
    }

    [Test]
    public async Task Send_TypeIsCaseInsensitive()
    {
        var request = ValidSendRequest();
        request.Type = "booking"; // lowercase
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => n);

        var result = await _service.SendAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Type.Should().Be("BOOKING");
    }

    // ════════════════════════════════════════════════
    // BULK SEND
    // ════════════════════════════════════════════════

    [Test]
    public async Task SendBulk_MultipleRecipients_ReturnsAll()
    {
        var request = new SendBulkNotificationRequest
        {
            RecipientIds = new List<int> { 1, 2, 3 },
            Type         = "PROMO",
            Title        = "Special Offer",
            Message      = "50% off today!",
            Channel      = "APP"
        };
        _repoMock.Setup(r => r.CreateBulkAsync(It.IsAny<List<Notification>>()))
                 .ReturnsAsync((List<Notification> n) => n);

        var result = await _service.SendBulkAsync(request);

        result.Success.Should().BeTrue();
        result.Data!.Count.Should().Be(3);
    }

    [Test]
    public async Task SendBulk_EmptyRecipientList_ReturnsFail()
    {
        var request = new SendBulkNotificationRequest
        {
            RecipientIds = new List<int>(),
            Type = "PROMO", Title = "Test", Message = "Test", Channel = "APP"
        };

        var result = await _service.SendBulkAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("RecipientIds list cannot be empty");
    }

    // ════════════════════════════════════════════════
    // MARK AS READ
    // ════════════════════════════════════════════════

    [Test]
    public async Task MarkAsRead_ExistingNotification_SetsIsRead()
    {
        var notification = UnreadNotification();
        _repoMock.Setup(r => r.FindByNotificationIdAsync(1)).ReturnsAsync(notification);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>())).ReturnsAsync((Notification n) => n);

        var result = await _service.MarkAsReadAsync(1);

        result.Success.Should().BeTrue();
        result.Data!.IsRead.Should().BeTrue();
    }

    [Test]
    public async Task MarkAsRead_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByNotificationIdAsync(99)).ReturnsAsync((Notification?)null);

        var result = await _service.MarkAsReadAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Notification not found");
    }

    [Test]
    public async Task MarkAllRead_CallsRepository()
    {
        _repoMock.Setup(r => r.MarkAllReadByRecipientIdAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.MarkAllReadAsync(1);

        result.Success.Should().BeTrue();
        _repoMock.Verify(r => r.MarkAllReadByRecipientIdAsync(1), Times.Once);
    }

    // ════════════════════════════════════════════════
    // UNREAD COUNT
    // ════════════════════════════════════════════════

    [Test]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        _repoMock.Setup(r => r.CountByRecipientIdAndIsReadAsync(1, false)).ReturnsAsync(5);

        var result = await _service.GetUnreadCountAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(5);
    }

    [Test]
    public async Task GetUnreadCount_NoUnread_ReturnsZero()
    {
        _repoMock.Setup(r => r.CountByRecipientIdAndIsReadAsync(1, false)).ReturnsAsync(0);

        var result = await _service.GetUnreadCountAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(0);
    }

    // ════════════════════════════════════════════════
    // DELETE
    // ════════════════════════════════════════════════

    [Test]
    public async Task Delete_ExistingNotification_ReturnsSuccess()
    {
        _repoMock.Setup(r => r.FindByNotificationIdAsync(1)).ReturnsAsync(UnreadNotification());
        _repoMock.Setup(r => r.DeleteByNotificationIdAsync(1)).Returns(Task.CompletedTask);

        var result = await _service.DeleteNotificationAsync(1);

        result.Success.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteByNotificationIdAsync(1), Times.Once);
    }

    [Test]
    public async Task Delete_NotFound_ReturnsFail()
    {
        _repoMock.Setup(r => r.FindByNotificationIdAsync(99)).ReturnsAsync((Notification?)null);

        var result = await _service.DeleteNotificationAsync(99);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Notification not found");
    }

    // ════════════════════════════════════════════════
    // PREDEFINED TRIGGERS
    // ════════════════════════════════════════════════

    [Test]
    public async Task SendBookingConfirmation_CallsCreateWithCorrectType()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => n);

        await _service.SendBookingConfirmationAsync(1, 1, "A1");

        _repoMock.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
            n.Type == "BOOKING" &&
            n.RecipientId == 1 &&
            n.Channel == "APP")), Times.Once);
    }

    [Test]
    public async Task SendCheckInAlert_CallsCreateWithCorrectType()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .ReturnsAsync((Notification n) => n);

        await _service.SendCheckInAlertAsync(1, 1, "A1");

        _repoMock.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
            n.Type == "CHECKIN" && n.RecipientId == 1)), Times.Once);
    }

    [Test]
    public async Task SendCheckOutConfirmation_MessageContainsTotalAmount()
    {
        Notification? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .Callback<Notification>(n => captured = n)
                 .ReturnsAsync((Notification n) => n);

        await _service.SendCheckOutConfirmationAsync(1, 1, 150.50m);

        captured.Should().NotBeNull();
        captured!.Type.Should().Be("CHECKOUT");
        captured.Message.Should().Contain("150.50");
    }

    [Test]
    public async Task SendPaymentReceipt_MessageContainsAmountAndMode()
    {
        Notification? captured = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                 .Callback<Notification>(n => captured = n)
                 .ReturnsAsync((Notification n) => n);

        await _service.SendPaymentReceiptAsync(1, 1, 100m, "UPI");

        captured.Should().NotBeNull();
        captured!.Type.Should().Be("PAYMENT");
        captured.Message.Should().Contain("100.00");
        captured.Message.Should().Contain("UPI");
    }

    // ════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════

    private static SendNotificationRequest ValidSendRequest() => new()
    {
        RecipientId  = 1,
        Type         = "BOOKING",
        Title        = "Booking Confirmed",
        Message      = "Your spot A1 is reserved.",
        Channel      = "APP",
        RelatedId    = 1,
        RelatedType  = "BOOKING"
    };

    private static Notification UnreadNotification() => new()
    {
        NotificationId = 1,
        RecipientId    = 1,
        Type           = "BOOKING",
        Title          = "Booking Confirmed",
        Message        = "Your spot A1 is reserved.",
        Channel        = "APP",
        IsRead         = false,
        SentAt         = DateTime.UtcNow
    };
}
