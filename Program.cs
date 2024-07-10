using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpLogging;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

//swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình dịch vụ logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(path: "info-logs.txt", restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File(path: "error-logs.txt", restrictedToMinimumLevel: LogEventLevel.Error) 
                .CreateLogger();

builder.Logging.AddSerilog(_logger);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            
            policy.WithOrigins("https://resume-management-system.vercel.app")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.MaxDepth = 64; // hoặc giá trị lớn hơn nếu cần thiết
    });


builder.Services.AddControllers();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidAudience = config["JwtSettings:Audience"],
#pragma warning disable CS8604 // Possible null reference argument.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JwtSettings:SignKey"]))
#pragma warning restore CS8604 // Possible null reference argument.
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("AuthToken"))
            {
                context.Token = context.Request.Cookies["AuthToken"];
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

// var staticFilesPath = Path.Combine(builder.Environment.ContentRootPath, "StaticFiles");
// if (!Directory.Exists(staticFilesPath))
// {
//     Directory.CreateDirectory(staticFilesPath);
// }

// var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider( 
//            Path.Combine(builder.Environment.ContentRootPath, "StaticFiles")),
//     RequestPath = "/static-files",
//     OnPrepareResponse = ctx =>
//     {
//         ctx.Context.Response.Headers.Append(
//              "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
//     }
// });

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

