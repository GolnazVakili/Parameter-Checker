using System.IO.Compression;
using System.Xml.Linq;

namespace ParameterQc.Core.Tests;

public class ValueRuleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("0.0")]
    [InlineData("0.00")]
    public void BlankAndZeroAreEmpty(string? text)
    {
        Assert.Equal(SlotKind.Empty, ValueRules.ClassifyText(text));
    }

    [Theory]
    [InlineData("n/a")]
    [InlineData("N/A")]
    [InlineData("na")]
    [InlineData("NA")]
    [InlineData("n.a.")]
    public void NaIsLeftOut(string text)
    {
        Assert.Equal(SlotKind.Excluded, ValueRules.ClassifyText(text));
    }

    [Theory]
    [InlineData("TBD")]
    [InlineData("tbd")]
    [InlineData("T.B.D.")]
    [InlineData("to be determined")]
    public void TbdIsItsOwnState(string text)
    {
        Assert.Equal(SlotKind.Tbd, ValueRules.ClassifyText(text));
    }

    [Theory]
    [InlineData("Yes")]
    [InlineData("No")]
    [InlineData("Door-01")]
    public void TextValuesCountAsFilled(string text)
    {
        Assert.Equal(SlotKind.Filled, ValueRules.ClassifyText(text));
    }

    [Fact]
    public void YesNoIntegerZeroCountsAsFilled()
    {
        Assert.Equal(SlotKind.Filled, ValueRules.ClassifyInteger(0, yesNo: true));
        Assert.Equal(SlotKind.Empty, ValueRules.ClassifyInteger(0, yesNo: false));
        Assert.Equal(SlotKind.Filled, ValueRules.ClassifyInteger(4, yesNo: false));
    }

    [Fact]
    public void NumericZeroCountsAsEmpty()
    {
        Assert.Equal(SlotKind.Empty, ValueRules.ClassifyDouble(0, yesNo: false));
        Assert.Equal(SlotKind.Filled, ValueRules.ClassifyDouble(1.5, yesNo: false));
    }

    [Fact]
    public void RegistrableYesIsCaseInsensitive()
    {
        Assert.True(ValueRules.IsYes(" YES "));
        Assert.False(ValueRules.IsYes("NO"));
        Assert.False(ValueRules.IsYes(""));
        Assert.False(ValueRules.IsYes(null));
    }
}

public class GroupCatalogTests
{
    [Theory]
    [InlineData("Mark", ParameterGroups.Pact)]
    [InlineData("mark", ParameterGroups.Pact)]
    [InlineData("Discipline", ParameterGroups.Pact)]
    [InlineData("IfcExportAs", ParameterGroups.Ifc)]
    [InlineData("Asset ID", ParameterGroups.Pact)]
    [InlineData("Area (m²)", ParameterGroups.Pact)]
    [InlineData("Maximo.Class", ParameterGroups.Maximo)]
    [InlineData("Maximo.Registrable", ParameterGroups.Maximo)]
    [InlineData("Classification.Uniformat", ParameterGroups.Classification)]
    [InlineData("COBie.Type.Name", ParameterGroups.Cobie)]
    [InlineData("Comments", ParameterGroups.Other)]
    public void NamesFromTheSheetStayInTheirGroup(string name, string group)
    {
        Assert.Equal(group, KnownParameters.GroupFor(name));
    }
}

public class ReportBuilderTests
{
    private static readonly ParameterSelection[] IfcParameters =
    [
        new("Mark", ParameterGroups.Ifc),
        new("Level", ParameterGroups.Ifc),
        new("Location", ParameterGroups.Ifc),
        new("Discipline", ParameterGroups.Ifc)
    ];

    [Fact]
    public void SelectedParametersInOneGroupShareOnePercentage()
    {
        var elements = new List<ElementRecord>();
        for (int i = 0; i < 10; i++)
        {
            var slots = new Dictionary<string, SlotKind>(StringComparer.OrdinalIgnoreCase)
            {
                ["Mark"] = SlotKind.Filled,
                ["Level"] = SlotKind.Filled,
                ["Location"] = SlotKind.Filled,
                ["Discipline"] = i < 5 ? SlotKind.Tbd : SlotKind.Empty
            };
            elements.Add(new ElementRecord("Walls", false, slots));
        }

        PopulationReport report = ReportBuilder.Build(elements, IfcParameters);
        GroupTally tally = report.Categories.Single().ByGroup[ParameterGroups.Ifc];

        Assert.Equal(30, tally.Filled);
        Assert.Equal(5, tally.Tbd);
        Assert.Equal(5, tally.Empty);
        Assert.Equal("30/40", tally.Fraction);
        Assert.Equal(0, tally.CompleteElements);
        Assert.Equal(10, tally.Elements);
        Assert.Equal(75, tally.FilledPercent);
        Assert.Equal(12.5, tally.TbdPercent);
        Assert.Equal([ParameterGroups.Ifc], report.Groups);
    }

    [Fact]
    public void NaIsRemovedFromTheDenominatorAndAMissingParameterIsEmpty()
    {
        var slots = new Dictionary<string, SlotKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mark"] = SlotKind.Excluded
        };
        var elements = new[] { new ElementRecord("Doors", false, slots) };
        var selected = new ParameterSelection[]
        {
            new("Mark", ParameterGroups.Ifc),
            new("Level", ParameterGroups.Ifc)
        };

        GroupTally tally = ReportBuilder.Build(elements, selected).Categories.Single().ByGroup[ParameterGroups.Ifc];

        Assert.Equal(0, tally.Filled);
        Assert.Equal(1, tally.Empty);
        Assert.Equal(1, tally.Excluded);
        Assert.Equal("0/1", tally.Fraction);
    }

    [Fact]
    public void CompleteCountIsElementsWhoseWholeGroupIsFilled()
    {
        var selected = new ParameterSelection[]
        {
            new("Mark", ParameterGroups.Ifc),
            new("Level", ParameterGroups.Ifc)
        };
        var elements = new[]
        {
            new ElementRecord("Air Terminals", false, new Dictionary<string, SlotKind>
            {
                ["Mark"] = SlotKind.Filled,
                ["Level"] = SlotKind.Filled
            }),
            new ElementRecord("Air Terminals", false, new Dictionary<string, SlotKind>
            {
                ["Mark"] = SlotKind.Filled,
                ["Level"] = SlotKind.Empty
            })
        };

        GroupTally tally = ReportBuilder.Build(elements, selected).Categories.Single().ByGroup[ParameterGroups.Ifc];

        Assert.Equal(1, tally.CompleteElements);
        Assert.Equal(2, tally.Elements);
        Assert.Equal("1/2", tally.CompleteFraction);
        Assert.Equal(50, tally.CompletePercent);
        Assert.Equal("3/4", tally.Fraction);
    }

    [Fact]
    public void MaximoSkipsElementsThatAreNotRegistrableYes()
    {
        var selected = new ParameterSelection[] { new("Maximo.Class", ParameterGroups.Maximo) };
        var elements = new[]
        {
            new ElementRecord("Doors", true, new Dictionary<string, SlotKind> { ["Maximo.Class"] = SlotKind.Filled }),
            new ElementRecord("Doors", false, new Dictionary<string, SlotKind> { ["Maximo.Class"] = SlotKind.Empty })
        };

        GroupTally maximo = ReportBuilder.Build(elements, selected).Categories.Single().ByGroup[ParameterGroups.Maximo];

        Assert.Equal("1/1", maximo.Fraction);
        Assert.Equal(100, maximo.FilledPercent);
    }

    [Fact]
    public void MaximoFilterDoesNotChangeOtherGroups()
    {
        var selected = new ParameterSelection[]
        {
            new("Mark", ParameterGroups.Ifc),
            new("Maximo.Class", ParameterGroups.Maximo)
        };
        var elements = new[]
        {
            new ElementRecord("Pipes", false, new Dictionary<string, SlotKind>
            {
                ["Mark"] = SlotKind.Filled,
                ["Maximo.Class"] = SlotKind.Filled
            })
        };

        PopulationReport report = ReportBuilder.Build(elements, selected);

        Assert.Equal("1/1", report.Categories.Single().ByGroup[ParameterGroups.Ifc].Fraction);
        Assert.Equal("—", report.Categories.Single().ByGroup[ParameterGroups.Maximo].Fraction);
    }

    [Fact]
    public void TotalAddsTheCountsInsteadOfAveragingPercents()
    {
        var selected = new ParameterSelection[] { new("Mark", ParameterGroups.Ifc) };
        var elements = new[]
        {
            new ElementRecord("Doors", false, new Dictionary<string, SlotKind> { ["Mark"] = SlotKind.Filled }),
            new ElementRecord("Doors", false, new Dictionary<string, SlotKind> { ["Mark"] = SlotKind.Empty }),
            new ElementRecord("Walls", false, new Dictionary<string, SlotKind> { ["Mark"] = SlotKind.Filled }),
            new ElementRecord("Walls", false, new Dictionary<string, SlotKind> { ["Mark"] = SlotKind.Filled })
        };

        PopulationReport report = ReportBuilder.Build(elements, selected);

        Assert.Equal(["Doors", "Walls"], report.Categories.Select(row => row.Category));
        Assert.Equal("1/2", report.Categories[0].ByGroup[ParameterGroups.Ifc].Fraction);
        Assert.Equal("2/2", report.Categories[1].ByGroup[ParameterGroups.Ifc].Fraction);
        Assert.Equal("3/4", report.Total.ByGroup[ParameterGroups.Ifc].Fraction);
        Assert.Equal(75, report.Total.ByGroup[ParameterGroups.Ifc].FilledPercent);
    }
}

public class WorkbookTests
{
    [Fact]
    public void WorkbookStoresTheSameFractionTheChartShows()
    {
        var selected = new ParameterSelection[] { new("Mark", ParameterGroups.Ifc) };
        var elements = Enumerable.Range(0, 32)
            .Select(index => new ElementRecord(
                "Ducts",
                false,
                new Dictionary<string, SlotKind> { ["Mark"] = index < 14 ? SlotKind.Filled : SlotKind.Empty }))
            .ToArray();
        PopulationReport report = ReportBuilder.Build(elements, selected);

        string path = Path.Combine(Path.GetTempPath(), $"parameter-qc-{Guid.NewGuid():N}.xlsx");
        try
        {
            PopulationWorkbook.Save(report, "Level 1", path);
            using var zip = ZipFile.OpenRead(path);
            string population = Read(zip, "xl/worksheets/sheet1.xml");
            string parameters = Read(zip, "xl/worksheets/sheet2.xml");
            XDocument.Parse(population);
            XDocument.Parse(parameters);

            Assert.Contains("14/32", population, StringComparison.Ordinal);
            Assert.Contains("Ducts", population, StringComparison.Ordinal);
            Assert.Contains("GTAA_IFC", population, StringComparison.Ordinal);
            Assert.Contains("Level 1", population, StringComparison.Ordinal);
            Assert.Contains("Mark", parameters, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static string Read(ZipArchive zip, string name)
    {
        ZipArchiveEntry? entry = zip.GetEntry(name);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
