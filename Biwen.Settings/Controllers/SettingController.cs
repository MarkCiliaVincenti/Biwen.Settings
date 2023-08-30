﻿using Biwen.Settings.Domains;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Biwen.Settings.Controllers
{
    public class SettingController : Controller
    {
        private readonly IOptions<SettingOptions> _options;
        private readonly ISettingManager _settingManager;

        public SettingController(IOptions<SettingOptions> options, ISettingManager settingManager)
        {
            _options = options;
            _settingManager = settingManager;
        }

        //[HttpGet("qwertyuiopasdfghjklzxcvbnm/setting")]
        public IActionResult Index()
        {
            var isValid = _options.Value.Valider.Invoke(Request.HttpContext);

            if (!isValid)
                return Unauthorized();

            var all = _settingManager.GetAllSettings();

            //移除的或者无效的配置 需要排除
            var settings = all.Where(
                s => TypeFinder.FindTypes.InAllAssemblies.Any(x => x.FullName == s.SettingName));

            ViewBag.Settings = settings;

            return View();
        }

        public IActionResult Edit(string id)
        {
            var isValid = _options.Value.Valider.Invoke(Request.HttpContext);
            if (!isValid)
                return Unauthorized();

            if (string.IsNullOrEmpty(id))
                return NotFound();

            var setting = _settingManager.GetAllSettings().FirstOrDefault(x => x.SettingName == id);
            if (setting == null)
                return NotFound();

            var type = TypeFinder.FindTypes.InAllAssemblies.FirstOrDefault(x => x.FullName == setting.SettingName);
            if (type == null)
                return NotFound();

            ViewBag.Setting = setting;
            ViewBag.SettingValues = SettingValues(setting);

            return View();
        }

        [NonAction]
        List<(string, string?, string?)> SettingValues(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            var type = TypeFinder.FindTypes.InAllAssemblies.FirstOrDefault(x => x.FullName == setting.SettingName);
            if (type == null)
                throw new ArgumentNullException(nameof(type));


            List<(string, string?, string?)> SettingValues = new();
            var json = JsonObject.Parse(setting.SettingContent!)!;
            type.GetProperties().Where(x =>
                x.Name != nameof(Setting.SettingName) &&
                x.Name != nameof(Setting.ProjectId) &&
                x.Name != nameof(Setting.Order) &&
                x.Name != nameof(Setting.Description) &&
                //x.Name != nameof(Setting.Version) &&
                x.Name != nameof(Setting.SettingContent) &&
                x.Name != nameof(Setting.LastModificationTime)).ToList().ForEach(x =>
                {
                    var instanceSetting = Request.HttpContext.RequestServices.GetService(type);
                    var desc = x.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

                    var value = json[x.Name];
                    if (value != null)
                    {
                        SettingValues.Add((x.Name, desc?.Description, value.ToString()));
                    }
                    else
                    {
                        var propertyValue = type.GetProperty(x.Name)!.GetValue(instanceSetting);
                        SettingValues.Add((x.Name, desc?.Description, propertyValue == null ? string.Empty : propertyValue.ToString()));
                    }
                });

            return SettingValues;
        }


        [HttpPost]
        public IActionResult Edit(string id, IFormCollection form)
        {
            var isValid = _options.Value.Valider.Invoke(Request.HttpContext);
            if (!isValid)
                return Unauthorized();

            var type = TypeFinder.FindTypes.InAllAssemblies.FirstOrDefault(x => x.FullName == id);
            if (type == null)
                return NotFound();

            var setting = Request.HttpContext.RequestServices.GetService(type)!;

            foreach (string? item in form.Keys)
            {
                PropertyInfo prop = null!;
                try
                {
                    prop = setting.GetType().GetProperty(item)!;
                }
                catch
                {
                    //todo:
                }

                //SetMethod 判断
                if (prop?.SetMethod == null)
                {
                    continue;
                }
                //当前类型必须能转换String
                if (!TypeDescriptor.GetConverter(prop.PropertyType).CanConvertFrom(typeof(string)))
                    continue;
                //当前类型必须能转换传递的参数值
                var strValue = form[item].ToString();
                if (!TypeDescriptor.GetConverter(prop.PropertyType).IsValid(strValue))
                    continue;
                //转换
                var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromInvariantString(strValue);
                //赋值
                prop.SetValue(setting, value);

            }

            //验证DTO
            var validator = Request.HttpContext.RequestServices.GetService(serviceType: typeof(IValidator<>).MakeGenericType(type));
            if (validator != null)
            {

                var md = validator.GetType().GetMethods().First(x => !x.IsGenericMethod && x.Name == "Validate");
                var result = md!.Invoke(validator, new object[] { setting! }) as ValidationResult;
                //验证不通过的情况
                if (result != null && !result!.IsValid)
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError(item.PropertyName, item.ErrorMessage);
                    }
                    var domainSetting = _settingManager.GetAllSettings().First(x => x.SettingName == id);
                    ViewBag.Setting = domainSetting!;
                    ViewBag.SettingValues = SettingValues(domainSetting!);
                    //验证不通过
                    return View();
                }
            }

            MethodInfo methodLoad = _settingManager.GetType().GetMethod("Save")!;
            MethodInfo generic = methodLoad.MakeGenericMethod(type);
            generic.Invoke(_settingManager, new object[] { setting });
            //_settingManager.Save(sitting as SettingBase);
            return RedirectToAction("Edit", new { id });

        }

    }
}
