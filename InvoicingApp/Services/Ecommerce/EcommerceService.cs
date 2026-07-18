using InvoicingApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApp.Services.Ecommerce;

public interface IEcommerceService
{
    // Products
    Task<Product> CreateProductAsync(string productName, float productPrice, string productDescription);

    Task<Product?> GetProductAsync(string stripeProductID);

    Task<Stripe.Checkout.Session?> CreateCheckoutSession(Dictionary<string, int> cartQuantities);
}

public class EcommerceService(Stripe.StripeClient stripeClient, Database.Context dbContext) : IEcommerceService
{
    private readonly Stripe.StripeClient _stripeClient = stripeClient;
    private readonly Database.Context _dbContext = dbContext;

    public async Task<Product> CreateProductAsync(string productName, float productPrice, string productDescription)
    {
        Product product = new(_stripeClient, productName, productPrice, productDescription);

        _dbContext.Products.Add(product);

        await _dbContext.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> GetProductAsync(string stripeProductID)
    {
        Product? product = await _dbContext.Products.FirstOrDefaultAsync(product => product.StripeProductID == stripeProductID);

        return product;
    }

    public async Task<Stripe.Checkout.Session?> CreateCheckoutSession(Dictionary<string, int> cartQuantities)
    {
        if (cartQuantities == null || cartQuantities.Count == 0)
            return null;

        List<Stripe.Checkout.SessionLineItemOptions> stripeLineItemOptionsList = [];

        List<string> cartStripeProductIDs = [.. cartQuantities.Keys];

        var productQueryEnumerable = _dbContext.Products.Select(product => new
        {
            StripeProductID = product.StripeProductID,
            StripePriceID = product.StripePriceID,
        }).Where(product => cartStripeProductIDs.Contains(product.StripeProductID)).ToAsyncEnumerable();

        await foreach (var row in productQueryEnumerable)
        {
            stripeLineItemOptionsList.Add(new()
            {
                Price = row.StripePriceID,
                Quantity = cartQuantities[row.StripeProductID]
            });
        }

        Stripe.Checkout.SessionCreateOptions stripeCheckoutSessionCreateOptions = new()
        {
            LineItems = stripeLineItemOptionsList,
            Mode = "payment",
            SuccessUrl = Environment.GetEnvironmentVariable("DOMAIN") + "/purchase_success"
        };

        return _stripeClient.V1.Checkout.Sessions.Create(stripeCheckoutSessionCreateOptions);
    }
}
