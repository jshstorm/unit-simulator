using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace UnitSimulator.GoogleSheets;

/// <summary>
/// 스프레드시트 데이터를 XML 형식으로 변환하는 클래스
/// </summary>
public static partial class XmlConverter
{
    /// <summary>
    /// SheetData를 XML 문자열로 변환합니다.
    /// </summary>
    /// <param name="sheetData">변환할 시트 데이터</param>
    /// <returns>XML 형식의 문자열</returns>
    public static string ConvertToXml(SheetData sheetData)
    {
        ArgumentNullException.ThrowIfNull(sheetData);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriterWithEncoding(Encoding.UTF8);
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            WriteSheetToXml(xmlWriter, sheetData);
        }

        return stringWriter.ToString();
    }

    /// <summary>
    /// SheetData를 XML 파일로 저장합니다.
    /// </summary>
    /// <param name="sheetData">저장할 시트 데이터</param>
    /// <param name="outputDirectory">출력 디렉토리</param>
    /// <returns>저장된 파일 경로</returns>
    public static string SaveToXmlFile(SheetData sheetData, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(sheetData);

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        // 출력 디렉토리가 없으면 생성
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var safeFileName = GetSafeFileName(sheetData.SheetName);
        var filePath = Path.Combine(outputDirectory, $"{safeFileName}.xml");

        var xmlContent = ConvertToXml(sheetData);
        File.WriteAllText(filePath, xmlContent, Encoding.UTF8);

        return filePath;
    }

    /// <summary>
    /// 여러 시트 데이터를 각각 XML 파일로 저장합니다.
    /// </summary>
    /// <param name="sheetsData">저장할 시트 데이터 목록</param>
    /// <param name="outputDirectory">출력 디렉토리</param>
    /// <returns>저장된 파일 경로 목록</returns>
    public static IList<string> SaveAllToXmlFiles(IList<SheetData> sheetsData, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(sheetsData);

        var savedPaths = new List<string>();

        foreach (var sheetData in sheetsData)
        {
            var path = SaveToXmlFile(sheetData, outputDirectory);
            savedPaths.Add(path);
        }

        return savedPaths;
    }

    private static void WriteSheetToXml(XmlWriter writer, SheetData sheetData)
    {
        var rootElement = GetValidXmlElementName(sheetData.SheetName);
        writer.WriteStartDocument();
        writer.WriteStartElement(rootElement);

        var headers = sheetData.Headers.Select(h => h?.ToString() ?? "Column").ToList();

        // 각 데이터 행 처리
        var rowIndex = 0;
        foreach (var row in sheetData.DataRows)
        {
            writer.WriteStartElement("Row");
            writer.WriteAttributeString("index", rowIndex.ToString());

            var maxColumns = Math.Max(headers.Count, row.Count);
            for (var colIndex = 0; colIndex < maxColumns; colIndex++)
            {
                var headerName = colIndex < headers.Count ? headers[colIndex] : $"Column{colIndex + 1}";
                var elementName = GetValidXmlElementName(headerName);
                var value = colIndex < row.Count ? row[colIndex]?.ToString() ?? "" : "";

                writer.WriteElementString(elementName, value);
            }

            writer.WriteEndElement(); // Row
            rowIndex++;
        }

        writer.WriteEndElement(); // Root element
        writer.WriteEndDocument();
    }

    /// <summary>
    /// 문자열을 유효한 XML 요소 이름으로 변환합니다.
    /// </summary>
    private static string GetValidXmlElementName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Element";

        // 공백을 언더스코어로 대체
        var result = name.Trim().Replace(' ', '_');

        // XML 요소 이름으로 허용되지 않는 문자 제거
        result = InvalidXmlCharsRegex().Replace(result, "");

        // 숫자나 특수문자로 시작하면 앞에 "_" 추가
        if (result.Length == 0 || char.IsDigit(result[0]) || result[0] == '-' || result[0] == '.')
        {
            result = "_" + result;
        }

        // 'xml'로 시작하는 경우 (대소문자 구분 없음) 앞에 "_" 추가
        if (result.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
        {
            result = "_" + result;
        }

        return string.IsNullOrEmpty(result) ? "Element" : result;
    }

    /// <summary>
    /// 파일 이름에 안전한 문자열로 변환합니다.
    /// </summary>
    private static string GetSafeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "sheet";

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        return string.IsNullOrEmpty(safeName) ? "sheet" : safeName;
    }

    [GeneratedRegex(@"[^\w\-_.]")]
    private static partial Regex InvalidXmlCharsRegex();
}

/// <summary>
/// 특정 인코딩을 사용하는 StringWriter
/// </summary>
internal sealed class StringWriterWithEncoding : StringWriter
{
    public override Encoding Encoding { get; }

    public StringWriterWithEncoding(Encoding encoding)
    {
        Encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
    }
}
