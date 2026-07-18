using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace InvoicingApp.Services.Database;

public class Product
{
    public int ID { get; private set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required float Price { get; set; }
    public required string StripeProductID { get; set; }
    public required string StripePriceID { get; set; }

    [NotMapped]
    private readonly Stripe.StripeClient _stripeClient;

    [SetsRequiredMembers]
    public Product(Stripe.StripeClient stripeClient, string productName, float productPrice, string productDescription)
    {
        _stripeClient = stripeClient;
        Name = productName;
        Description = productDescription;
        Price = productPrice;

        // Create stripe product and price and get product and price id
        // Create product:
        Stripe.ProductCreateOptions stripeProductOptions = new()
        {
            Name = Name,
            Description = Description
        };
        Stripe.Product stripeProduct = _stripeClient.V1.Products.Create(stripeProductOptions);

        // Create price:
        Stripe.PriceCreateOptions stripePriceOptions = new()
        {
            UnitAmount = (int)(productPrice * 100),
            Currency = "usd",
            Product = stripeProduct.Id
        };
        Stripe.Price stripePrice = _stripeClient.V1.Prices.Create(stripePriceOptions);

        StripeProductID = stripeProduct.Id;
        StripePriceID = stripePrice.Id;
    }
}