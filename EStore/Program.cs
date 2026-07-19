using Stripe;
using EStore.Components;
using EStore.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using EStore.Services.Embedding;

var builder = WebApplication.CreateBuilder(args);

/////// ENV ///////

if (builder.Environment.IsDevelopment())
{    
    DotNetEnv.Env.Load("../.env");
    builder.Configuration.AddEnvironmentVariables();
}

/////// SERVICES ///////

// Add services to the container.
builder.Services.AddRazorComponents();

// Add HttpClient for backend tasks (if any) or endpoints
builder.Services.AddHttpClient();

//// Payment Services
// Add the stripe client as a singleton service
builder.Services.AddSingleton<StripeClient>(s =>
{
    string? stripeAPIKey = builder.Configuration["Stripe:Key"]
        ?? "sk_test_placeholder_key_for_migrations";
    return new StripeClient(stripeAPIKey);
});

// Add the embedding generator as a service
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider => 
    new OllamaEmbeddingGenerator(new Uri(builder.Configuration["OLLAMA_URL"] ?? "http://localhost:11434"), "nomic-embed-text:v1.5"));

// add the embedding service
builder.Services.AddScoped<EStore.Services.Embedding.IEmbeddingService, EStore.Services.Embedding.EmbeddingService>();

// Add the product service as a scoped service - this depends on the stripe client service and database
builder.Services.AddScoped<EStore.Services.Products.IProductService, EStore.Services.Products.ProductService>();

// Add the ecommerce service as a scoped service - this depends on the stripe client service, dbcontext, and product service 
builder.Services.AddScoped<EStore.Services.Ecommerce.IEcommerceService, EStore.Services.Ecommerce.EcommerceService>();


//// Add database
builder.Services.AddDbContext<EStore.Services.Database.Context>(options =>
    options
        .UseSqlite(builder.Configuration.GetConnectionString("SQL") ?? "Data Source=:memory:")
        .AddInterceptors(new EStore.Services.Database.ConnectionIntercepter())
);

//// Add vector enabled database
builder.Services.AddSqliteVectorStore(_ => builder.Configuration.GetConnectionString("SQL") ?? "Data Source=:memory:");


/////// APP CONFIG ///////

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>();

// Map Product REST Endpoints
app.MapProductEndpoints();

app.Run();
