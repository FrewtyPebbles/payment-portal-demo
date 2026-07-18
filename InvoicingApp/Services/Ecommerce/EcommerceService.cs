using InvoicingApp.Services.Database;
using Microsoft.EntityFrameworkCore;

namespace InvoicingApp.Services.Ecommerce;

public interface IEcommerceService
{
    // Products
    Task<Product> CreateProductAsync(string productName, float productPrice, string productDescription);
    Task<Product?> GetProductAsync(string stripeProductID);

    // Subscription
    // Subscription CreateSubscription(string productName, float productPrice, string productDescription);
}

public class EcommerceService(Stripe.StripeClient stripeClient) : IEcommerceService
{
    private readonly Stripe.StripeClient _stripeClient = stripeClient;
    private Database.Context _dbContext;

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

    async Task<Stripe.Checkout.Session> CreateCheckoutSession(Dictionary<string, int> productsInCart)
    {
        List<Stripe.Checkout.SessionLineItemOptions> stripeLineItemOptionsList = [];

        var productQueryEnumerable = _dbContext.Products.Select(product => new
        {
            StripeProductID = product.StripeProductID,
            StripePriceID = product.StripePriceID,
        }).Where(product => productsInCart.ContainsKey(product.StripeProductID)).ToAsyncEnumerable();

        await foreach (var row in productQueryEnumerable)
        {
            stripeLineItemOptionsList.Add(new()
            {
                Price = row.StripePriceID,
                Quantity = productsInCart[row.StripeProductID]
            });
        }

        Stripe.Checkout.SessionCreateOptions stripeCheckoutSessionCreateOptions = new()
        {
            LineItems = stripeLineItemOptionsList,
            Mode = "payment",
            SuccessUrl = Environment.GetEnvironmentVariable("DOMAIN")
        };

        return _stripeClient.V1.Checkout.Sessions.Create(stripeCheckoutSessionCreateOptions);
    }
}