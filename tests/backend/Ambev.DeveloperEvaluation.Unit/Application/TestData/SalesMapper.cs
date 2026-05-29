using Ambev.DeveloperEvaluation.Application.Sales.Common;
using AutoMapper;

namespace Ambev.DeveloperEvaluation.Unit.Application.TestData;

/// <summary>Builds a real AutoMapper instance with the sales profiles for handler tests.</summary>
public static class SalesMapper
{
    public static IMapper Create()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<SaleResultProfile>());
        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }
}
