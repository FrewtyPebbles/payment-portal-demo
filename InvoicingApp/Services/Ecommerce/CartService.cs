using InvoicingApp.Services.Database;
using InvoicingApp.Services.LocalStorage;

namespace InvoicingApp.Services.Ecommerce;

public interface ICartService
{
    Task AddItem(string stripeProductID);

    Task RemoveItem(string stripeProductID);
    
    IAsyncEnumerable<(Product, int)> GetProducts();

    Task<Dictionary<string, int>?> GetCartQuantities();
}

public class CartService(LocalStorageService localStorageService, Database.Context dbContext) : ICartService
{

    private readonly LocalStorageService _localStorageService = localStorageService;
    private readonly Database.Context _dbContext = dbContext;

    async public Task<Dictionary<string, int>?> GetCartQuantities()
    {
        return await _localStorageService.GetItemAsync<Dictionary<string, int>>("cart");
    }
    
    async public Task AddItem(string stripeProductID)
    {
        Dictionary<string, int>? cart = await _localStorageService.GetItemAsync<Dictionary<string, int>>("cart");

        if (cart != null)
        {
            cart[stripeProductID] = cart.GetValueOrDefault(stripeProductID, 0) + 1;
            await _localStorageService.SetItemAsync("cart", cart);
        }
        else
            await _localStorageService.SetItemAsync("cart", new Dictionary<string, int>{[stripeProductID] = 1});
    }

    async public Task RemoveItem(string stripeProductID)
    {
        Dictionary<string, int>? cart = await _localStorageService.GetItemAsync<Dictionary<string, int>>("cart");

        if (cart != null)
        {
            if (cart.TryGetValue(stripeProductID, out int value))
            {
                if (value > 1)
                    await _localStorageService.SetItemAsync(stripeProductID, value - 1);
                else
                    await _localStorageService.RemoveItemAsync(stripeProductID);
            }
        }
    }
    
    async public IAsyncEnumerable<(Product, int)> GetProducts()
    {
        Dictionary<string, int>? cart = await _localStorageService.GetItemAsync<Dictionary<string, int>>("cart");
        if (cart != null)
        {
            IAsyncEnumerable<Product> cartProductsEnumerable = _dbContext.Products.Where(product => cart.ContainsKey(product.StripeProductID)).ToAsyncEnumerable();

            await foreach (Product product in cartProductsEnumerable)
            {
                yield return (product, cart[product.StripeProductID]);
            }
        }
    }
}