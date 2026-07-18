using InvoicingApp.Components;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{    
    DotNetEnv.Env.Load("../.env");
}
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

//// LocalStorage Service
builder.Services.AddScoped<InvoicingApp.Services.LocalStorage.LocalStorageService>();

//// Payment Services
// Add the stripe client as a singleton service
builder.Services.AddSingleton<IStripeClient>(s =>
{
    string? stripeAPIKey = builder.Configuration["Stripe:Key"];
    return new StripeClient(stripeAPIKey);
});

// Add the ecommerce service as a scoped service - this depends on the stripe client service
builder.Services.AddScoped<InvoicingApp.Services.Ecommerce.IEcommerceService, InvoicingApp.Services.Ecommerce.EcommerceService>();


//// Add database
builder.Services.AddDbContext<InvoicingApp.Services.Database.Context>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
