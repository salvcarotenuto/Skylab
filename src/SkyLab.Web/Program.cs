using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using SkyLab.Web.Models;
using SkyLab.Web.Data;
using Microsoft.AspNetCore.HttpOverrides;
using MySqlConnector;
using System.Threading.RateLimiting;

namespace SkyLab.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole();

        // Add services to the container.
        builder.Services.AddRazorPages()
            .AddMvcOptions(options =>
                options.ModelBinderProviders.Insert(0, new SkyLab.Web.Infrastructure.FlexibleDecimalModelBinderProvider()));
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["SkyLab:DataProtectionKeysPath"]
                ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")))
            .SetApplicationName("SkyLab");
        builder.Services.AddSingleton<SkyLab.Web.Services.ApplicationState>();
        builder.Services.AddScoped<SkyLab.Web.Services.SkyLabServicePaths>();
        builder.Services.AddScoped<SkyLabDatabase>();
        builder.Services.AddSingleton<SkyLabDatabaseOptions>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.Name = ".SkyLab.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        builder.Services.AddScoped<SkyLab.Web.Services.ApplicationAuthService>();
        builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        builder.Services.AddScoped<PurchaseInvoiceRepository>();
        builder.Services.AddScoped<SalesInvoiceRepository>();
        builder.Services.AddScoped<AccountingCauseRepository>();
        builder.Services.AddScoped<AccountingMovementRepository>();
        builder.Services.AddScoped<AccountingMovementSummaryRepository>();
        builder.Services.AddScoped<AccountingStatementRepository>();
        builder.Services.AddScoped<CashStatementRepository>();
        builder.Services.AddScoped<CustomerSupplierBalanceSummaryRepository>();
        builder.Services.AddScoped<InitialCustomerSupplierBalanceRepository>();
        builder.Services.AddScoped<LookupRepository>();
        builder.Services.AddScoped<OpeningBalanceRepository>();
        builder.Services.AddScoped<StoreMovementSummaryRepository>();
        builder.Services.AddScoped<TrialBalanceRepository>();
        builder.Services.AddScoped<SkyLab.Web.Services.SalesElectronicInvoiceService>();
        builder.Services.AddScoped<SkyLab.Web.Services.TrialBalancePdfService>();
        builder.Services.AddScoped<SkyLab.Web.Services.StoreMovementSummaryPdfService>();
        builder.Services.AddSingleton<SkyLab.Web.Services.InterventionService>();
        builder.Services.AddScoped<SkyLab.Web.Services.PlanningService>();
        builder.Services.AddScoped<SkyLab.Web.Services.CustomerService>();
        builder.Services.AddScoped<SkyLab.Web.Services.SmtpConnectionTester>();
        builder.Services.AddScoped<SkyLab.Web.Services.SupplierService>();
        builder.Services.AddScoped<SkyLab.Web.Services.WorkService>();
        builder.Services.AddScoped<SkyLab.Web.Services.UserService>();
        builder.Services.AddSingleton<SkyLab.Web.Services.MobileAuthService>();
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var italian = new CultureInfo("it-IT");
            options.DefaultRequestCulture = new RequestCulture(italian);
            options.SupportedCultures = [italian];
            options.SupportedUICultures = [italian];
        });

        var app = builder.Build();
        app.Services.GetRequiredService<SkyLabDatabaseOptions>().LoadDefaultCompanyAsync().GetAwaiter().GetResult();

        app.UseForwardedHeaders();
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRequestLocalization();

        app.UseRouting();
        app.UseRateLimiter();
        app.UseSession();
        var authenticationRequired = !app.Environment.IsDevelopment()
            || builder.Configuration.GetValue("SkyLab:Authentication:Required", false);
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var publicRequest = path.Equals("/Login", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/Error", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/mobile")
                || path.StartsWithSegments("/css") || path.StartsWithSegments("/js")
                || path.StartsWithSegments("/lib") || path.StartsWithSegments("/images")
                || path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/SkyLab.Web.styles.css", StringComparison.OrdinalIgnoreCase);
            if (!authenticationRequired || publicRequest || context.RequestServices.GetRequiredService<SkyLab.Web.Services.ApplicationAuthService>().IsLoggedIn())
            { await next(); return; }
            if (path.StartsWithSegments("/api")) { context.Response.StatusCode = 401; return; }
            context.Response.Redirect("/Login");
        });

        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.MapGet("/api/lookup_anagrafiche", async (
            string? type,
            string? q,
            int? code,
            LookupRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (code is not null)
            {
                return Results.Json(new { row = await repository.FindAnagraficaAsync(type, code.Value, cancellationToken) });
            }

            return Results.Json(new { rows = await repository.SearchAnagraficheAsync(type, q, cancellationToken) });
        });

        app.MapPost("/api/sales-invoices", async (
            SalesInvoiceSaveRequest request,
            SalesInvoiceRepository repository,
            SkyLab.Web.Services.ApplicationState applicationState,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var saved = await repository.SaveAsync(request, applicationState.Esercizio, cancellationToken);
                return Results.Ok(new { success = true, saved.Id, saved.Year, saved.Code });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { success = false, message = exception.Message });
            }
            catch (MySqlException exception)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"Registrazione della fattura non riuscita. Motivo: {exception.Message}"
                });
            }
            catch (Exception exception)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"Registrazione della fattura non riuscita. Motivo: {exception.Message}"
                });
            }
        });

        app.MapGet("/api/sales-invoices/{id:int}", async (int id, SalesInvoiceRepository repository, CancellationToken cancellationToken) =>
            await repository.FindAsync(id, cancellationToken) is { } invoice ? Results.Ok(invoice) : Results.NotFound());
        app.MapPut("/api/sales-invoices/{id:int}", async (int id, SalesInvoiceSaveRequest request, SalesInvoiceRepository repository, SkyLab.Web.Services.ApplicationState applicationState, CancellationToken cancellationToken) =>
        {
            try { await repository.UpdateAsync(id, request, applicationState.Esercizio, cancellationToken); return Results.Ok(new { success=true,id }); }
            catch(Exception exception){ return Results.BadRequest(new { success=false,message=$"Aggiornamento della fattura non riuscito. Motivo: {exception.Message}" }); }
        });
        app.MapDelete("/api/sales-invoices/{id:int}", async (int id, SalesInvoiceRepository repository, CancellationToken cancellationToken) =>
        {
            try { await repository.DeleteAsync(id,cancellationToken); return Results.Ok(new { success=true }); }
            catch(Exception exception){ return Results.BadRequest(new { success=false,message=$"Cancellazione della fattura non riuscita. Motivo: {exception.Message}" }); }
        });
        app.MapPost("/api/sales-invoices/{id:int}/electronic-prepare", async (int id, SkyLab.Web.Services.SalesElectronicInvoiceService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var prepared = await service.PrepareAsync(id, cancellationToken);
                return Results.Ok(new { success = true, prepared.FileName, prepared.AlreadyPrepared });
            }
            catch (InvalidOperationException exception) { return Results.BadRequest(new { success = false, message = exception.Message }); }
            catch (Exception) { return Results.BadRequest(new { success = false, message = "Preparazione della fattura elettronica non riuscita." }); }
        });
        app.MapPost("/api/sales-invoices/{id:int}/electronic-send", async (int id, SkyLab.Web.Services.SalesElectronicInvoiceService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var fileName = await service.SendAsync(id, cancellationToken);
                return Results.Ok(new { success = true, fileName });
            }
            catch (InvalidOperationException exception) { return Results.BadRequest(new { success = false, message = exception.Message }); }
            catch (Exception) { return Results.BadRequest(new { success = false, message = "Invio della fattura elettronica tramite PEC non riuscito." }); }
        });

        app.MapGet("/api/sales-invoices/{id:int}/electronic-view", async (int id, SkyLab.Web.Services.SalesElectronicInvoiceService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var html = await service.ReadArchivedHtmlAsync(id, cancellationToken);
                return Results.Text(html, "text/html", System.Text.Encoding.UTF8);
            }
            catch (InvalidOperationException exception)
            {
                return Results.NotFound(new { message = exception.Message });
            }
        });

        app.MapPost("/api/sales-invoices/{id:int}/electronic-rejection", async (int id, SkyLab.Web.Services.SalesElectronicInvoiceService service, CancellationToken cancellationToken) =>
        {
            try
            {
                await service.MarkRejectedAsync(id, cancellationToken);
                return Results.Ok(new { success = true });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { success = false, message = exception.Message });
            }
        });

        if (builder.Configuration.GetValue("SkyLab:Mobile:Enabled", app.Environment.IsDevelopment()))
        {
            app.MapGet("/api/mobile/login-users", async (SkyLab.Web.Services.UserService users, CancellationToken ct) =>
                Results.Ok(await users.GetMobileLoginUsersAsync(ct)));
            app.MapPost("/api/mobile/login", async (MobileLoginRequest request, SkyLab.Web.Services.UserService users, SkyLab.Web.Services.MobileAuthService auth, CancellationToken ct) =>
                await users.VerifyMobileLoginAsync(request.Username, request.Password, ct)
                    ? Results.Ok(new { authenticated = true, token = auth.CreateSession(request.Username) })
                    : Results.Unauthorized()).RequireRateLimiting("login");
            app.MapGet("/api/mobile/my-works", async (HttpRequest request, SkyLab.Web.Services.MobileAuthService auth, SkyLab.Web.Services.WorkService works, CancellationToken ct) =>
            {
                var username = auth.GetUsername(request.Headers.Authorization);
                return username is null ? Results.Unauthorized() : Results.Ok(await works.MobileWorksAsync(username, ct));
            });
            app.MapGet("/api/mobile/outcomes", async (HttpRequest request, SkyLab.Web.Services.MobileAuthService auth, SkyLab.Web.Services.WorkService works, CancellationToken ct) =>
            {
                var username = auth.GetUsername(request.Headers.Authorization);
                return username is null ? Results.Unauthorized() : Results.Ok(await works.OutcomesAsync(ct));
            });
            app.MapGet("/api/mobile/catalog", async (HttpRequest request, SkyLab.Web.Services.MobileAuthService auth, SkyLab.Web.Services.WorkService works, CancellationToken ct) =>
            {
                var username = auth.GetUsername(request.Headers.Authorization);
                return username is null ? Results.Unauthorized() : Results.Ok(await works.WorkReferencesAsync(ct));
            });
            app.MapGet("/api/mobile/my-works/{id:int}", async (int id, HttpRequest request, SkyLab.Web.Services.MobileAuthService auth, SkyLab.Web.Services.WorkService works, CancellationToken ct) =>
            {
                var username = auth.GetUsername(request.Headers.Authorization);
                if (username is null) return Results.Unauthorized();
                var detail = await works.MobileWorkDetailAsync(id, username, ct);
                return detail is null ? Results.NotFound() : Results.Ok(detail);
            });
            app.MapPost("/api/mobile/my-works/{id:int}/report", async (int id, MobileReportRequest report, HttpRequest request, SkyLab.Web.Services.MobileAuthService auth, SkyLab.Web.Services.WorkService works, CancellationToken ct) =>
            {
                var username = auth.GetUsername(request.Headers.Authorization);
                if (username is null) return Results.Unauthorized();
                try { return Results.Ok(await works.SubmitMobileReportAsync(id, username, report, ct)); }
                catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }
            });
        }

        app.Run();
    }
}

public sealed record MobileLoginRequest(string Username, string Password);

