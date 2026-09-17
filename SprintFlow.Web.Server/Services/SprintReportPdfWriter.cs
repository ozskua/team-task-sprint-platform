using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SprintFlow.Web.Server.DTOs.AdminSprintReport;
using SprintFlow.Web.Server.DTOs.Sprints;

namespace SprintFlow.Web.Server.Services;

public static class SprintReportPdfWriter
{
    public const string ContentType = "application/pdf";

    public const string Extension = ".pdf";

    private const string DateFormat = "yyyy-MM-dd";

    private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

    private static readonly string[] OutcomeOrder = ["Completed", "Cancelled", "Ongoing"];

    static SprintReportPdfWriter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Write(SprintReportExport export)
        => Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontSize(9));

            page.Header().Column(column =>
            {
                column.Item().Text("SprintFlow — Sprint Report").FontSize(18).Bold();
                column.Item().Text($"Generated {export.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC")
                    .FontColor(Colors.Grey.Darken1);
                column.Item().PaddingBottom(12);
            });

            page.Content().Column(column =>
            {
                column.Spacing(14);

                column.Item().Element(container => WriteTotals(container, export));
                column.Item().Element(container => WriteSummary(container, export));

                foreach (var sprint in export.Rows)
                {
                    column.Item().Element(container => WriteSprint(container, sprint, export.TasksOf(sprint.SprintId)));
                }
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();

    private static void WriteTotals(IContainer container, SprintReportExport export)
        => container.Row(row =>
        {
            row.Spacing(8);

            Metric(row, "Sprints", export.Rows.Count);
            Metric(row, "Tasks", export.TotalTasks);
            Metric(row, "Completed", export.CompletedTasks);
            Metric(row, "Cancelled", export.CancelledTasks);
            Metric(row, "Ongoing", export.OngoingTasks);
        });

    private static void Metric(RowDescriptor row, string label, int value)
        => row.RelativeItem()
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(8)
            .Column(column =>
            {
                column.Item().Text(label).FontColor(Colors.Grey.Darken1);
                column.Item().Text(value.ToString()).FontSize(16).Bold();
            });

    private static void WriteSummary(IContainer container, SprintReportExport export)
        => container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Sprints").FontSize(12).Bold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(56);
                    columns.ConstantColumn(56);
                    columns.ConstantColumn(46);
                    columns.ConstantColumn(30);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(52);
                });

                table.Header(header =>
                {
                    foreach (var title in new[] { "Sprint", "Board", "Area", "Start", "End", "Status", "Total", "Completed", "Cancelled", "Ongoing" })
                    {
                        HeaderCell(header.Cell(), title);
                    }
                });

                foreach (var sprint in export.Rows)
                {
                    BodyCell(table.Cell(), sprint.Name);
                    BodyCell(table.Cell(), sprint.BoardName);
                    BodyCell(table.Cell(), sprint.AreaName);
                    BodyCell(table.Cell(), sprint.StartDate.ToString(DateFormat));
                    BodyCell(table.Cell(), sprint.EndDate.ToString(DateFormat));
                    BodyCell(table.Cell(), sprint.Status);
                    BodyCell(table.Cell(), sprint.TotalTasks.ToString());
                    BodyCell(table.Cell(), $"{sprint.CompletedCount} ({sprint.CompletedPercent}%)");
                    BodyCell(table.Cell(), $"{sprint.CancelledCount} ({sprint.CancelledPercent}%)");
                    BodyCell(table.Cell(), $"{sprint.OngoingCount} ({sprint.OngoingPercent}%)");
                }
            });
        });

    private static void WriteSprint(
        IContainer container,
        SprintReportRowDto sprint,
        IReadOnlyList<SprintTaskResponseDto> tasks)
        => container.Column(column =>
        {
            column.Spacing(6);

            column.Item().Text(sprint.Name).FontSize(12).Bold();
            column.Item().Text(
                    $"{sprint.BoardName} / {sprint.AreaName} · {sprint.StartDate.ToString(DateFormat)} → {sprint.EndDate.ToString(DateFormat)} · {sprint.Status}")
                .FontColor(Colors.Grey.Darken1);

            if (tasks.Count == 0)
            {
                column.Item().Text("No tasks").Italic().FontColor(Colors.Grey.Darken1);

                return;
            }

            foreach (var outcome in OutcomeOrder)
            {
                var group = tasks.Where(task => SprintReportExport.OutcomeOf(task) == outcome).ToList();

                if (group.Count == 0)
                {
                    continue;
                }

                column.Item().PaddingTop(4).Text($"{outcome} · {group.Count}").Bold();
                column.Item().Element(item => WriteTaskTable(item, group));
            }
        });

    private static void WriteTaskTable(IContainer container, IReadOnlyList<SprintTaskResponseDto> tasks)
        => container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(44);
                columns.RelativeColumn(4);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(84);
            });

            table.Header(header =>
            {
                foreach (var title in new[] { "Id", "Title", "Column", "Assignee", "Created by", "Created" })
                {
                    HeaderCell(header.Cell(), title);
                }
            });

            foreach (var task in tasks)
            {
                BodyCell(table.Cell(), task.TaskId.ToString());
                BodyCell(table.Cell(), task.Title);
                BodyCell(table.Cell(), task.ColumnName);
                BodyCell(table.Cell(), task.AssigneeFullName ?? "—");
                BodyCell(table.Cell(), task.CreatedByFullName);
                BodyCell(table.Cell(), task.CreatedDate.ToString(DateTimeFormat));
            }
        });

    private static void HeaderCell(IContainer cell, string text)
        => cell.Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(3)
            .Text(text)
            .FontSize(8)
            .Bold();

    private static void BodyCell(IContainer cell, string text)
        => cell.BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .Padding(3)
            .Text(text)
            .FontSize(8);
}
