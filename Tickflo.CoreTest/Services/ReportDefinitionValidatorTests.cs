namespace Tickflo.CoreTest.Services;

using Xunit;

public class ReportDefinitionValidatorTests
{
    [Fact]
    public void ParseEmptyJsonReturnsDefaults()
    {
        var validator = new ReportDefinitionValidator();

        var def = validator.Parse(null);

        Assert.Equal("tickets", def.Source);
        Assert.Equal(["Id", "Subject", "Status", "CreatedAt"], def.Fields);
        Assert.Null(def.FiltersJson);
    }

    [Fact]
    public void ParseValidJsonExtractsSourceFieldsFilters()
    {
        var validator = new ReportDefinitionValidator();
        var json = /*lang=json,strict*/ """{"source":"contacts","fields":["Id","Name"],"filters":[]}""";

        var def = validator.Parse(json);

        Assert.Equal("contacts", def.Source);
        Assert.Equal(["Id", "Name"], def.Fields);
        Assert.NotNull(def.FiltersJson);
    }

    [Fact]
    public void BuildJsonConstructsValidJson()
    {
        var validator = new ReportDefinitionValidator();

        var json = validator.BuildJson("tickets", "Id,Subject,Status", null);

        Assert.Contains("\"source\":\"tickets\"", json);
        Assert.Contains("\"Id\"", json);
        Assert.Contains("\"Subject\"", json);
        Assert.Contains("\"Status\"", json);
    }

    [Fact]
    public void GetAvailableSourcesReturnsAllSources()
    {
        var validator = new ReportDefinitionValidator();

        var sources = validator.GetAvailableSources();

        Assert.Contains("tickets", sources.Keys);
        Assert.Contains("contacts", sources.Keys);
        Assert.Contains("locations", sources.Keys);
        Assert.Contains("inventory", sources.Keys);
    }
}

