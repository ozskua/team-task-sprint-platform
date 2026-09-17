using ClosedXML.Excel;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;

namespace SprintFlow.Web.Server.Services;

public static class SprintReportExcelWriter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public const string Extension = ".xlsx";

    private const string DateFormat = "yyyy-mm-dd";

    private const string DateTimeFormat = "yyyy-mm-dd hh:mm";

    private const string PercentFormat = "0%";

    private const int HeaderRow = 4;

    private static readonly string[] SummaryColumns =
    [
        "Sprint", "Board", "Area", "Start", "End", "Status", "Total",
        "Completed", "Completed %", "Cancelled", "Cancelled %", "Ongoing", "Ongoing %"
    ];

    private static readonly string[] TaskColumns =
    [
        "Sprint", "Board", "Area", "Task Id", "Title", "Column", "Outcome", "Assignee", "Created by", "Created"
    ];

    public static byte[] Write(SprintReportExport export)
    {
        using var workbook = new XLWorkbook();

        WriteSummary(workbook.Worksheets.Add("Summary"), export);
        WriteTasks(workbook.Worksheets.Add("Tasks"), export);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    private static void WriteSummary(IXLWorksheet sheet, SprintReportExport export)
    {
        WriteTitle(sheet, export);
        WriteHeader(sheet, SummaryColumns);

        var row = HeaderRow + 1;

        foreach (var sprint in export.Rows)
        {
            sheet.Cell(row, 1).Value = sprint.Name;
            sheet.Cell(row, 2).Value = sprint.BoardName;
            sheet.Cell(row, 3).Value = sprint.AreaName;
            SetDate(sheet.Cell(row, 4), sprint.StartDate);
            SetDate(sheet.Cell(row, 5), sprint.EndDate);
            sheet.Cell(row, 6).Value = sprint.Status;
            sheet.Cell(row, 7).Value = sprint.TotalTasks;
            sheet.Cell(row, 8).Value = sprint.CompletedCount;
            SetPercent(sheet.Cell(row, 9), sprint.CompletedPercent);
            sheet.Cell(row, 10).Value = sprint.CancelledCount;
            SetPercent(sheet.Cell(row, 11), sprint.CancelledPercent);
            sheet.Cell(row, 12).Value = sprint.OngoingCount;
            SetPercent(sheet.Cell(row, 13), sprint.OngoingPercent);

            row++;
        }

        sheet.Cell(row, 1).Value = "All sprints";
        sheet.Cell(row, 7).Value = export.TotalTasks;
        sheet.Cell(row, 8).Value = export.CompletedTasks;
        sheet.Cell(row, 10).Value = export.CancelledTasks;
        sheet.Cell(row, 12).Value = export.OngoingTasks;
        sheet.Row(row).Style.Font.Bold = true;

        Finish(sheet, SummaryColumns.Length, row - 1);
    }

    private static void WriteTasks(IXLWorksheet sheet, SprintReportExport export)
    {
        WriteTitle(sheet, export);
        WriteHeader(sheet, TaskColumns);

        var row = HeaderRow + 1;

        foreach (var sprint in export.Rows)
        {
            foreach (var task in export.TasksOf(sprint.SprintId))
            {
                sheet.Cell(row, 1).Value = sprint.Name;
                sheet.Cell(row, 2).Value = sprint.BoardName;
                sheet.Cell(row, 3).Value = sprint.AreaName;
                sheet.Cell(row, 4).Value = task.TaskId;
                sheet.Cell(row, 5).Value = task.Title;
                sheet.Cell(row, 6).Value = task.ColumnName;
                sheet.Cell(row, 7).Value = SprintReportExport.OutcomeOf(task);
                sheet.Cell(row, 8).Value = task.AssigneeFullName ?? string.Empty;
                sheet.Cell(row, 9).Value = task.CreatedByFullName;
                SetDateTime(sheet.Cell(row, 10), task.CreatedDate);

                row++;
            }
        }

        Finish(sheet, TaskColumns.Length, row - 1);
    }

    private static void WriteTitle(IXLWorksheet sheet, SprintReportExport export)
    {
        sheet.Cell(1, 1).Value = "Sprint Report";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        sheet.Cell(2, 1).Value = $"Generated {export.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC";
    }

    private static void WriteHeader(IXLWorksheet sheet, string[] columns)
    {
        for (var column = 0; column < columns.Length; column++)
        {
            sheet.Cell(HeaderRow, column + 1).Value = columns[column];
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;
    }

    private static void Finish(IXLWorksheet sheet, int columnCount, int lastDataRow)
    {
        sheet.SheetView.FreezeRows(HeaderRow);

        if (lastDataRow >= HeaderRow)
        {
            sheet.Range(HeaderRow, 1, Math.Max(lastDataRow, HeaderRow), columnCount).SetAutoFilter();
        }

        sheet.Columns(1, columnCount).AdjustToContents();
    }

    private static void SetDate(IXLCell cell, DateOnly date)
    {
        cell.Value = date.ToDateTime(TimeOnly.MinValue);
        cell.Style.DateFormat.Format = DateFormat;
    }

    private static void SetDateTime(IXLCell cell, DateTime value)
    {
        cell.Value = value;
        cell.Style.DateFormat.Format = DateTimeFormat;
    }

    private static void SetPercent(IXLCell cell, int percent)
    {
        cell.Value = percent / 100.0;
        cell.Style.NumberFormat.Format = PercentFormat;
    }
}
