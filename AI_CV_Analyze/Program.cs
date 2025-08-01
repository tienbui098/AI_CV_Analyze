using AI_CV_Analyze.Data;
using AI_CV_Analyze.Services;
using AI_CV_Analyze.Services.Interfaces;
using AI_CV_Analyze.Services.Implementation;
using AI_CV_Analyze.Repositories.Interfaces;
using AI_CV_Analyze.Repositories.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.Loader;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Connection")));

builder.Services.AddHttpClient();

// Register Repositories
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IResumeAnalysisResultRepository, ResumeAnalysisResultRepository>();

// Register Services
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IPdfProcessingService, PdfProcessingService>();
builder.Services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();
builder.Services.AddScoped<IScoreAnalysisService, ScoreAnalysisService>();
builder.Services.AddScoped<ICVEditService, CVEditService>();
builder.Services.AddScoped<IJobRecommendationService, JobRecommendationService>();
builder.Services.AddScoped<IResumeAnalysisService, ResumeAnalysisService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
// Load native libwkhtmltox.dll nếu chạy trên Windows
var libPath = Path.Combine(Directory.GetCurrentDirectory(), "libwkhtmltox.dll");
if (File.Exists(libPath))
{
    var context = new CustomAssemblyLoadContext();
    context.LoadUnmanagedLibrary(libPath);
}
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Thêm vào sau builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    facebookOptions.Scope.Add("email");
    facebookOptions.Fields.Add("email");
    facebookOptions.Fields.Add("name");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Thêm vào trước app.MapControllerRoute
app.UseAuthentication();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

//using AI_CV_Analyze.Data;
//using AI_CV_Analyze.Services;
//using AI_CV_Analyze.Services.Implementation;
//using AI_CV_Analyze.Services.Interfaces;
//using AI_CV_Analyze.Repositories.Implementation;
//using AI_CV_Analyze.Repositories.Interfaces;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.Google;
//using Microsoft.AspNetCore.Authentication.Facebook;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddControllersWithViews();

//// Add DbContext
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("Connection")));

//builder.Services.AddHttpClient();

//// Register services and repositories
//builder.Services.AddScoped<IResumeAnalysisService, ResumeAnalysisService>();
//builder.Services.AddScoped<IResumeAnalysisResultService, ResumeAnalysisResultService>();
//builder.Services.AddScoped<IResumeAnalysisResultRepository, ResumeAnalysisResultRepository>();
//builder.Services.AddScoped<IEmailSender, EmailSender>();
//builder.Services.AddScoped<AI_CV_Analyze.Services.Interfaces.IEmailSender, AI_CV_Analyze.Services.Implementation.EmailSender>();
//builder.Services.AddLogging(logging =>
//{
//    logging.AddConsole();
//    logging.SetMinimumLevel(LogLevel.Information);
//});

//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(10);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// Add authentication
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//})
//.AddCookie()
//.AddGoogle(googleOptions =>
//{
//    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
//})
//.AddFacebook(facebookOptions =>
//{
//    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
//    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
//    facebookOptions.Scope.Add("email");
//    facebookOptions.Fields.Add("email");
//    facebookOptions.Fields.Add("name");
//});

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}
//else
//{
//    app.UseDeveloperExceptionPage();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//app.UseSession();
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();