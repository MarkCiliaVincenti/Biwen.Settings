using Biwen.Settings;
using Biwen.Settings.Caching;
using Biwen.Settings.TestWebUI.Data;
using Biwen.Settings.TestWebUI.Settings;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();



//ע��DbContext
builder.Services.AddDbContext<MyDbContext>(options =>
{
    //just for test
    //options.UseInMemoryDatabase("BiwenSettings");
    options.UseSqlite("Data Source=BiwenSettings.db");
});


builder.Services.AddBiwenSettings(options =>
{

#if DEBUG
    options.ProjectId = $"Biwen.Settings.TestWebUI-{"Development"}";
#endif

#if !DEBUG
    options.ProjectId = $"Biwen.Settings.TestWebUI-{"Production"}";
#endif

    options.Layout = "~/Views/Shared/_Layout.cshtml";
    options.Title = "Biwen.Settings";
    options.Route = "system/settings";
    options.HasPermission = (ctx) => true;
    options.EditorOption.EditorOnclick = "return confirm('Are You Sure!?');";
    options.EditorOption.EdtiorConfirmButtonText = "Submit";
    options.EditorOption.EditorEditButtonText = "Edit";
    options.EditorOption.ShouldPagenation = true;
    //����AutoFluentValidation
    options.AutoFluentValidationOption.Enable = true;

    //֧�ֻ����ṩ��,Ĭ�ϲ�ʹ�û���
    //��Ҳ����ʹ��Biwen.Settings�ṩ�ڴ滺��:Biwen.Settings.Caching.MemoryCacheProvider
    //options.UseCacheOfNull();
    options.UseCacheOfMemory();

    //����,���򽫳�ʼ������!
    //ʹ��EFCoreStore
    options.UseSettingManagerOfEFCore(options =>
    {
        options.DbContextType = typeof(MyDbContext);
    });

    //ʹ��JsonStore
    //options.UserSettingManagerOfJsonStore(options =>
    //{
    //    options.FormatJson = true;
    //    options.JsonPath = "1systemsetting.json";
    //});

});

//֧�ֻ����ṩ��,Ĭ�ϲ�ʹ�û���
//��Ҳ����ʹ��Biwen.Settings�ṩ�ڴ滺��:Biwen.Settings.Caching.MemoryCacheProvider
//builder.Services.AddScoped<ICacheProvider, MemoryCacheProvider>();

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
