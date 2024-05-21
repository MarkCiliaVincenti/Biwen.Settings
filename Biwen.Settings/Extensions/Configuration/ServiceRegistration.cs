﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Biwen.Settings.Extensions.Configuration
{
    public static class ServiceRegistration
    {
        internal static IServiceCollection AddBiwenSettingConfiguration(this IServiceCollection services)
        {
            //ConfigurationMediratorDoneHandler
            services.AddSingleton<IMediratorDoneHandler, ConfigurationMediratorDoneHandler>();
            return services;
        }

        /// <summary>
        /// 提供对IConfiguration,IOptions的支持
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="autoRefresh"></param>
        /// <returns></returns>
        public static ConfigurationManager AddBiwenSettingConfiguration(
            this ConfigurationManager manager, IServiceCollection serviceDescriptors, bool autoRefresh = true)
        {
            var sp = Settings.ServiceRegistration.ServiceProvider ?? throw new BiwenException("必须首先注册Biwen.Setting模块,请调用:services.AddBiwenSettings()");
            //添加订阅
            if (autoRefresh)
            {
                serviceDescriptors.AddBiwenSettingConfiguration();
            }
            IConfigurationBuilder configBuilder = manager;
            configBuilder.Add(new BiwenSettingConfigurationSource(autoRefresh));
            var settings = ASS.InAllRequiredAssemblies.ThatInherit(typeof(ISetting)).Where(x => x.IsClass && !x.IsAbstract).ToList();
            //注册ISetting
            settings.ForEach(x =>
            {
                //IOptions DI
                manager?.GetSection(x.Name).Bind(GetSetting(x, sp));
            });
            return manager;
        }

        static object GetSetting(Type x, IServiceProvider sp)
        {
            var settingManager = sp.GetRequiredService<ISettingManager>();
            var cache = sp.GetRequiredService<IMemoryCache>();

            //使用缓存避免重复反射
            var md = cache.GetOrCreate($"GenericMethod_{x.FullName}", entry =>
            {
                MethodInfo methodLoad = settingManager.GetType().GetMethod(nameof(settingManager.Get))!;
                MethodInfo generic = methodLoad.MakeGenericMethod(x);
                return generic;
            });
            return md!.Invoke(settingManager, null)!;
        }

    }
}