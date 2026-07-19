using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.VectorData;

namespace EStore.Services.Database;

public class Product
{
    public int ID { get; private set; }

    public required string Name { get; set; }

    public required string Description { get; set; }
    
    public required float Price { get; set; }
    
    public required string StripeProductID { get; set; }
    
    public required string StripePriceID { get; set; }

    [NotMapped]
    public float[]? Embedding { get; set; }

    [NotMapped]
    public string EmbeddingText => $"Product Name: {Name}\n\nPrice: ${Price}\n\nDescription: {Description}";

    private Product()
    {
        // EF Core populates the required members using property reflection
    }

    [SetsRequiredMembers]
    public Product(Stripe.StripeClient stripeClient, string productName, float productPrice, string productDescription)
    {
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
        Stripe.Product stripeProduct = stripeClient.V1.Products.Create(stripeProductOptions);

        // Create price:
        Stripe.PriceCreateOptions stripePriceOptions = new()
        {
            UnitAmount = (int)(productPrice * 100),
            Currency = "usd",
            Product = stripeProduct.Id
        };
        Stripe.Price stripePrice = stripeClient.V1.Prices.Create(stripePriceOptions);

        StripeProductID = stripeProduct.Id;
        StripePriceID = stripePrice.Id;
    }
}

public record ProductEmbeddingRecord // Vector embedding table
{
    [VectorStoreKey]
    public int ID { get; set; }

    [VectorStoreData]
    public required string Name { get; set; }

    [VectorStoreData]
    public required string Description { get; set; }

    [VectorStoreData]
    public required float Price { get; set; }

    [VectorStoreData]

    public required string StripeProductID { get; set; }

    [VectorStoreData]
    public required string StripePriceID { get; set; }

    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineDistance)]
    public float[] Embedding { get; set; } = null!;
}