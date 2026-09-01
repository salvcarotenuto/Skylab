using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace SkyLab.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole();

        // Add services to the container.
        builder.Services.AddRazorPages()
            .AddMvcOptions(options =>
                options.ModelBinderProviders.Insert(0, new SkyLab.Web.Infrastructure.FlexibleDecimalModelBinderProvider()));
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddSingleton<SkyLab.Web.Services.InterventionService>();
        builder.Services.AddScoped<SkyLab.Web.Services.PlanningService>();
        builder.Services.AddScoped<SkyLab.Web.Services.CustomerService>();
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

        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/api/mobile/login-users", async (SkyLab.Web.Services.UserService users, CancellationToken ct) =>
                Results.Ok(await users.GetMobileLoginUsersAsync(ct)));
            app.MapPost("/api/mobile/login", async (MobileLoginRequest request, SkyLab.Web.Services.UserService users, SkyLab.Web.Services.MobileAuthService auth, CancellationToken ct) =>
                await users.VerifyMobileLoginAsync(request.Username, request.Password, ct)
                    ? Results.Ok(new { authenticated = true, token = auth.CreateSession(request.Username) })
                    : Results.Unauthorized());
            app.MapGet("/api/mobile/my-works", async (HttpRequest request, SkyLab.Web.Services.MobileAuthService auth, SkyLab.Web.Services.WorkService works, CancellationToken ct) =>
            {
                var username = auth.GetUsername(request.Headers.Authorization);
                return username is null ? Results.Unauthorized() : Results.Ok(await works.MobileWorksAsync(username, ct));
            });
        }

        app.Run();
    }
}

public sealed record MobileLoginRequest(string Username, string Password);
