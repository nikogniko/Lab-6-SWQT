using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SnippetsLibraryWebApp.Models;
using SnippetsLibraryWebApp.Repository;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Authentication;
using SnippetsLibraryWebApp.Extensions;


var builder = WebApplication.CreateBuilder(args);

// Íàëàøòóâàííÿ ï³äêëþ÷åííÿ äî áàçè äàíèõ
builder.Configuration.GetConnectionString("DefaultConnection");

// Äîäàéòå ³íø³ ñëóæáè
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ProgrammingLanguageRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<TagRepository>();
builder.Services.AddScoped<SnippetRepository>();

// Налаштування аутентифікації (використовуючи кукі)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Error";

        // Налаштування подій для зміни поведінки при неавтентифікованому доступі
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") || context.Request.IsAjaxRequest())
            {
                context.Response.StatusCode = 401;
            }
            else
            {
                context.Response.Redirect("/"); // Перенаправлення на головну сторінку
            }
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") || context.Request.IsAjaxRequest())
            {
                context.Response.StatusCode = 403;
            }
            else
            {
                context.Response.Redirect("/"); // Перенаправлення на головну сторінку
            }
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

// Íàëàøòóâàííÿ middleware (Configure the HTTP request pipeline.)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Snippets}/{action=AllSnippets}/{id?}");

app.MapControllerRoute(
    name: "CreateSnippet",
    pattern: "{controller=Snippets}/{action=CreateSnippetAsync}");

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");
//
//app.MapControllerRoute(
//    name: "addSnippet",
//    pattern: "{controller=AddSnippet}/{action=AddSnippet}/{id?}");
//
//app.MapControllerRoute(
//    name: "editSnippet",
//    pattern: "{controller=EditSnippet}/{action=EditSnippet}/{id?}");
//
//app.MapControllerRoute(
//    name: "snippetDetails", 
//    pattern: "{controller=SnippetDetails}/{action=SnippetDetails}/{id?}");
//
//app.MapControllerRoute(
//    name: "personalSnippets",
//    pattern: "{controller=PersonalSnippets}/{action=PersonalSnippets}/{id?}");
//
//app.MapControllerRoute(
//    name: "savedSnippets",
//    pattern: "{controller=SavedSnippets}/{action=SavedSnippets}/{id?}");
//
//app.MapControllerRoute(
//    name: "tags",
//    pattern: "{controller=Tags}/{action=AddTag}/{id?}"); 
//
//app.MapControllerRoute(
//    name: "snippets",
//    pattern: "{controller=Snippets}/{action=GetAllSnippets}/{id?}");

app.Run();