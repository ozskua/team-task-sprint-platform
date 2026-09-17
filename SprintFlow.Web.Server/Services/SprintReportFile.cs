using SprintFlow.Web.Server.Common.Enums;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;

namespace SprintFlow.Web.Server.Services;

public sealed record SprintReportFile(byte[] Bytes, string ContentType, string FileName)
{
    public static SprintReportFile Render(SprintReportExport export, SprintReportFormat format, DateOnly date, int? areaId = null)
    {
        var suffix = areaId is int id ? $"_{id}" : string.Empty;
        var baseName = $"SprintReport_{date:yyyy-MM-dd}{suffix}";

        return format switch
        {
            SprintReportFormat.Xlsx => new SprintReportFile(
                SprintReportExcelWriter.Write(export),
                SprintReportExcelWriter.ContentType,
                baseName + SprintReportExcelWriter.Extension),
            SprintReportFormat.Pdf => new SprintReportFile(
                SprintReportPdfWriter.Write(export),
                SprintReportPdfWriter.ContentType,
                baseName + SprintReportPdfWriter.Extension),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }
}
