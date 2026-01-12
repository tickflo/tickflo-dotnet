using Tickflo.Core.Services;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class ReportDefinitionValidatorTests
{
    [Fact]
    public void Parse_EmptyJson_ReturnsDefaults()
    {
        var validator = new ReportDefinitionValidator();
        
        var def = validator.Parse(null);
        
        Assert.Equal("tickets", def.Source);
        Assert.Equal(new[] { "Id", "Subject", "Status", "CreatedAt" }, def.Fields);
        Assert.Null(def.FiltersJson);
    }

    [Fact]
    public void Parse_ValidJson_ExtractsSourceFieldsFilters()
    {
        var validator = new ReportDefinitionValidator();
        var json = """{"source":"contacts","fields":["Id","Name"],"filters":[]}""";
        
        var def = validator.Parse(json);
        
        Assert.Equal("contacts", def.Source);
        Assert.Equal(new[] { "Id", "Name" }, def.Fields);
        Assert.NotNull(def.FiltersJson);
    }

    [Fact]
    public void BuildJson_ConstructsValidJson()
    {
        var validator = new ReportDefinitionValidator();
        
        var json = validator.BuildJson("tickets", "Id,Subject,Status", null);
        
        Assert.Contains("\"source\":\"tickets\"", json);
        Assert.Contains("\"Id\"", json);
        Assert.Contains("\"Subject\"", json);
        Assert.Contains("\"Status\"", json);
    }

    [Fact]
    public void GetAvailableSources_ReturnsAllSources()
    {
        var validator = new ReportDefinitionValidator();
        
        var sources = validator.GetAvailableSources();
        
        Assert.Contains("tickets", sources.Keys);
        Assert.Contains("contacts", sources.Keys);
        Assert.Contains("locations", sources.Keys);
        Assert.Contains("inventory", sources.Keys);
    }
}
