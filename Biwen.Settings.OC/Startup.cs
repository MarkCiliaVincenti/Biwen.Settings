using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace Biwen.Settings.OC
{


    [Feature("Biwen.Settings.OC")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {

            using var sp = services.BuildServiceProvider().CreateScope();
            //var shell = sp.ServiceProvider.GetRequiredService<IShellConfiguration>();
            var settings = sp.ServiceProvider.GetRequiredService<ShellSettings>();

            var jsonPath = settings.ShellConfiguration.GetValue("Biwen.Settings:JsonFilePath", "systemsetting.json");
            var projectId = settings.ShellConfiguration.GetValue("Biwen.Settings:ProjectId", "Default");

            services.AddBiwenSettings(options =>
            {

                options.ProjectId = $"{settings.TenantId}-{projectId}";
                options.Title = "Biwen.Settings";
                options.HasPermission = (ctx) =>
                {
                    //�ж��Ƿ���Ȩ�޷�������ҳ��
                    return ctx.RequestServices
                    .GetService<IAuthorizationService>()
                    .AuthorizeAsync(ctx.User, Permissions.ManageSettings).GetAwaiter().GetResult();
                };
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

                //ʹ��JsonStore
                options.UserSettingManagerOfJsonStore(options =>
                {
                    options.FormatJson = true;
                    options.JsonPath = jsonPath;
                });
            });
            //Ȩ��
            services.AddScoped<IPermissionProvider, Permissions>();
            //�˵�
            services.AddScoped<INavigationProvider, AdminMenu>();
            //������
            services.AddScoped<Biwen.Settings.Controllers.SettingController>();

        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {


            var settingOption = serviceProvider.GetRequiredService<IOptions<SettingOptions>>();
            if (settingOption.Value.EditorOption.ShouldPagenation)
            {
                //���Ƕ��ʽ��Դ
                var embeddedFileProvider = new EmbeddedFileProvider(typeof(ISetting).Assembly, "Biwen.Settings");

                builder.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = embeddedFileProvider,
                    OnPrepareResponse = ctx =>
                    {
                        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
                    }
                });
            }


            routes.MapAreaControllerRoute(
                   name: "settingRouteIndex",
                   areaName: "Biwen.Settings",
                   pattern: settingOption.Value.Route,
                   defaults: new { controller = "Setting", action = "Index" });

            routes.MapAreaControllerRoute(
                   name: "settingRouteEdit",
                   areaName: "Biwen.Settings",
                   pattern: "biwen/settings/setting/edit/{id}",
                   defaults: new { controller = "Setting", action = "Edit" });
        }

    }
}
