using AuraRental.Service.DTOs.Quote;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;

namespace AuraRental.Service.UseCase;

public sealed class QuoteUseCase(
    IQuoteRepository quoteRepository,
    IAdminRepository adminRepository,
    IAvailabilityRepository availabilityRepository,
    IRequestContext requestContext) : IQuoteUseCase
{
    public async Task<QuoteDto> Create(CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var itemIds = request.Items.Select(item => item.InventoryItemId).Distinct().ToArray();
        var available = await availabilityRepository.AreAvailable(
            requestContext.BranchId,
            itemIds,
            request.RentalStartAt,
            request.RentalEndAt,
            cancellationToken);
        if (!available)
        {
            throw new ConflictException("INVENTORY_NOT_AVAILABLE", "Có mã đồ không còn trống trong khoảng thời gian đã chọn.");
        }

        var inventory = await quoteRepository.GetInventoryItems(
            requestContext.BranchId,
            itemIds,
            cancellationToken);
        if (inventory.Count != itemIds.Length)
        {
            throw new NotFoundException("INVENTORY_NOT_FOUND", "Có mã đồ không thuộc chi nhánh đang chọn.");
        }

        var requestByItem = request.Items.ToDictionary(item => item.InventoryItemId);
        var settings = await adminRepository.GetSettings(false, cancellationToken) ?? new AuraRental.Domain.Entities.Setting();
        var quoteItems = inventory.Select(item =>
        {
            var selected = requestByItem[item.Id];
            var price = item.Variant.RentalPrices.FirstOrDefault(candidate =>
                candidate.PackageCode.Equals(selected.PackageCode, StringComparison.OrdinalIgnoreCase));
            if (price is null)
            {
                throw new ValidationException(
                    "PACKAGE_NOT_OFFERED_AT_BRANCH",
                    $"Gói {selected.PackageCode} không được bán cho mã {item.AssetCode} tại chi nhánh này.");
            }

            var rentalPrice = price.Price + CalculateExtraDayFee(item.Variant.RentalPrices, request, settings.ExtraDayRate);
            return new QuoteItemDto(
                item.Id,
                item.AssetCode,
                item.Variant.Product.Name,
                item.Variant.Size,
                price.PackageCode,
                rentalPrice,
                item.Variant.ReplacementValue);
        }).ToList();

        var depositRatio = request.DepositPlan.ToUpperInvariant() switch
        {
            "FIFTY_WITH_ID" => 0.5m,
            "FULL" => 1m,
            _ => throw new ValidationException("INVALID_DEPOSIT_PLAN", "Gói cọc phải là FIFTY_WITH_ID hoặc FULL.")
        };

        return new QuoteDto(
            requestContext.BranchId,
            "VND",
            quoteItems,
            quoteItems.Sum(item => item.RentalPrice),
            quoteItems.Sum(item => item.ReplacementValue) * depositRatio,
            await quoteRepository.GetSlotDepositAmount(cancellationToken),
            DateTimeOffset.UtcNow.AddMinutes(15));
    }

    private static decimal CalculateExtraDayFee(
        IEnumerable<AuraRental.Domain.Entities.BranchRentalPrice> prices,
        CreateQuoteRequest request,
        decimal extraDayRate)
    {
        var extraDays = Math.Max(0, (int)Math.Ceiling((request.RentalEndAt - request.RentalStartAt).TotalDays - 3));
        if (extraDays == 0)
        {
            return 0;
        }

        var oneDayPrice = prices.FirstOrDefault(price => price.PackageCode == "1D")
            ?? throw new ValidationException("ONE_DAY_PRICE_REQUIRED", "Thiếu giá gói 1D để tính ngày thuê thêm.");
        return Math.Ceiling(extraDays * oneDayPrice.Price * extraDayRate);
    }

    private static void Validate(CreateQuoteRequest request)
    {
        if (request.RentalEndAt <= request.RentalStartAt)
        {
            throw new ValidationException("INVALID_TIME_RANGE", "Thời gian trả phải sau thời gian nhận.");
        }

        if (request.Items.Count == 0)
        {
            throw new ValidationException("QUOTE_ITEMS_REQUIRED", "Báo giá cần ít nhất một mã đồ.");
        }

        if (request.Items.Select(item => item.InventoryItemId).Distinct().Count() != request.Items.Count)
        {
            throw new ValidationException("DUPLICATE_INVENTORY_ITEM", "Một mã đồ không thể xuất hiện hai lần trong báo giá.");
        }
    }
}
