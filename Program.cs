using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using MontanaMesh.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// The framework's own forwarded-headers switch would run before the trusted-proxy check below.
if (builder.Configuration.GetValue<bool>("FORWARDEDHEADERS_ENABLED") ||
    builder.Configuration.GetValue<bool>("ForwardedHeaders_Enabled"))
{
    throw new InvalidOperationException(
        "Unset ASPNETCORE_FORWARDEDHEADERS_ENABLED; this site applies forwarded headers only from its trusted proxy.");
}

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Untrusted senders have these headers stripped before this runs; Caddy overwrites them.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Persist form-protection keys in the mounted data folder so open forms survive redeploys.
var keysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data", "dataprotection-keys");
builder.Services.AddDataProtection()
    .SetApplicationName("MontanaMesh.Web")
    .PersistKeysToFileSystem(Directory.CreateDirectory(keysPath));
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
builder.Services.AddSingleton<ContactSubmissionLimiter>();

var app = builder.Build();

// Trust X-Forwarded-* only from the published-port forwarder. Rootless Podman delivers
// Caddy's connections to 127.0.0.1:8082 from the container's own address, which other
// containers on the shared network cannot use; their connections arrive from their own IPs.
app.Use((context, next) =>
{
    if (!TrustedProxy.IsForwarder(context.Connection))
    {
        context.Request.Headers.Remove("X-Forwarded-For");
        context.Request.Headers.Remove("X-Forwarded-Proto");
        context.Request.Headers.Remove("X-Forwarded-Host");
    }
    return next();
});

// Must run first so HSTS, HTTPS redirection, and cookies see Caddy's original HTTPS scheme.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers.ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; style-src-attr 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self'; connect-src 'self' https://api.github.com; " +
        "object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    await next();
});

app.UseHttpsRedirection();
// Contact POST renders its own errors, so keep status pages from replacing that response.
app.UseWhen(context => !HttpMethods.IsPost(context.Request.Method) || context.Request.Path != "/contact",
    branch => branch.UseStatusCodePagesWithReExecute("/error/{0}"));
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
