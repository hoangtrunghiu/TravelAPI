using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TravelAPI.Data;
using TravelAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình database
builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TravelContext") ?? throw new InvalidOperationException("Connection string 'TravelDbContext' not found.")));

// Add services to the container.
//them cai nay
// Config cho Identity
builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<TravelDbContext>()
                .AddDefaultTokenProviders();
// Config cho Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Config cho JWT
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});
builder.Services.AddControllers();
//them cai nay
// Config CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});
//cấu hình JSON Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

//end
// Cấu hình Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TravelAPI", Version = "v1" });

    // Hỗ trợ upload file
    options.OperationFilter<AddFileUploadFilter>();
});

// builder.Services.AddOpenApi();
//Thêm service này để sử dụng StoredProcedureService lấy dữ liệu từ store procedure
builder.Services.AddScoped<StoredProcedureService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

app.MapControllers();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();


app.Run();
