using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OnlineLearningApp;
using OnlineLearningApp.Data;
using OnlineLearningApp.Data.Cart;
using OnlineLearningApp.Data.Services;
using OnlineLearningApp.Data.Services.Implementation;
using OnlineLearningApp.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<OnlineLearningAppDbContext>(options => options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 23)), o => o.EnableRetryOnFailure()));
builder.Services.AddIdentity<Account, IdentityRole>(options => { options.SignIn.RequireConfirmedAccount=false; options.User.RequireUniqueEmail=true; options.Password.RequiredLength=8; options.Password.RequireDigit=true; options.Password.RequireLowercase=true; options.Password.RequireUppercase=true; options.Password.RequireNonAlphanumeric=true; options.Lockout.AllowedForNewUsers=true; options.Lockout.MaxFailedAccessAttempts=5; options.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15); }).AddEntityFrameworkStores<OnlineLearningAppDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => { options.Cookie.Name="TBA.Datalytics.Auth"; options.Cookie.HttpOnly=true; options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest; options.Cookie.SameSite=SameSiteMode.Lax; options.LoginPath="/Account/SignIn"; options.AccessDeniedPath="/Account/AccessDenied"; options.ExpireTimeSpan=TimeSpan.FromHours(8); options.SlidingExpiration=true; });
builder.Services.AddScoped<ICourseService, CourseService>(); builder.Services.AddScoped<IModuleService, ModuleService>(); builder.Services.AddScoped<IOrderService, OrderService>(); builder.Services.AddScoped<IOptionService, OptionService>(); builder.Services.AddScoped<IQuestionService, QuestionService>(); builder.Services.AddScoped<IQuizService, QuizService>(); builder.Services.AddScoped<ICourseCompletionService, CourseCompletionService>();
builder.Services.AddHttpClient<IPaymentGateway, PaystackPaymentGateway>(client => client.BaseAddress = new Uri("https://api.paystack.co/"));
builder.Services.AddHttpContextAccessor(); builder.Services.AddScoped<ShoppingCart>(sp => ShoppingCart.GetShoppingCart(sp));
var redisConnection=builder.Configuration["REDIS_URL"]; if(!string.IsNullOrWhiteSpace(redisConnection)) builder.Services.AddStackExchangeRedisCache(o=>o.Configuration=redisConnection); else builder.Services.AddDistributedMemoryCache();
builder.Services.AddHealthChecks(); builder.Services.AddSession(options=>{options.IdleTimeout=TimeSpan.FromMinutes(30);options.Cookie.Name="TBA.Datalytics.Session";options.Cookie.HttpOnly=true;options.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;options.Cookie.SameSite=SameSiteMode.Lax;options.Cookie.IsEssential=true;});
var app=builder.Build();
var forwardedHeaders=new ForwardedHeadersOptions{ForwardedHeaders=ForwardedHeaders.XForwardedFor|ForwardedHeaders.XForwardedProto}; forwardedHeaders.KnownNetworks.Clear(); forwardedHeaders.KnownProxies.Clear(); app.UseForwardedHeaders(forwardedHeaders);
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Home/Error");app.UseHsts();app.UseHttpsRedirection();}
app.UseStaticFiles(); app.UseRouting(); app.UseSession(); app.UseAuthentication(); app.UseAuthorization(); app.MapHealthChecks("/health"); app.MapControllerRoute(name:"default",pattern:"{controller=Course}/{action=Index}/{id?}");
await AppInitializer.InitializeAsync(app.Services); app.Run();
