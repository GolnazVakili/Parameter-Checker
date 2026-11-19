using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace ParameterQc;

public static class PopulationWorkbook
{
    public static void Save(PopulationReport report, string viewName, string path)
    {
        using var stream = File.Create(path);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        Write(zip, "[Content_Types].xml", ContentTypes());
        Write(zip, "_rels/.rels", PackageRels());
        Write(zip, "xl/workbook.xml", Workbook());
        Write(zip, "xl/_rels/workbook.xml.rels", WorkbookRels());
        Write(zip, "xl/styles.xml", Styles());
        Write(zip, "xl/worksheets/sheet1.xml", PopulationSheet(report, viewName));
        Write(zip, "xl/worksheets/sheet2.xml", ParameterSheet(report));
    }

    private static void Write(ZipArchive zip, string name, string xml)
    {
        ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using Stream stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(xml);
    }

    private static string ContentTypes() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private static string PackageRels() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string Workbook() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets>
            <sheet name="Population" sheetId="1" r:id="rId1"/>
            <sheet name="Parameters" sheetId="2" r:id="rId2"/>
          </sheets>
        </workbook>
        """;

    private static string WorkbookRels() =>
        """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private static string Styles()
    {
        var fills = new StringBuilder();
        fills.Append("<fill><patternFill patternType=\"none\"/></fill>");
        fills.Append("<fill><patternFill patternType=\"gray125\"/></fill>");
        foreach (string group in ParameterGroups.DisplayOrder)
            fills.Append(SolidFill(ParameterGroups.ColorHex(group)));
        fills.Append(SolidFill("E7EDF4"));
        fills.Append(SolidFill("F4F7FB"));

        var cellXfs = new StringBuilder();
        cellXfs.Append(Xf(0, 0, 0, 0));
        cellXfs.Append(Xf(2, 0, 0, 0));
        for (int i = 0; i < ParameterGroups.DisplayOrder.Length; i++)
            cellXfs.Append(Xf(1, i + 2, 1, 0, center: true));
        cellXfs.Append(Xf(3, ParameterGroups.DisplayOrder.Length + 2, 1, 0, center: true));
        cellXfs.Append(Xf(0, 0, 1, 0));
        cellXfs.Append(Xf(0, 0, 1, 164, center: true));
        cellXfs.Append(Xf(3, ParameterGroups.DisplayOrder.Length + 3, 1, 0));
        cellXfs.Append(Xf(3, ParameterGroups.DisplayOrder.Length + 3, 1, 164, center: true));

        int fillCount = 2 + ParameterGroups.DisplayOrder.Length + 2;
        int xfCount = 1 + 1 + ParameterGroups.DisplayOrder.Length + 5;

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <numFmts count="1">
                <numFmt numFmtId="164" formatCode="0.00&quot;%&quot;"/>
              </numFmts>
              <fonts count="4">
                <font><sz val="11"/><name val="Calibri"/></font>
                <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font>
                <font><b/><sz val="14"/><name val="Calibri"/></font>
                <font><b/><sz val="11"/><name val="Calibri"/></font>
              </fonts>
              <fills count="{fillCount}">
                {fills}
              </fills>
              <borders count="2">
                <border><left/><right/><top/><bottom/><diagonal/></border>
                <border>
                  <left style="thin"><color auto="1"/></left>
                  <right style="thin"><color auto="1"/></right>
                  <top style="thin"><color auto="1"/></top>
                  <bottom style="thin"><color auto="1"/></bottom>
                  <diagonal/>
                </border>
              </borders>
              <cellStyleXfs count="1">
                <xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>
              </cellStyleXfs>
              <cellXfs count="{xfCount}">
                {cellXfs}
              </cellXfs>
            </styleSheet>
            """;
    }

    private static string SolidFill(string rgb) =>
        $"<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF{rgb}\"/><bgColor indexed=\"64\"/></patternFill></fill>";

    private static string Xf(int fontId, int fillId, int borderId, int numFmtId, bool center = false)
    {
        string alignment = center ? "<alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\"/>" : "";
        string apply = center ? " applyAlignment=\"1\"" : "";
        return $"<xf numFmtId=\"{numFmtId}\" fontId=\"{fontId}\" fillId=\"{fillId}\" borderId=\"{borderId}\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"{apply}>{alignment}</xf>";
    }

    private static int GroupStyle(string group)
    {
        int index = Array.IndexOf(ParameterGroups.DisplayOrder, group);
        if (index < 0)
            index = ParameterGroups.DisplayOrder.Length - 1;
        return 2 + index;
    }

    private static int SubheaderStyle => 2 + ParameterGroups.DisplayOrder.Length;
    private static int BodyStyle => SubheaderStyle + 1;
    private static int PercentStyle => BodyStyle + 1;
    private static int TotalStyle => PercentStyle + 1;
    private static int TotalPercentStyle => TotalStyle + 1;

    private static string PopulationSheet(PopulationReport report, string viewName)
    {
        int columns = 1 + report.Groups.Count * 3;
        int lastRow = 3 + report.Categories.Count + 1;
        var rows = new StringBuilder();
        var merges = new StringBuilder();

        rows.Append(Row(1,
            TextCell(1, 1, $"Parameter QC — {viewName}", 1)));
        merges.Append($"<mergeCell ref=\"A1:{ColumnName(columns)}1\"/>");

        var groupHeader = new List<string> { TextCell(1, 2, "Model Element Category", SubheaderStyle) };
        var subHeader = new List<string> { TextCell(1, 3, "", SubheaderStyle) };
        for (int i = 0; i < report.Groups.Count; i++)
        {
            int start = 2 + i * 3;
            string group = report.Groups[i];
            groupHeader.Add(TextCell(start, 2, group, GroupStyle(group)));
            subHeader.Add(TextCell(start, 3, "Complete elements", SubheaderStyle));
            subHeader.Add(TextCell(start + 1, 3, "Complete %", SubheaderStyle));
            subHeader.Add(TextCell(start + 2, 3, "Values filled", SubheaderStyle));
            merges.Append($"<mergeCell ref=\"{ColumnName(start)}2:{ColumnName(start + 2)}2\"/>");
        }

        rows.Append(Row(2, groupHeader));
        rows.Append(Row(3, subHeader));

        int rowIndex = 4;
        foreach (CategoryRow category in report.Categories)
        {
            rows.Append(Row(rowIndex, DataCells(category, rowIndex, total: false)));
            rowIndex++;
        }

        rows.Append(Row(rowIndex, DataCells(report.Total, rowIndex, total: true)));

        int mergeCount = 1 + report.Groups.Count;
        return Worksheet(columns, lastRow, rows.ToString(), merges.ToString(), mergeCount, frozenRow: 3);
    }

    private static IEnumerable<string> DataCells(CategoryRow category, int row, bool total)
    {
        int textStyle = total ? TotalStyle : BodyStyle;
        int percentStyle = total ? TotalPercentStyle : PercentStyle;
        yield return TextCell(1, row, category.Category, textStyle);

        int column = 2;
        foreach (string group in category.ByGroup.Keys.OrderBy(OrderOf))
        {
            GroupTally tally = category.ByGroup[group];
            yield return TextCell(column, row, tally.CompleteFraction, textStyle);
            yield return PercentCell(column + 1, row, tally.HasElements, tally.CompletePercent, percentStyle);
            yield return TextCell(column + 2, row, tally.Fraction, textStyle);
            column += 3;
        }
    }

    private static int OrderOf(string group)
    {
        int index = Array.IndexOf(ParameterGroups.DisplayOrder, group);
        return index < 0 ? int.MaxValue : index;
    }

    private static string PercentCell(int column, int row, bool hasValue, double percent, int style)
    {
        if (!hasValue)
            return TextCell(column, row, "—", style);

        string number = percent.ToString("0.00", CultureInfo.InvariantCulture);
        return $"<c r=\"{Cell(column, row)}\" s=\"{style}\"><v>{number}</v></c>";
    }

    private static string ParameterSheet(PopulationReport report)
    {
        var rows = new StringBuilder();
        rows.Append(Row(1, TextCell(1, 1, "Group", SubheaderStyle), TextCell(2, 1, "Parameter", SubheaderStyle)));
        int row = 2;
        foreach (ParameterSelection parameter in report.Parameters.OrderBy(item => OrderOf(item.Group)).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
        {
            rows.Append(Row(row,
                TextCell(1, row, parameter.Group, BodyStyle),
                TextCell(2, row, parameter.Name, BodyStyle)));
            row++;
        }

        int lastRow = Math.Max(1, row - 1);
        return Worksheet(2, lastRow, rows.ToString(), merges: "", mergeCount: 0, frozenRow: 1);
    }

    private static string Worksheet(int columns, int rows, string sheetData, string merges, int mergeCount, int frozenRow)
    {
        string mergeXml = mergeCount == 0
            ? ""
            : $"<mergeCells count=\"{mergeCount}\">{merges}</mergeCells>";
        string topLeft = $"A{frozenRow + 1}";
        var widths = new StringBuilder();
        widths.Append("<col min=\"1\" max=\"1\" width=\"32\" customWidth=\"1\"/>");
        if (columns > 1)
            widths.Append($"<col min=\"2\" max=\"{columns}\" width=\"16\" customWidth=\"1\"/>");

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <dimension ref="A1:{ColumnName(columns)}{rows}"/>
              <sheetViews>
                <sheetView workbookViewId="0">
                  <pane ySplit="{frozenRow}" topLeftCell="{topLeft}" activePane="bottomLeft" state="frozen"/>
                </sheetView>
              </sheetViews>
              <cols>{widths}</cols>
              <sheetData>
                {sheetData}
              </sheetData>
              {mergeXml}
            </worksheet>
            """;
    }

    private static string Row(int index, params string[] cells) => Row(index, (IEnumerable<string>)cells);

    private static string Row(int index, IEnumerable<string> cells) =>
        $"<row r=\"{index}\">{string.Concat(cells)}</row>";

    private static string TextCell(int column, int row, string text, int style) =>
        $"<c r=\"{Cell(column, row)}\" t=\"inlineStr\" s=\"{style}\"><is><t>{Escape(text)}</t></is></c>";

    private static string Cell(int column, int row) =>
        ColumnName(column) + row.ToString(CultureInfo.InvariantCulture);

    private static string ColumnName(int column)
    {
        var chars = new Stack<char>();
        int value = column;
        while (value > 0)
        {
            value--;
            chars.Push((char)('A' + value % 26));
            value /= 26;
        }

        return new string(chars.ToArray());
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
