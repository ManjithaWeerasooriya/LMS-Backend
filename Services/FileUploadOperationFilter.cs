using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParams = context.MethodInfo
            .GetParameters()
            .Where(p =>
                p.ParameterType == typeof(IFormFile) ||
                p.GetCustomAttributes(typeof(FromFormAttribute), inherit: false).Any())
            .ToList();

        if (!formParams.Any(p => p.ParameterType == typeof(IFormFile)))
            return;

        var properties = formParams.ToDictionary(
            p => p.GetCustomAttributes(typeof(FromFormAttribute), inherit: false)
                    .Cast<FromFormAttribute>()
                    .FirstOrDefault()?.Name ?? p.Name!,
            p => BuildSchemaForType(p.ParameterType));

        var required = formParams
            .Where(p => p.ParameterType == typeof(IFormFile) || !IsNullableParameter(p))
            .Select(p => p.GetCustomAttributes(typeof(FromFormAttribute), inherit: false)
                .Cast<FromFormAttribute>()
                .FirstOrDefault()?.Name ?? p.Name!)
            .ToHashSet();

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = required
                    }
                }
            }
        };
    }

    private static OpenApiSchema BuildSchemaForType(Type parameterType)
    {
        var type = Nullable.GetUnderlyingType(parameterType) ?? parameterType;

        if (type == typeof(IFormFile))
        {
            return new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            };
        }

        if (type == typeof(Guid))
        {
            return new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            };
        }

        if (type == typeof(string))
        {
            return new OpenApiSchema
            {
                Type = "string"
            };
        }

        return new OpenApiSchema
        {
            Type = "string"
        };
    }

    private static bool IsNullableParameter(System.Reflection.ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsValueType)
        {
            return Nullable.GetUnderlyingType(parameter.ParameterType) != null ||
                   new System.Reflection.NullabilityInfoContext().Create(parameter).WriteState ==
                   System.Reflection.NullabilityState.Nullable;
        }

        return Nullable.GetUnderlyingType(parameter.ParameterType) != null;
    }
}
