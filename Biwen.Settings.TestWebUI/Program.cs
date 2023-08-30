using Biwen.Settings;
using Biwen.Settings.TestWebUI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();



//ע��DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
{
    //options.UseInMemoryDatabase("BiwenSettings");
    options.UseSqlite("Data Source=BiwenSettings.db");
});


builder.Services.AddBiwenSettings(typeof(MyDbContext), options =>
{

    //#if DEBUG
    //    options.ProjectId = $"Biwen.Settings.TestWebUI-{"Development"}";
    //#endif

#if DEBUG
    options.ProjectId = $"Biwen.Settings.TestWebUI-{"Production"}";
#endif

    options.Layout = "~/Views/Shared/_Layout.cshtml";
    options.Title = "Biwen.Settings";
    options.Route = "system/settings";
    options.Valider = (context) =>
    {
        return true;
    };
}, true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();


app.UseBiwenSettings();


app.Run();
