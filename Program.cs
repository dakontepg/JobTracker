using System.Net;
using Firebase.Auth;
using Firebase.Auth.Providers;
using FirebaseAdmin;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Google.Cloud.Firestore;
using JobTracker.Services;
using Google.Apis.Auth.OAuth2;
using JobTracker.Models;


var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json and service-account.json 
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true); 
builder.Configuration.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(),"service-account.json"), optional: false, reloadOnChange: true);

// Bind Firebase setting
builder.Services.Configure<FirebaseSettings>(builder.Configuration.GetSection("Firebase"));

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Read Firebase settings from configuration 
var firebaseSettings = builder.Configuration.GetSection("Firebase").Get<FirebaseSettings>();

// Construct file path based on environment 
string credentialsFileLocation; 
if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "service-account.json"))) 
{ 
    credentialsFileLocation = Path.Combine(Directory.GetCurrentDirectory(), "service-account.json"); 
} 
else 
{ 
    credentialsFileLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "service-account.json"); 
}
var firebaseProjectName = firebaseSettings.ProjectId;
string firebaseApiKey = firebaseSettings.ApiKey;

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",credentialsFileLocation);

// Ensure FirebaseApp is created and available globally
var defaultApp = FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(credentialsFileLocation)
});

builder.Services.AddSingleton(defaultApp);

// Configure Firestore here, must be before the builder for authentication because authentication depends upon the database now
builder.Services.AddSingleton(provider =>
{
    var builder = new FirestoreDbBuilder
    {
        ProjectId = "jobtracker-b7365",
        JsonCredentials=File.ReadAllText(credentialsFileLocation)
    };
    return builder.Build();
});

// Authentication setup
builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig
{
    ApiKey = firebaseApiKey,
    AuthDomain = $"{firebaseProjectName}.firebaseapp.com",
    Providers = new FirebaseAuthProvider[]
    {
        new EmailProvider(),
        new GoogleProvider()
    }
}));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectName}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectName}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectName,
            ValidateLifetime = true,
            RoleClaimType= "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });

builder.Services.AddAuthorization();

// Register FirebaseAuthService
builder.Services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();

//Register UserService with required dependencies
builder.Services.AddSingleton<UserService>(provider =>
{
    var firestoreDb = provider.GetRequiredService<FirestoreDb>();
    var logger = provider.GetRequiredService<ILogger<UserService>>();
    var firebaseAuthService = provider.GetRequiredService<IFirebaseAuthService>();
    return new UserService(firestoreDb, logger, firebaseAuthService);
});

builder.Services.AddLogging(LoggingBuilder =>
{
    LoggingBuilder.AddConsole();
    LoggingBuilder.AddDebug();
});


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

app.UseSession();

app.Use(async (context, next) =>
{
    var token = context.Session.GetString("token");
    if(!string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// this code section verifies the token, claims for the user and writes it to the terminal;  helpful in checking that user's had roles
// app.Use(async (context, next) =>
// {
//     if (context.User.Identity.IsAuthenticated)
//     {
//         var claims = context.User.Claims;
//         foreach (var claim in claims)
//         {
//             Console.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
//         }
//     }

//     await next();
// });


app.MapControllerRoute(
    name: "default",
    // pattern: "{controller=Home}/{action=Index}/{id?}");
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
