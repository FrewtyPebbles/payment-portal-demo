using EStore.Services.Embedding;
using EStore.Services;
using Microsoft.EntityFrameworkCore;

namespace EStore.Services.Ecommerce;

public interface IEcommerceService
{
    // Products
    Task<Database.Product> CreateProductAsync(string productName, float productPrice, string productDescription);

    Task<Database.Product?> GetProductAsync(string stripeProductID);

    Task<Stripe.Checkout.Session?> CreateCheckoutSession(Dictionary<string, int> cartQuantities);
}

public class EcommerceService(Stripe.StripeClient stripeClient, Database.Context dbContext, Products.ProductService productService) : IEcommerceService
{
    private readonly Stripe.StripeClient _stripeClient = stripeClient;

    private readonly Database.Context _dbContext = dbContext;

    private readonly Products.ProductService _productService = productService;


    public async Task<Database.Product> CreateProductAsync(string productName, float productPrice, string productDescription)
    {
        return await _productService.AddProductAsync(productName, productPrice, productDescription);
    }

    public async Task<Database.Product?> GetProductAsync(string stripeProductID)
    {
        return await _productService.GetProductAsync(stripeProductID);
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
            BillingAddressCollection = "required",
            
            ShippingAddressCollection = new Stripe.Checkout.SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = ["US"],
                },
            LineItems = stripeLineItemOptionsList,
            Mode = "payment",
            SuccessUrl = Environment.GetEnvironmentVariable("DOMAIN") + "/purchase_success"
        };

        return _stripeClient.V1.Checkout.Sessions.Create(stripeCheckoutSessionCreateOptions);
    }
}
