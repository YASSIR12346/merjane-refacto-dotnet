using Merjane.Domain.Entities;

namespace Merjane.Application.Orders;

internal static class OrderProcessingValidator
{
    public static string? Validate(ICollection<Product> products)
    {
        foreach (Product product in products)
        {
            if (product.Type is not ("NORMAL" or "SEASONAL" or "EXPIRABLE"))
                return $"Unsupported product type for product {product.Id}.";

            if (product.Name is null)
                return $"Product {product.Id} has no name.";

            if (product.Type == "EXPIRABLE" && product.ExpiryDate is null)
                return $"Product {product.Id} has no expiration date.";

            if (product.Type == "SEASONAL" &&
                (product.SeasonStartDate is null || product.SeasonEndDate is null))
                return $"Product {product.Id} has incomplete season dates.";
        }

        return null;
    }
}
