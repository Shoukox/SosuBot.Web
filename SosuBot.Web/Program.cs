using Microsoft.Extensions.FileProviders;
using SosuBot.Web.Constants;
using SosuBot.Web.Hubs;
using SosuBot.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHostedService<ConfigureRabbitMqBackgroundService>();
builder.Services.AddSingleton<RabbitMqService>();

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
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(FilePathConstants.VideoPath),
    RequestPath = "/Videos"
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions()
{
    FileProvider = new PhysicalFileProvider(FilePathConstants.VideoPath),
    RequestPath = "/Videos",
});

app.UseRouting();

app.UseAuthorization();

app.MapHub<RenderJobHub>("/render-job-hub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();