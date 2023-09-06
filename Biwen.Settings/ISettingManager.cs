﻿using Biwen.Settings.Caching;
using Microsoft.Extensions.DependencyInjection;

namespace Biwen.Settings
{
    /// <summary>
    /// 需要实现自己的SettingManager请继承至BaseSettingManager
    /// </summary>
    public interface ISettingManager
    {
        /// <summary>
        /// 持久化存储.并刷新缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setting"></param>
        void Save<T>(T setting) where T : ISetting, new();
        /// <summary>
        /// 从缓存中获取配置.如果没有则从持久化存储中获取.如果持久层也没有则返回默认值并存入持久层
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T Get<T>() where T : ISetting, new();

        /// <summary>
        /// 从持久化存储中获取配置
        /// </summary>
        /// <returns></returns>
        List<Setting> GetAllSettings();

        /// <summary>
        /// 从持久层获取指定类型的配置
        /// </summary>
        /// <param name="settingType"></param>
        /// <returns></returns>
        Setting? GetSetting(string settingType);


    }

    /// <summary>
    /// SettingManager的装饰器基类
    /// </summary>
    internal sealed class BaseSettingManagerDecorator : ISettingManager
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly ISettingManager _settingManager;
        private readonly ICacheProvider _cacheProvider;

        public BaseSettingManagerDecorator(
            ISettingManager settingManager,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _settingManager = settingManager;
            _cacheProvider = serviceProvider.GetRequiredService<ICacheProvider>();
        }

        private const string CacheKeyFormat = "SettingManager_{0}";

        public async void Save<T>(T setting) where T : ISetting, new()
        {
            //Save
            _settingManager.Save(setting);
            //Remove Cache
            _cacheProvider.Remove(string.Format(CacheKeyFormat, typeof(T).Name));
            //Notify
            var notiyfys = _serviceProvider.GetServices<INotify<T>>();
            foreach (var notify in notiyfys)
            {
                if (notify.IsAsync)
                {
                    _ = notify.NotifyAsync(setting);
                }
                else
                {
                    await notify.NotifyAsync(setting);
                }
            }
        }

        public T Get<T>() where T : ISetting, new()
        {
            return (T)_cacheProvider.GetOrCreate(string.Format(CacheKeyFormat, typeof(T).FullName), () =>
            {
                return _settingManager.Get<T>();
            });
        }

        public List<Setting> GetAllSettings()
        {
            return _settingManager.GetAllSettings();
        }

        public Setting? GetSetting(string settingType)
        {
            return _settingManager.GetSetting(settingType);
        }
    }


    /// <summary>
    /// BaseSettingManager
    /// </summary>
    public abstract class BaseSettingManager : ISettingManager
    {

        protected readonly ILogger<ISettingManager> _logger;

        public BaseSettingManager(ILogger<ISettingManager> logger)
        {
            _logger = logger;
        }

        public virtual void Save<T>(T setting) where T : ISetting, new()
        {
            throw new NotImplementedException();
        }

        public virtual T Get<T>() where T : ISetting, new()
        {
            throw new NotImplementedException();
        }

        public virtual List<Setting> GetAllSettings()
        {
            throw new NotImplementedException();
        }

        public virtual Setting? GetSetting(string settingType)
        {
            throw new NotImplementedException();
        }

    }
}