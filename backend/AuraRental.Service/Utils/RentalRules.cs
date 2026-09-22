using System.Security.Cryptography;
using AuraRental.Domain.Entities;
using AuraRental.Domain.Enums;
using AuraRental.Service.Exceptions;

namespace AuraRental.Service.Utils;

public static class RentalRules
{
    public const decimal DefaultExtraDayRate = 0.10m;

    public static decimal GetDepositRatio(string depositPlan) => depositPlan.ToUpperInvariant() switch
    {
        "FIFTY_WITH_ID" => 0.5m,
        "FULL" => 1m,
        _ => throw new ValidationException("INVALID_DEPOSIT_PLAN", "Gói cọc phải là FIFTY_WITH_ID hoặc FULL.")
    };

    public static decimal ConfirmedDeposit(IEnumerable<Payment> payments) =>
        payments
            .Where(payment =>
                payment.Status == PaymentStatus.Confirmed &&
                payment.Type is PaymentType.SlotDeposit or PaymentType.TargetDeposit)
            .Sum(payment => payment.Amount);

    public static decimal DepositRemaining(Reservation reservation) =>
        Math.Max(0, reservation.DepositRequired - ConfirmedDeposit(reservation.Payments));

    public static decimal RentalFee(ReservationItem item, DateTimeOffset startAt, DateTimeOffset endAt)
    {
        var extraDays = Math.Max(0, (int)Math.Ceiling((endAt - startAt).TotalDays - 3));
        return item.RentalPrice + Math.Ceiling(extraDays * item.OneDayPrice * item.ExtraDayRate);
    }

    public static TEnum ParseEnum<TEnum>(string value, string code, string message)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value.Replace("_", string.Empty, StringComparison.Ordinal), true, out var result))
        {
            return result;
        }

        throw new ValidationException(code, message);
    }

    public static IReadOnlyList<TEnum> ParseEnumList<TEnum>(string? value, string code, string message)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => ParseEnum<TEnum>(item, code, message))
            .Distinct()
            .ToList();
    }

    public static string MaskPhone(string phone)
    {
        if (phone.Length < 7)
        {
            return phone;
        }

        return $"{phone[..4]} *** {phone[^3..]}";
    }

    public static string CreateNumber(string branchCode, string? marker = null)
    {
        var suffix = $"{DateTimeOffset.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(0, 100_000):D5}";
        return string.IsNullOrWhiteSpace(marker)
            ? $"{branchCode}-{suffix}"
            : $"{branchCode}-{marker}-{suffix}";
    }
}
