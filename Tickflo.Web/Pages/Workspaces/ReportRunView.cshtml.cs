using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;

namespace Tickflo.Web.Pages.Workspaces;

public class ReportRunViewModel : PageModel
{
    private readonly IWorkspaceRepository _workspaceRepo;
    private readonly IReportRepository _reportRepo;
    private readonly IReportRunRepository _reportRunRepo;
    private readonly IWebHostEnvironment _env;

    public ReportRunViewModel(IWorkspaceRepository workspaceRepo, IReportRepository reportRepo, IReportRunRepository reportRunRepo, IWebHostEnvironment env)
    {
        _workspaceRepo = workspaceRepo;
        _reportRepo = reportRepo;
        _reportRunRepo = reportRunRepo;
        _env = env;
    }

    [BindProperty(SupportsGet = true)]
    public string WorkspaceSlug { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int RunId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Take { get; set; } = 500; // default display limit
    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1; // 1-based page index, hides PageModel.Page()

    public Core.Entities.Workspace? Workspace { get; set; }
    public Report? Report { get; set; }
    public ReportRun? Run { get; set; }

    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();

    public int DisplayLimit { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages { get; set; }
    public int FromRow { get; set; }
    public int ToRow { get; set; }
    public bool HasContent { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, int reportId, int runId, int? take, int? page)
    {
        WorkspaceSlug = slug;
        ReportId = reportId;
        RunId = runId;
        if (take.HasValue && take.Value > 0) Take = Math.Min(take.Value, 5000);
        if (page.HasValue && page.Value > 0) Page = page.Value;
        DisplayLimit = Take;

        var ws = await _workspaceRepo.FindBySlugAsync(slug);
        if (ws == null) return NotFound();
        Workspace = ws;
        var rep = await _reportRepo.FindAsync(ws.Id, reportId);
        if (rep == null) return NotFound();
        Report = rep;
        var run = await _reportRunRepo.FindAsync(ws.Id, runId);
        if (run == null) return NotFound();
        Run = run;
        TotalRows = run.RowCount;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRows / Math.Max(1, Take)));
        if (Page > TotalPages) Page = TotalPages;
        if (Page < 1) Page = 1;

        HasContent = run.FileBytes != null && run.FileBytes.Length > 0;
        if (!HasContent)
        {
            // No content stored for this run (likely pre-DB storage). Render page with info message.
            FromRow = 0;
            ToRow = 0;
            return Page();
        }

        if (run.FileBytes == null)
        {
            return Page();
        }

        await using var fs = new MemoryStream(run.FileBytes);
        using var sr = new StreamReader(fs, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        // Read header record
        var header = ReadCsvRecord(sr);
        if (header == null)
        {
            return Page();
        }
        Headers = header;

        // Skip rows for paging
        var skip = (Page - 1) * Take;
        for (int i = 0; i < skip; i++)
        {
            var skipped = ReadCsvRecord(sr);
            if (skipped == null)
            {
                // Reached EOF before expected; adjust page metrics
                FromRow = 0;
                ToRow = 0;
                return Page();
            }
        }

        // Read up to Take rows
        int count = 0;
        while (count < Take)
        {
            var row = ReadCsvRecord(sr);
            if (row == null) break; // EOF
            Rows.Add(row);
            count++;
        }

        FromRow = TotalRows == 0 ? 0 : (Page - 1) * Take + 1;
        ToRow = Math.Min(Page * Take, TotalRows);

        return Page();
    }

    private static List<string>? ReadCsvRecord(StreamReader sr)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        bool started = false;

        while (true)
        {
            int ci = sr.Read();
            if (ci == -1)
            {
                if (!started && sb.Length == 0 && result.Count == 0)
                {
                    return null; // EOF, no record
                }
                // finalize last field at EOF
                result.Add(sb.ToString());
                return result;
            }
            char c = (char)ci;

            if (c == '\r')
            {
                continue; // normalize newlines
            }
            if (c == '\n')
            {
                if (inQuotes)
                {
                    sb.Append('\n');
                    started = true;
                    continue;
                }
                // end of record
                result.Add(sb.ToString());
                return result;
            }

            if (c == '"')
            {
                if (!inQuotes)
                {
                    if (sb.Length == 0)
                    {
                        inQuotes = true;
                        started = true;
                    }
                    else
                    {
                        sb.Append('"');
                    }
                }
                else
                {
                    int peek = sr.Peek();
                    if (peek == '"')
                    {
                        sb.Append('"');
                        sr.Read(); // consume second quote
                        started = true;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                started = false;
                continue;
            }

            sb.Append(c);
            started = true;
        }
    }
}
