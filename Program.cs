using PEI_Doctors.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<DoctorMonitorService>();

// Add Vite dev server service to automatically start the dev server
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<PEI_Doctors.Services.ViteDevServerService>();
}

// Configure CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Configure SPA
if (app.Environment.IsDevelopment())
{
    app.MapWhen(context => !context.Request.Path.Value!.StartsWith("/api"), appBuilder =>
    {
        appBuilder.UseSpa(spa =>
        {
            spa.Options.SourcePath = "ClientApp";
            // Get the dev server URL from environment variable (set by SPA proxy) or use default
            var devServerUrl = builder.Configuration["ASPNETCORE_SpaProxyServerUrl"] ?? "http://127.0.0.1:5173";
            // The SPA proxy package will automatically start the dev server
            spa.UseProxyToSpaDevelopmentServer(devServerUrl);
        });
    });
}
else
{
    app.UseSpa(spa =>
    {
        spa.Options.SourcePath = "ClientApp";
        spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                Path.Combine(builder.Environment.ContentRootPath, "ClientApp/dist")),
            RequestPath = ""
        };
    });
}

app.Run();
