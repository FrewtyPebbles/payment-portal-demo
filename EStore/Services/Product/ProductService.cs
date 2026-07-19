using Microsoft.SemanticKernel.Connectors.SqliteVec;
using Microsoft.EntityFrameworkCore;
using EStore.Services;
using Microsoft.Extensions.VectorData;

namespace EStore.Services.Products;

public interface IProductService
{
    Task<Database.Product> AddProductAsync(string productName, float productPrice, string productDescription);
    Task<Database.Product?> GetProductAsync(string stripeProductID);
    Task<List<Database.Product>> SearchProductsAsync(string searchString, int limit = 5);
}

public class ProductService(Database.Context dbContext, Embedding.IEmbeddingService embeddingService, Stripe.StripeClient stripeClient, IConfiguration configuration) : IProductService
{
    private readonly Database.Context _dbContext = dbContext;

    private readonly Embedding.IEmbeddingService _embeddingService = embeddingService;

    private readonly Stripe.StripeClient _stripeClient = stripeClient;

    private readonly SqliteCollection<int, Database.ProductEmbeddingRecord> _embeddingCollection =
        new(
            configuration.GetConnectionString("SQL") ?? "Data Source=:memory:", 
            "ProductEmbeddings"
        );

    public async Task<Database.Product> AddProductAsync(string productName, float productPrice, string productDescription)
    {
        // Add db table model
        Database.Product product = new(_stripeClient, productName, productPrice, productDescription);

        _dbContext.Products.Add(product);

        product.Embedding = await _embeddingService.GenerateEmbeddingAsync(product.EmbeddingText);

        await _dbContext.SaveChangesAsync();

        // Add vector table model
        Database.ProductEmbeddingRecord embeddingRecord = new()
        {
            ID = product.ID,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            StripePriceID = product.StripePriceID,
            StripeProductID = product.StripeProductID,
            Embedding = product.Embedding
        };

        await _embeddingCollection.EnsureCollectionExistsAsync();
        await _embeddingCollection.UpsertAsync(embeddingRecord);

        return product;
    }

    public async Task<Database.Product?> GetProductAsync(string stripeProductID)
    {
        Database.Product? product = await _dbContext.Products.FirstOrDefaultAsync(product => product.StripeProductID == stripeProductID);
        
        if (product != null)
        {
            await _embeddingCollection.EnsureCollectionExistsAsync();
            // There will only be a single record since it is a primary key
            await foreach (Database.ProductEmbeddingRecord record in _embeddingCollection.GetAsync(r => r.ID == product.ID, 1))
                product.Embedding = record.Embedding;
        }

        return product;
    }

    public async Task<List<Database.Product>> SearchProductsAsync(string searchString, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return await _dbContext.Products.Take(limit).ToListAsync();
        }

        await _embeddingCollection.EnsureCollectionExistsAsync();

        float[] searchVector = await _embeddingService.GenerateEmbeddingAsync(searchString);

        IAsyncEnumerable<VectorSearchResult<Database.ProductEmbeddingRecord>> searchResults = _embeddingCollection.SearchAsync(searchVector, limit);
        
        // Get embedding ids
        var matchedIds = new List<int>();
        
        await foreach (var result in searchResults)
        {
            matchedIds.Add(result.Record.ID);
        }

        if (matchedIds.Count == 0)
        {
            return [];
        }

        // get products
        var products = await _dbContext.Products
            .Where(p => matchedIds.Contains(p.ID))
            .ToListAsync();

        // Reorder based on similarity
        return [.. products.OrderBy(p => matchedIds.IndexOf(p.ID))];
    }
}