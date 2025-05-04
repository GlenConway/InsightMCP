using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsightMCP.Models;

[JsonConverter(typeof(ReportJsonConverter))]
public class Report
{
    public required string CaseNumber { get; set; }
    public required string ReportLOINCCode { get; set; }
    public required string ReportLOINCName { get; set; }
    public required string ProtocolName { get; set; }
    public required string ReportText { get; set; }
    public DateTime? Date { get; set; }
    public string? ProcedureType { get; set; }
    public string? TumorType { get; set; }
    public double? TumorSize { get; set; }
    public string? MarginStatus { get; set; }
    public string? ClinicalStage { get; set; }
    public string? PathologicalStage { get; set; }
    public string? StagingSystem { get; set; }
}

public class ReportJsonConverter : JsonConverter<Report>
{
    public override Report Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var report = new Report
        {
            CaseNumber = "",
            ReportLOINCCode = "",
            ReportLOINCName = "",
            ProtocolName = "",
            ReportText = ""
        };

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return report;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLower())
            {
                case "casenumber":
                    report.CaseNumber = reader.GetString() ?? "";
                    break;
                case "reportloinccode":
                    report.ReportLOINCCode = reader.GetString() ?? "";
                    break;
                case "reportloincname":
                    report.ReportLOINCName = reader.GetString() ?? "";
                    break;
                case "protocolname":
                    report.ProtocolName = reader.GetString() ?? "";
                    break;
                case "reporttext":
                    report.ReportText = reader.GetString() ?? "";
                    break;
                case "date":
                    if (reader.TokenType != JsonTokenType.Null)
                    {
                        report.Date = reader.GetDateTime();
                    }
                    break;
                case "proceduretype":
                    report.ProcedureType = reader.GetString();
                    break;
                case "tumortype":
                    report.TumorType = reader.GetString();
                    break;
                case "tumorsize":
                    if (reader.TokenType != JsonTokenType.Null)
                    {
                        report.TumorSize = reader.GetDouble();
                    }
                    break;
                case "marginstatus":
                    report.MarginStatus = reader.GetString();
                    break;
                case "clinicalstage":
                    report.ClinicalStage = reader.GetString();
                    break;
                case "pathologicalstage":
                    report.PathologicalStage = reader.GetString();
                    break;
                case "stagingsystem":
                    report.StagingSystem = reader.GetString();
                    break;
            }
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Report value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("caseNumber", value.CaseNumber);
        writer.WriteString("reportLOINCCode", value.ReportLOINCCode);
        writer.WriteString("reportLOINCName", value.ReportLOINCName);
        writer.WriteString("protocolName", value.ProtocolName);
        writer.WriteString("reportText", value.ReportText);
        if (value.Date.HasValue)
        {
            writer.WriteString("date", value.Date.Value);
        }
        if (value.ProcedureType != null)
        {
            writer.WriteString("procedureType", value.ProcedureType);
        }
        if (value.TumorType != null)
        {
            writer.WriteString("tumorType", value.TumorType);
        }
        if (value.TumorSize.HasValue)
        {
            writer.WriteNumber("tumorSize", value.TumorSize.Value);
        }
        if (value.MarginStatus != null)
        {
            writer.WriteString("marginStatus", value.MarginStatus);
        }
        if (value.ClinicalStage != null)
        {
            writer.WriteString("clinicalStage", value.ClinicalStage);
        }
        if (value.PathologicalStage != null)
        {
            writer.WriteString("pathologicalStage", value.PathologicalStage);
        }
        if (value.StagingSystem != null)
        {
            writer.WriteString("stagingSystem", value.StagingSystem);
        }
        writer.WriteEndObject();
    }
}
