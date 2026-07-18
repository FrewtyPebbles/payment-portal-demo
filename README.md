# LogiSale Store — Stripe Payment Portal Demo

LogiSale Store is a modern, responsive digital storefront demo designed to demonstrate a clean, secure integration with **Stripe Checkout**. Built using **ASP.NET Core 10** and **Blazor Static Server-Side Rendering (SSR)**, this storefront demo bridges server-side catalog management with the Stripe checkout and product API using a lightweight, scalable approach.

---

## 🚀 Key Features
- **Blazor Frontend:** Dynamic catalog browsing and individual product views using Blazor Static SSR.
- **Client-Side Cart Management:** Browser-native shopping cart implemented with JavaScript (`localStorage`) to maintain state during navigation.
- **Synchronized Catalog (Dual-Write):** Publishing a new product in the store automatically registers the product and price on Stripe in real-time, matching local database records with Stripe identifiers.
- **Secure Hosted Checkout:** Generates pre-configured Stripe Checkout sessions with customer billing and shipping address validation, redirecting seamlessly back upon payment confirmation.

---

## 🛠️ Technology Stack
- **Framework:** ASP.NET Core 10.0 (using Blazor Static SSR)
- **Programming Language:** C# & JavaScript
- **Database:** SQLite
- **Database Access:** Entity Framework Core (EF Core 10.0)
- **Payment Processor:** Stripe API via the `Stripe.net` SDK (v52.1)
- **Environment Management:** `DotNetEnv` for loading variables locally

---

## 💾 Database Schema (SQLite)

The application uses an **SQLite** database (`localapp.db` under the `EStore/` folder) for catalog data persistence. It is managed via Entity Framework Core.

### Entity Model: `Product`
| Property | Data Type | Database Constraints | Description |
| :--- | :--- | :--- | :--- |
| `ID` | `int` | Primary Key, Autoincrement | Internal primary key for local tracking. |
| `Name` | `string` | Required, Max Length: 50 | Product name displayed to customers. |
| `Description` | `string` | Required, Max Length: 1500 | Detailed product information. |
| `Price` | `float` | Required, Range: $> 0.00$ | Product unit price rate (USD). |
| `StripeProductID` | `string` | Required, Unique Index | Unique reference ID mapped to the Stripe Product. |
| `StripePriceID` | `string` | Required, Unique Index | Unique reference ID mapped to the Stripe Price. |

---

## ⚡ Worth Mentioning Implementation Details

### 1. Dual-Write via Model Construction
A notable architectural choice is how Stripe Products are created. When a new store item is published via `/add_product`, the `Product` entity's constructor receives a `StripeClient` instance alongside the product's details. 

```csharp
public Product(Stripe.StripeClient stripeClient, string productName, float productPrice, string productDescription)
{
    _stripeClient = stripeClient;
    Name = productName;
    Description = productDescription;
    Price = productPrice;

    // Register on Stripe
    Stripe.ProductCreateOptions stripeProductOptions = new() { Name = Name, Description = Description };
    Stripe.Product stripeProduct = _stripeClient.V1.Products.Create(stripeProductOptions);

    // Create the pricing model on Stripe
    Stripe.PriceCreateOptions stripePriceOptions = new()
    {
        UnitAmount = (int)(productPrice * 100), // Converted to cents
        Currency = "usd",
        Product = stripeProduct.Id
    };
    Stripe.Price stripePrice = _stripeClient.V1.Prices.Create(stripePriceOptions);

    StripeProductID = stripeProduct.Id;
    StripePriceID = stripePrice.Id;
}
```
This guarantees that **no product can exist in the SQLite database without a matching product & price profile ready on Stripe**.

### 2. ASP.NET Configuration Mapping (`Stripe__Key`)
The application uses environment variables in development loaded via `.env` file. In ASP.NET Core, double underscores `__` in environment variables act as separators for hierarchical configuration keys.
- `.env` variable `Stripe__Key` automatically maps to configuration section `Stripe:Key` queried in `Program.cs`:
  ```csharp
  string stripeAPIKey = builder.Configuration["Stripe:Key"];
  ```

### 3. Static SSR & Hybrid Cart Communication
Blazor Static SSR minimizes client-side overhead by serving static HTML. To support an interactive checkout flow without a heavy Blazor Server/WASM stateful connection:
- Cart operations (adding, removing, calculating totals, and clearing) are handled entirely via browser-native JavaScript (`cart.js`) utilizing `localStorage`.
- To check out, `cart.js` sends a HTTP POST request containing product IDs and quantities to a C# Minimal API endpoint: `/api/products/checkout`.
- The endpoint queries the local SQLite database to resolve the product price IDs and builds the secure payment redirection link using the Stripe SDK.

### 4. Post-Purchase Fulfillment Flow
Upon a successful payment, Stripe redirects the customer to `/purchase_success`. This page triggers an inline script to empty the shopping cart from the client's `localStorage`:
```javascript
if (window.cart && typeof window.cart.clearItems === 'function') {
    window.cart.clearItems();
} else {
    localStorage.removeItem('cart');
}
```

---

## ⚙️ Configuration & Setup

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A [Stripe Developer Account](https://stripe.com/) (to obtain test keys)

### Step 1: Environment Variables
Create a file named `.env` in the root folder of the repository:
```env
Stripe__Key=sk_test_your_secret_stripe_api_key_here
DOMAIN=http://localhost:5045
```

### Step 2: Running Database Migrations
If the database has not been initialized yet, run Entity Framework migrations to generate `localapp.db`:
```bash
dotnet ef database update --project EStore
```

### Step 3: Run the Application
Execute the following command from the root directory:
```bash
dotnet run --project EStore
```
Open your browser and navigate to the local hosting port (typically `http://localhost:5045`).

---

## 🖼️ User Experience Walkthrough
1. **Catalog Exploration:** View all available products in the store. Clicking on an item takes you to its specialized specs page.
2. **Catalog Creation:** Add new products via `/add_product`. It automatically syncs them to your Stripe Dashboard using the test credentials.
3. **Cart & Summary:** Manage items in your shopping bag. Items persist across tab or browser reloads.
4. **Stripe Checkout Portal:** Initiate a purchase. Fill in secure sandbox credentials to finalize payment and return back to LogiSale with a success state!
