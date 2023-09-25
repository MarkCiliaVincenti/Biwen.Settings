﻿using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Biwen.Settings
{
    /// <summary>
    /// T变更提醒
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INotify<T> where T : ISetting, new()
    {
        /// <summary>
        /// 实现提醒业务
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        Task NotifyAsync(T setting);

        /// <summary>
        /// 是否异步执行(true无需等待)
        /// </summary>
        bool IsAsync { get; }


    }

    public abstract class BaseNotify<T> : INotify<T> where T : ISetting, new()
    {
        public abstract Task NotifyAsync(T setting);
        /// <summary>
        /// 默认异步执行
        /// </summary>
        public virtual bool IsAsync => true;
    }

    /// <summary>
    /// Medirator Of T
    /// </summary>
    internal interface IMedirator
    {
        /// <summary>
        /// Publish T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="event"></param>
        /// <returns></returns>
        Task PublishAsync<T>(T @event) where T : ISetting, new();
    }

    internal class Medirator : IMedirator
    {
        private readonly IServiceProvider _serviceProvider;
        public Medirator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task PublishAsync<T>(T @event) where T : ISetting, new()
        {
            var notifys = _serviceProvider.GetServices<INotify<T>>();
            foreach (var notify in notifys)
            {
                if (notify.IsAsync)
                {
                    _ = notify.NotifyAsync(@event);
                }
                else
                {
                    await notify.NotifyAsync(@event);
                }
            }

        }
    }
}


namespace Biwen.Settings.EndpointNotify
{
    internal class Consts
    {
        /// <summary>
        /// route
        /// </summary>
        public const string EndpointUrl = "biwensettings/nofity/qwertyuiopasdfghjklzxcvbnm123/{secret}";
        /// <summary>
        /// cache key format
        /// </summary>
        public const string CacheKeyFormat = "SettingManager_{1}_{0}";
    }


    /// <summary>
    /// 通知DTO
    /// </summary>
    internal class NofityDto
    {
        public string SettingType { get; set; } = null!;
        public string ProjectId { get; set; } = null!;
    }


    /// <summary>
    /// 通知服务
    /// </summary>
    internal class NotifyServices
    {
        private readonly IOptions<SettingOptions> _options;
        public NotifyServices(IOptions<SettingOptions> options)
        {
            _options = options;
        }

        public async Task NotifyConsumerAsync(NofityDto dto)
        {
            if (!_options.Value.NotifyOption.IsNotifyEnable)
            {
                return;
            }
            if (_options.Value.NotifyOption.EndpointHosts.Length == 0)
            {
                return;
            }

            foreach (var host in _options.Value.NotifyOption.EndpointHosts)
            {
                _ = Task.Run(async () =>
                 {
                     var url = $"{host}/{Consts.EndpointUrl.Replace("{secret}", _options.Value.NotifyOption.Secret)}";
                     using HttpClient httpClient = new();
                     httpClient.DefaultRequestHeaders.Clear();
                     httpClient.DefaultRequestHeaders.Add("User-Agent", "Biwen.Settings");
                     httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                     await httpClient.PostAsJsonAsync(url, dto);
                 });

                Console.WriteLine($"NotifyConsumerAsync:{host}");
            }

            await Task.CompletedTask;
        }
    }
}