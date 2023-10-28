using Biwen.Settings;
using Biwen.Settings.Encryption;
using Biwen.Settings.TestWebUI.Data;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

//swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


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

    //options.Layout = "~/Views/Shared/_Layout.cshtml";
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

    //�����ṩ��,�ռ���ΪĬ��ʵ��
    options.UseEncryption<EmptyEncryptionProvider>();


    //����,���򽫳�ʼ������!
    //ʹ��EFCoreStore
    options.UseStoreOfEFCore(options =>
    {
        options.DbContextType = typeof(MyDbContext);
        options.EncryptionOption = new SettingOptions.EncryptionOptions
        {
            //Ĭ�ϲ���������
            Enable = true
        };
    });


    //��Ⱥ��֪ͨ��������
    options.NotifyOption.IsNotifyEnable = true;
    options.NotifyOption.Secret = "Biwen.Settings.Notify";
    options.NotifyOption.EndpointHosts = new[]
    {
        "http://localhost:5150"
    };

    //ʹ��JsonStore
    //options.UserStoreOfJsonFile(options =>
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



app.UseSwagger();
app.UseSwaggerUI();


app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.UseBiwenSettings();
//map api
app.MapBiwenSettingApi(mapNotifyEndpoint: true).WithTags("BiwenSettingApi").WithOpenApi();


app.Run();
