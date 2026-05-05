# ParkEase.Tests — NUnit Test Suite

## Setup

### 1. Create the test project in your solution

```bash
cd ParkEase-ParkingLotSystem-Backend
mkdir ParkEase.Tests
```

Copy all files from this folder into `ParkEase.Tests/`.

### 2. Add project to solution

```bash
dotnet sln add ParkEase.Tests/ParkEase.Tests.csproj
```

### 3. Restore packages

```bash
cd ParkEase.Tests
dotnet restore
```

### 4. Run all tests

```bash
dotnet test
```

### 5. Run with detailed output

```bash
dotnet test --logger "console;verbosity=detailed"
```

### 6. Run a specific test class

```bash
dotnet test --filter "FullyQualifiedName~BookingServiceTests"
dotnet test --filter "FullyQualifiedName~PaymentServiceTests"
```

---

## Test Coverage

### BookingServiceTests (17 tests)
| Test | Covers |
|---|---|
| CreateBooking_ValidRequest | Happy path booking creation |
| CreateBooking_EndTimeBeforeStartTime | Time validation |
| CreateBooking_StartTimeInPast | Past time validation |
| CreateBooking_ZeroPricePerHour | Price validation |
| CreateBooking_SpotAlreadyBooked | Spot conflict detection |
| CreateBooking_FareCalculation_MinimumOneHour | Min 1hr fare rule |
| CreateBooking_FareCalculation_TwoHours | Fare math |
| CheckIn_ReservedBooking | RESERVED → ACTIVE |
| CheckIn_NonExistentBooking | Not found handling |
| CheckIn_AlreadyActiveBooking | Status guard |
| CheckIn_CancelledBooking | Status guard |
| CheckOut_ActiveBooking | ACTIVE → COMPLETED |
| CheckOut_NotActiveBooking | Status guard |
| CheckOut_FareAppliesMinimumOneHour | Min 1hr charge on exit |
| Cancel_ReservedBooking | Cancellation + zero amount |
| Cancel_CompletedBooking | Cannot cancel completed |
| Cancel_AlreadyCancelledBooking | Idempotency guard |
| Extend_ValidNewTime | Extension happy path |
| Extend_NewTimeBeforeCurrentEndTime | Extension validation |
| Extend_CompletedBooking | Status guard |
| GetOccupancyRate_CalculatesCorrectly | 50% occupancy math |
| GetOccupancyRate_ZeroTotalSpots | Division by zero guard |

### PaymentServiceTests (17 tests)
| Test | Covers |
|---|---|
| ProcessPayment_ValidCashPayment | Cash payment, no txn ID |
| ProcessPayment_OnlineMode_GeneratesTransactionId | CARD/UPI/WALLET txn ID |
| ProcessPayment_InvalidMode | Mode validation |
| ProcessPayment_ZeroAmount | Amount validation |
| ProcessPayment_NegativeAmount | Amount validation |
| ProcessPayment_AlreadyPaid | Duplicate payment guard |
| ProcessPayment_ModeIsCaseInsensitive | Normalisation |
| GetByBookingId_ExistingPayment | Lookup happy path |
| GetByBookingId_NotFound | Not found handling |
| GetByPaymentId_NotFound | Not found handling |
| Refund_PaidPayment | PAID → REFUNDED |
| Refund_NotPaidPayment | Cannot double-refund |
| Refund_NotFound | Not found handling |
| GetPaymentStatus_ExistingPayment | Status lookup |
| GetPaymentStatus_NotFound | Not found handling |
| GetTransactionHistory_ReturnsUserPayments | History list |
| GetTransactionHistory_NoPayments | Empty list |

---

## Packages Used
- **NUnit 4.1** — test framework
- **Moq 4.20** — mocking repositories
- **FluentAssertions 6.12** — readable assertions
