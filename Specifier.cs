using NUnit.Framework.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace Documentation;

public class Specifier<T> : ISpecifier
{
	public string GetApiDescription()
	{
		ApiDescriptionAttribute apiDescription = (ApiDescriptionAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(ApiDescriptionAttribute));
		if (apiDescription != null)
		{
			return apiDescription.Description;
		}

		return null;
	}

	public string[] GetApiMethodNames()
    {
		var apiMethodsNames = typeof(T).GetMethods()
			.Where(s => s.GetCustomAttribute(typeof(ApiMethodAttribute), false) != null)
			.Select(a => a.Name)
			.ToArray();
		return apiMethodsNames;
	}

	public string GetApiMethodDescription(string methodName)
	{
        var methodInfo = typeof(T).GetMethod(methodName);
		if (IsApiMethod(methodInfo))
		{
            var description = (ApiDescriptionAttribute)methodInfo.GetCustomAttribute(typeof(ApiDescriptionAttribute), false);
            return description?.Description;
        }

        return null;
	}

    public string[] GetApiMethodParamNames(string methodName)
	{
		var methodInfo = typeof(T).GetMethod(methodName);
		if (IsApiMethod(methodInfo))
		{
			var paramNames = methodInfo.GetParameters()
				.Select(p => p.Name)
				.ToArray();
			if (paramNames.Length > 0)
			{
				return paramNames;
			}
		}

		return null;
    }

    public string GetApiMethodParamDescription(string methodName, string paramName)
	{
        var methodInfo = typeof(T).GetMethod(methodName);
        if (IsApiMethod(methodInfo))
        {
			var paramInfo = GetParameterInfo(methodInfo, paramName);
			if (paramInfo != null)
            {
                var paramAttribute = (ApiDescriptionAttribute)paramInfo.GetCustomAttribute(typeof(ApiDescriptionAttribute), false);
                return paramAttribute? .Description;
            }
        }

        return null;
    }

    public ApiParamDescription GetApiMethodParamFullDescription(string methodName, string paramName)
	{
		ApiParamDescription fullDescription = new()
		{
			ParamDescription = new CommonDescription(paramName, GetApiMethodParamDescription(methodName, paramName))
		};

		var methodInfo = typeof(T).GetMethod(methodName);
		if (IsApiMethod(methodInfo))
		{
			var paramInfo = GetParameterInfo(methodInfo, paramName);
			if (paramInfo != null)
			{
				FillAttributes(fullDescription, paramInfo.GetCustomAttributes(false));
			}
		}

		return fullDescription;
    }

    public ApiMethodDescription GetApiMethodFullDescription(string methodName)
	{
        var methodInfo = typeof(T).GetMethod(methodName);

        if (IsApiMethod(methodInfo))
		{
            ApiMethodDescription fullDescription = new()
            {
                MethodDescription = new CommonDescription(methodName, GetApiMethodDescription(methodName)),
                ReturnDescription = GetReturnValueDescription(methodInfo),
            };

            var methodParams = GetApiMethodParamNames(methodName);
            if (methodParams != null)
            {
                fullDescription.ParamDescriptions = GetParamsDescriptions(methodParams, methodName);
            }

            return fullDescription;
        }

		return null;
    }

    private ApiParamDescription[] GetParamsDescriptions(string[] methodParams, string methodName)
    {
        var paramsDescriptions = methodParams
                    .Select(p => GetApiMethodParamFullDescription(methodName, p))
                    .ToArray();

        return paramsDescriptions;
    }

	private static ApiParamDescription? GetReturnValueDescription(MethodInfo methodInfo)
	{
        var returnValueAttributes = methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(false);
        if (returnValueAttributes.Length > 0)
        {
            ApiParamDescription returnDescription = new();
            FillAttributes(returnDescription, returnValueAttributes);
            return returnDescription;
        }

        return null;
	}

    private static ParameterInfo? GetParameterInfo(MethodInfo methodInfo, string paramName)
    {
        var paramInfo = methodInfo.GetParameters()
                .Where(p => p.Name == paramName)
                .Select(p => p)
                .FirstOrDefault();

        return paramInfo ?? null;
    }

    private static void FillAttributes(ApiParamDescription fullDescription, object[] customAttributes)
    {
        foreach (var attr in customAttributes)
        {
            if (attr is ApiIntValidationAttribute valAttribute)
            {
                fullDescription.MinValue = valAttribute.MinValue;
                fullDescription.MaxValue = valAttribute.MaxValue;
            }

            if (attr is ApiRequiredAttribute reqAttribute)
            {
                fullDescription.Required = reqAttribute.Required;
            }
        }
    }

    private static bool IsApiMethod(MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            return false;
        }

        var apiAttribute = methodInfo.GetCustomAttributes(true)
                .OfType<ApiMethodAttribute>()
                .FirstOrDefault();
        return apiAttribute != null;
    }
}