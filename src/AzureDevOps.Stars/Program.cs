using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Logging;
using System.Diagnostics;
using AzureDevOps.Stars.Services;

IdentityModelEventSource.ShowPII = Debugger.IsAttached;
// Prevents the default claim transformations done by the authentication middleware.
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// User: test@gabrielbourgaultkiosoft.onmicrosoft.com
// Pass: Gabi1234

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
    .AddCookie()
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        //options.Authority = "https://login.microsoftonline.com/7ba8d2fb-4660-4a19-802e-4d015a17e167/v2.0/";
        //options.ClientId = "ad92dc86-3360-479b-89af-66b1ff2168cf"; // TODO: Set value
        //options.ClientSecret = "ZyR8Q~qQ.xhI7a5-L-WnrBqJ.FypkEtmfvbUxaPZ"; // TODO: Set value

        options.Authority = "https://login.microsoftonline.com/6aaaeb3c-37b6-4b5d-94ec-292ed902a99d/v2.0/";
        options.ClientId = "24215236-fb31-4e26-b688-8fbf2d13d660";
        options.ClientSecret = "qJa8Q~52U38mKxLRCTzY3iSs_7zQu-gYVaGlBbW9";
        options.ResponseType = "code";
        options.SaveTokens = true; // Specifies whether access_tokens and refresh_tokens should be stored. Required when the app calls APIs
        options.GetClaimsFromUserInfoEndpoint = false;
        options.ClaimActions.MapAll();

        options.Scope.Clear();
		// "https://app.vssps.visualstudio.com/user_impersonation"
		foreach (string scope in new List<string> { "openid", "profile", "email", "499b84ac-1321-427f-aa17-267ca6975798/.default" })
        {
            options.Scope.Add(scope);
        }

        // Specifies which claim type to use for the `User.Identity.Name` and `User.IsInRole()`.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        options.Events.OnTokenValidated = context =>
        {
            // Issue a token with a fixed expiration
            context.Properties.IsPersistent = true;

            // Set the expiration of the cookie for the same expiration as the access_token
            var accessToken = new JwtSecurityToken(context.TokenEndpointResponse.AccessToken);
            context.Properties.ExpiresUtc = accessToken.ValidTo;
            context.Properties.IssuedUtc = accessToken.ValidFrom;

            return Task.CompletedTask;
        };
    });

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IStarService, StarService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
