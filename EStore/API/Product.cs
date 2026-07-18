using EStore.Services.Database;
using EStore.Services.Ecommerce;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace EStore.API;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products");

        group.MapPost("/details", async (List<string> productIDs, Context dbContext) =>
        {
            if (productIDs == null || productIDs.Count == 0)
            {
                return Results.Ok(new List<Product>());
            }

            var products = await dbContext.Products
                .Where(p => productIDs.Contains(p.StripeProductID))
                .ToListAsync();

            return Results.Ok(products);
        });

        group.MapPost("/checkout", async (Dictionary<string, int> cartQuantities, IEcommerceService ecommerceService) =>
        {
            if (cartQuantities == null || cartQuantities.Count == 0)
            {
                return Results.BadRequest("Cart is empty");
            }

            var session = await ecommerceService.CreateCheckoutSession(cartQuantities);
            if (session == null)
            {
                return Results.BadRequest("Failed to create checkout session");
            }

            return Results.Ok(new { url = session.Url });
        });
    }
}
