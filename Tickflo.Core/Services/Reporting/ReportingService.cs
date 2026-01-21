namespace Tickflo.Core.Services.Reporting;

using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using InventoryEntity = Entities.Inventory;

public class ReportingService(TickfloDbContext dbContext) : IReportingService
{
    private readonly TickfloDbContext dbContext = dbContext;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public IReadOnlyDictionary<string, string[]> GetAvailableSources() => new Dictionary<string, string[]>
    {
        ["tickets"] = ["Id", "Subject", "Description", "TypeId", "PriorityId", "StatusId", "AssignedUserId", "AssignedTeamId", "CreatedAt", "UpdatedAt", "ContactId", "ChargeAmount", "ChargeAmountAtLocation"],
        ["contacts"] = ["Id", "Name", "Email", "Phone", "Company", "Title", "Priority", "Status", "AssignedUserId", "LastInteraction", "CreatedAt"],
        ["locations"] = ["Id", "Name", "Address", "Active", "InventoryCount", "TicketCount", "OpenTicketCount", "LastTicketAt"],
        ["inventory"] = ["Id", "Sku", "Name", "Description", "Quantity", "LocationId", "MinStock", "Cost", "Price", "Category", "Status", "Tags", "LastRestockAt", "CreatedAt", "UpdatedAt", "TicketCount", "OpenTicketCount", "LastTicketAt"],
    };

    private sealed class ReportDef
    {
        public string Source { get; set; } = "tickets";
        public List<string> Fields { get; set; } = [];
        public List<FilterDef> Filters { get; set; } = [];
        public List<OrderDef> OrderBy { get; set; } = [];
    }
    private sealed class FilterDef
    {
        public string Field { get; set; } = string.Empty;
        public string Op { get; set; } = "eq"; // eq, neq, lt, lte, gt, gte, contains, between
        public JsonElement Value { get; set; }
    }
    private sealed class OrderDef
    {
        public string Field { get; set; } = string.Empty;
        public string Dir { get; set; } = "asc";
    }

    public async Task<ReportExecutionResult> ExecuteAsync(int workspaceId, Report report, CancellationToken ct = default)
    {
        var def = ParseDefinition(report.DefinitionJson);
        def.Fields = this.NormalizeFields(def.Source, def.Fields);

        var rows = def.Source.ToLowerInvariant() switch
        {
            "tickets" => await this.QueryTickets(workspaceId, def, ct),
            "contacts" => await this.QueryContacts(workspaceId, def, ct),
            "locations" => await this.QueryLocations(workspaceId, def, ct),
            "inventory" => await this.QueryInventory(workspaceId, def, ct),
            _ => throw new InvalidOperationException($"Unknown report source: {def.Source}")
        };

        // Generate CSV bytes and return (DB-only storage)
        var bytes = this.GenerateCsvBytes(def.Fields);
        var fileName = $"run_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return new ReportExecutionResult(rows, string.Empty, bytes, fileName, "text/csv");
    }

    public Task<ReportRunPage> GetRunPageAsync(ReportRun run, int page, int take, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(run);

        var totalRows = run.RowCount;
        var clampedTake = Math.Clamp(take <= 0 ? 500 : take, 1, 5000);
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalRows / Math.Max(1, clampedTake)));
        var clampedPage = Math.Clamp(page <= 0 ? 1 : page, 1, totalPages);

        if (run.FileBytes == null || run.FileBytes.Length == 0)
        {
            var empty = new ReportRunPage(clampedPage, clampedTake, totalRows, totalPages, 0, 0, false,
                [], []);
            return Task.FromResult(empty);
        }

        using var fs = new MemoryStream(run.FileBytes);
        using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var header = ReadCsvRecord(sr);
        if (header == null)
        {
            var empty = new ReportRunPage(clampedPage, clampedTake, totalRows, totalPages, 0, 0, false,
                [], []);
            return Task.FromResult(empty);
        }

        var skip = (clampedPage - 1) * clampedTake;
        for (var i = 0; i < skip; i++)
        {
            var skipped = ReadCsvRecord(sr);
            if (skipped == null)
            {
                var empty = new ReportRunPage(clampedPage, clampedTake, totalRows, totalPages, 0, 0, false,
                    header, []);
                return Task.FromResult(empty);
            }
        }

        var rows = new List<IReadOnlyList<string>>();
        var count = 0;
        while (count < clampedTake)
        {
            var row = ReadCsvRecord(sr);
            if (row == null)
            {
                break;
            }

            rows.Add(row);
            count++;
        }

        var fromRow = totalRows == 0 ? 0 : ((clampedPage - 1) * clampedTake) + 1;
        var toRow = Math.Min(clampedPage * clampedTake, totalRows);

        var result = new ReportRunPage(clampedPage, clampedTake, totalRows, totalPages, fromRow, toRow, true, header, rows);
        return Task.FromResult(result);
    }

    private static ReportDef ParseDefinition(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ReportDef { Source = "tickets", Fields = ["Id", "Subject", "StatusId", "CreatedAt"] };
        }
        try
        {
            var def = JsonSerializer.Deserialize<ReportDef>(json, JsonOptions) ?? throw new InvalidOperationException("Failed to parse report definition.");

            return def;
        }
        catch
        {
            return new ReportDef { Source = "tickets", Fields = ["Id", "Subject", "StatusId", "CreatedAt"] };
        }
    }

    private List<string> NormalizeFields(string source, List<string> fields)
    {
        var map = this.GetAvailableSources();
        if (!map.TryGetValue(source.ToLowerInvariant(), out var allowed))
        {
            source = "tickets";
            allowed = map[source];
        }
        if (fields == null || fields.Count == 0)
        {
            return [.. allowed.Take(5)];
        }

        return [.. fields.Where(f => allowed.Contains(f))];
    }

    private async Task<int> QueryTickets(int workspaceId, ReportDef def, CancellationToken ct)
    {
        var q = this.dbContext.Tickets.AsNoTracking().Where(t => t.WorkspaceId == workspaceId);
        var filterLocationIds = ExtractLocationFilterIds(def.Filters);
        var chargeLocationIds = ExtractChargeLocationFilterIds(def.Filters) ?? filterLocationIds;
        q = this.ApplyTicketFilters(q, def.Filters, workspaceId);
        q = ApplyTicketOrdering(q, def.OrderBy);
        var list = await q.ToListAsync(ct);
        this.currentRows.Clear();
        foreach (var t in list)
        {
            var row = new Dictionary<string, object?>();
            foreach (var f in def.Fields)
            {
                row[f] = f switch
                {
                    "Id" => t.Id,
                    "Subject" => t.Subject,
                    "Description" => t.Description,
                    "TypeId" => t.TicketTypeId,
                    "PriorityId" => t.PriorityId,
                    "StatusId" => t.StatusId,
                    "AssignedUserId" => t.AssignedUserId,
                    "AssignedTeamId" => t.AssignedTeamId,
                    "CreatedAt" => t.CreatedAt,
                    "UpdatedAt" => t.UpdatedAt,
                    "ContactId" => t.ContactId,
                    "ChargeAmount" => this.ComputeTicketCharge(t.Id, workspaceId),
                    "ChargeAmountAtLocation" => this.ComputeTicketChargeForLocations(t.Id, chargeLocationIds, workspaceId),
                    _ => null
                };
            }
            this.currentRows.Add(row);
        }
        return this.currentRows.Count;
    }

    private decimal ComputeTicketCharge(int ticketId, int workspaceId)
    {
        var query = from ti in this.dbContext.TicketInventories.AsNoTracking()
                    join inv in this.dbContext.Inventory.AsNoTracking() on ti.InventoryId equals inv.Id
                    where ti.TicketId == ticketId && inv.WorkspaceId == workspaceId
                    select new { ti.Quantity, ti.UnitPrice, inv.Price };
        var lines = query.ToList();
        var sum = 0m;
        foreach (var l in lines)
        {
            var price = l.UnitPrice != 0m ? l.UnitPrice : (l.Price ?? 0m);
            sum += price * l.Quantity;
        }
        return sum;
    }

    private decimal ComputeTicketChargeForLocations(int ticketId, List<int>? locationIds, int workspaceId)
    {
        // Pull ticket line items joined to inventory; optionally restrict to a location
        var query = from ti in this.dbContext.TicketInventories.AsNoTracking()
                    join inv in this.dbContext.Inventory.AsNoTracking() on ti.InventoryId equals inv.Id
                    where ti.TicketId == ticketId && inv.WorkspaceId == workspaceId
                    select new { ti.Quantity, ti.UnitPrice, inv.LocationId, inv.Price };
        if (locationIds is { Count: > 0 })
        {
            query = query.Where(x => x.LocationId != null && locationIds.Contains(x.LocationId.Value));
        }
        var lines = query.ToList();
        var sum = 0m;
        foreach (var l in lines)
        {
            var price = l.UnitPrice != 0m ? l.UnitPrice : (l.Price ?? 0m);
            sum += price * l.Quantity;
        }
        return sum;
    }

    private async Task<int> QueryContacts(int workspaceId, ReportDef def, CancellationToken ct)
    {
        var q = this.dbContext.Contacts.AsNoTracking().Where(c => c.WorkspaceId == workspaceId);
        q = ApplyContactFilters(q, def.Filters);
        q = ApplyContactOrdering(q, def.OrderBy);
        var list = await q.ToListAsync(ct);
        this.currentRows.Clear();
        foreach (var c in list)
        {
            var row = new Dictionary<string, object?>();
            foreach (var f in def.Fields)
            {
                row[f] = f switch
                {
                    "Id" => c.Id,
                    "Name" => c.Name,
                    "Email" => c.Email,
                    "Phone" => c.Phone,
                    "Company" => c.Company,
                    "Title" => c.Title,
                    "Priority" => c.Priority,
                    "Status" => c.Status,
                    "AssignedUserId" => c.AssignedUserId,
                    "LastInteraction" => c.LastInteraction,
                    "CreatedAt" => c.CreatedAt,
                    _ => null
                };
            }
            this.currentRows.Add(row);
        }
        return this.currentRows.Count;
    }

    private async Task<int> QueryLocations(int workspaceId, ReportDef def, CancellationToken ct)
    {
        var q = this.dbContext.Locations.AsNoTracking().Where(l => l.WorkspaceId == workspaceId);
        q = ApplyLocationFilters(q, def.Filters);
        var list = await q.ToListAsync(ct);
        this.currentRows.Clear();
        foreach (var l in list)
        {
            var invCount = await this.dbContext.Inventory.AsNoTracking().CountAsync(i => i.WorkspaceId == workspaceId && i.LocationId == l.Id, ct);
            // Tickets counted by InventoryEntity present at this location
            var ticketJoin = from ti in this.dbContext.TicketInventories.AsNoTracking()
                             join i in this.dbContext.Inventory.AsNoTracking() on ti.InventoryId equals i.Id
                             join t in this.dbContext.Tickets.AsNoTracking() on ti.TicketId equals t.Id
                             where i.WorkspaceId == workspaceId && i.LocationId == l.Id && t.WorkspaceId == workspaceId
                             select t;
            var totalTickets = await ticketJoin.Select(t => t.Id).Distinct().CountAsync(ct);
            var openTickets = await ticketJoin.Where(t => !t.StatusId.HasValue).Select(t => t.Id).Distinct().CountAsync(ct);  // Placeholder: actual closed check needs status repo lookup
            var lastTicketAt = await ticketJoin.OrderByDescending(t => t.CreatedAt).Select(t => (DateTime?)t.CreatedAt).FirstOrDefaultAsync(ct);

            var row = new Dictionary<string, object?>();
            foreach (var f in def.Fields)
            {
                row[f] = f switch
                {
                    "Id" => l.Id,
                    "Name" => l.Name,
                    "Address" => l.Address,
                    "Active" => l.Active,
                    "InventoryCount" => invCount,
                    "TicketCount" => totalTickets,
                    "OpenTicketCount" => openTickets,
                    "LastTicketAt" => lastTicketAt,
                    _ => null
                };
            }
            this.currentRows.Add(row);
        }
        return this.currentRows.Count;
    }

    private async Task<int> QueryInventory(int workspaceId, ReportDef def, CancellationToken ct)
    {
        var q = this.dbContext.Inventory.AsNoTracking().Where(i => i.WorkspaceId == workspaceId);
        q = ApplyInventoryFilters(q, def.Filters);
        var list = await q.ToListAsync(ct);
        this.currentRows.Clear();
        foreach (var i in list)
        {
            var ticketJoin = from ti in this.dbContext.TicketInventories.AsNoTracking()
                             join t in this.dbContext.Tickets.AsNoTracking() on ti.TicketId equals t.Id
                             where ti.InventoryId == i.Id && t.WorkspaceId == workspaceId
                             select t;
            var totalTickets = await ticketJoin.Select(t => t.Id).Distinct().CountAsync(ct);
            var openTickets = await ticketJoin.Where(t => !t.StatusId.HasValue).Select(t => t.Id).Distinct().CountAsync(ct);  // Placeholder: actual closed check needs status repo lookup
            var lastTicketAt = await ticketJoin.OrderByDescending(t => t.CreatedAt).Select(t => (DateTime?)t.CreatedAt).FirstOrDefaultAsync(ct);

            var row = new Dictionary<string, object?>();
            foreach (var f in def.Fields)
            {
                row[f] = f switch
                {
                    "Id" => i.Id,
                    "Sku" => i.Sku,
                    "Name" => i.Name,
                    "Description" => i.Description,
                    "Quantity" => i.Quantity,
                    "LocationId" => i.LocationId,
                    "MinStock" => i.MinStock,
                    "Cost" => i.Cost,
                    "Price" => i.Price,
                    "Category" => i.Category,
                    "Status" => i.Status,
                    "Tags" => i.Tags,
                    "LastRestockAt" => i.LastRestockAt,
                    "CreatedAt" => i.CreatedAt,
                    "UpdatedAt" => i.UpdatedAt,
                    "TicketCount" => totalTickets,
                    "OpenTicketCount" => openTickets,
                    "LastTicketAt" => lastTicketAt,
                    _ => null
                };
            }
            this.currentRows.Add(row);
        }
        return this.currentRows.Count;
    }

    private IQueryable<Ticket> ApplyTicketFilters(IQueryable<Ticket> q, List<FilterDef> filters, int workspaceId)
    {
        foreach (var f in filters)
        {
            switch (f.Field)
            {
                case "StatusId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var sid))
                    {
                        q = q.Where(t => t.StatusId == sid);
                    }

                    break;
                case "PriorityId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var pid))
                    {
                        q = q.Where(t => t.PriorityId == pid);
                    }

                    break;
                case "TypeId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var tid))
                    {
                        q = q.Where(t => t.TicketTypeId == tid);
                    }

                    break;
                case "AssignedUserId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var uid))
                    {
                        q = q.Where(t => t.AssignedUserId == uid);
                    }

                    break;
                case "AssignedTeamId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var teamid))
                    {
                        q = q.Where(t => t.AssignedTeamId == teamid);
                    }

                    break;
                case "ContactId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var cid))
                    {
                        q = q.Where(t => t.ContactId == cid);
                    }

                    break;
                case "CreatedAt":
                    if (f.Op == "between" && f.Value.ValueKind == JsonValueKind.Array && f.Value.GetArrayLength() == 2)
                    {
                        var from = f.Value[0].GetDateTime();
                        var to = f.Value[1].GetDateTime();
                        q = q.Where(t => t.CreatedAt >= from && t.CreatedAt <= to);
                    }
                    break;
                case "LocationId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var lid))
                    {
                        q = q.Where(t => this.dbContext.TicketInventories.Any(ti => ti.TicketId == t.Id &&
                                                              this.dbContext.Inventory.Any(i => i.Id == ti.InventoryId && i.LocationId == lid && i.WorkspaceId == workspaceId)));
                    }
                    else if (f.Op == "in" && f.Value.ValueKind == JsonValueKind.Array)
                    {
                        var lids = ExtractIntArray(f.Value);
                        if (lids.Count > 0)
                        {
                            q = q.Where(t => this.dbContext.TicketInventories.Any(ti => ti.TicketId == t.Id &&
                                                          this.dbContext.Inventory.Any(i => i.Id == ti.InventoryId && i.LocationId != null && lids.Contains(i.LocationId.Value) && i.WorkspaceId == workspaceId)));
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        return q;
    }

    private static List<int>? ExtractLocationFilterIds(List<FilterDef> filters)
    {
        foreach (var f in filters)
        {
            if (string.Equals(f.Field, "LocationId", StringComparison.OrdinalIgnoreCase))
            {
                if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var lid))
                {
                    return [lid];
                }

                if (f.Op == "in" && f.Value.ValueKind == JsonValueKind.Array)
                {
                    var lids = ExtractIntArray(f.Value);
                    if (lids.Count > 0)
                    {
                        return lids;
                    }
                }
            }
        }
        return null;
    }

    private static List<int>? ExtractChargeLocationFilterIds(List<FilterDef> filters)
    {
        foreach (var f in filters)
        {
            if (string.Equals(f.Field, "ChargeLocationId", StringComparison.OrdinalIgnoreCase))
            {
                if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var lid))
                {
                    return [lid];
                }

                if (f.Op == "in" && f.Value.ValueKind == JsonValueKind.Array)
                {
                    var lids = ExtractIntArray(f.Value);
                    if (lids.Count > 0)
                    {
                        return lids;
                    }
                }
            }
        }
        return null;
    }

    private static List<int> ExtractIntArray(JsonElement value)
    {
        var list = new List<int>();
        foreach (var el in value.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v))
            {
                list.Add(v);
            }
        }
        return list;
    }

    private static IQueryable<Contact> ApplyContactFilters(IQueryable<Contact> q, List<FilterDef> filters)
    {
        foreach (var f in filters)
        {
            switch (f.Field)
            {
                case "Status":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.String)
                    {
                        q = q.Where(c => c.Status == f.Value.GetString());
                    }

                    break;
                case "Priority":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.String)
                    {
                        q = q.Where(c => c.Priority == f.Value.GetString());
                    }

                    break;
                case "CreatedAt":
                    if (f.Op == "between" && f.Value.ValueKind == JsonValueKind.Array && f.Value.GetArrayLength() == 2)
                    {
                        var from = f.Value[0].GetDateTime();
                        var to = f.Value[1].GetDateTime();
                        q = q.Where(c => c.CreatedAt >= from && c.CreatedAt <= to);
                    }
                    break;
                default:
                    break;
            }
        }
        return q;
    }

    private static IQueryable<Location> ApplyLocationFilters(IQueryable<Location> q, List<FilterDef> filters)
    {
        foreach (var f in filters)
        {
            switch (f.Field)
            {
                case "Name":
                    if (f.Op == "contains" && f.Value.ValueKind == JsonValueKind.String)
                    {
                        var v = f.Value.GetString() ?? string.Empty;
                        q = q.Where(l => l.Name.Contains(v));
                    }
                    break;
                case "Active":
                    if ((f.Op == "eq" && f.Value.ValueKind == JsonValueKind.True) || f.Value.ValueKind == JsonValueKind.False)
                    {
                        var v = f.Value.ValueKind == JsonValueKind.True;
                        q = q.Where(l => l.Active == v);
                    }
                    break;
                default:
                    break;
                    // CreatedAt is not modeled on Location entity; skip
            }
        }
        return q;
    }

    private static IQueryable<InventoryEntity> ApplyInventoryFilters(IQueryable<InventoryEntity> q, List<FilterDef> filters)
    {
        foreach (var f in filters)
        {
            switch (f.Field)
            {
                case "Status":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.String)
                    {
                        q = q.Where(i => i.Status == f.Value.GetString());
                    }

                    break;
                case "Sku":
                    if (f.Op == "contains" && f.Value.ValueKind == JsonValueKind.String)
                    {
                        var v = f.Value.GetString() ?? string.Empty;
                        q = q.Where(i => i.Sku.Contains(v));
                    }
                    break;
                case "LocationId":
                    if (f.Op == "eq" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var lid))
                    {
                        q = q.Where(i => i.LocationId == lid);
                    }

                    break;
                case "Quantity":
                    if (f.Op == "gte" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var qmin))
                    {
                        q = q.Where(i => i.Quantity >= qmin);
                    }

                    if (f.Op == "lte" && f.Value.ValueKind == JsonValueKind.Number && f.Value.TryGetInt32(out var qmax))
                    {
                        q = q.Where(i => i.Quantity <= qmax);
                    }

                    break;
                case "CreatedAt":
                    if (f.Op == "between" && f.Value.ValueKind == JsonValueKind.Array && f.Value.GetArrayLength() == 2)
                    {
                        var from = f.Value[0].GetDateTime();
                        var to = f.Value[1].GetDateTime();
                        q = q.Where(i => i.CreatedAt >= from && i.CreatedAt <= to);
                    }
                    break;
                default:
                    break;
            }
        }
        return q;
    }

    private static IQueryable<Ticket> ApplyTicketOrdering(IQueryable<Ticket> q, List<OrderDef> orders)
    {
        foreach (var o in orders)
        {
            if (o.Field == "CreatedAt")
            {
                q = o.Dir == "desc" ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt);
            }
            else if (o.Field == "UpdatedAt")
            {
                q = o.Dir == "desc" ? q.OrderByDescending(t => t.UpdatedAt) : q.OrderBy(t => t.UpdatedAt);
            }
        }
        return q;
    }
    private static IQueryable<Contact> ApplyContactOrdering(IQueryable<Contact> q, List<OrderDef> orders)
    {
        foreach (var o in orders)
        {
            if (o.Field == "CreatedAt")
            {
                q = o.Dir == "desc" ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt);
            }
            else if (o.Field == "LastInteraction")
            {
                q = o.Dir == "desc" ? q.OrderByDescending(t => t.LastInteraction) : q.OrderBy(t => t.LastInteraction);
            }
        }
        return q;
    }

    // TODO: This is horrible
    private readonly List<IDictionary<string, object?>> currentRows = [];

    private static List<string>? ReadCsvRecord(StreamReader sr)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        var started = false;

        while (true)
        {
            var ci = sr.Read();
            if (ci == -1)
            {
                if (!started && sb.Length == 0 && result.Count == 0)
                {
                    return null;
                }
                result.Add(sb.ToString());
                return result;
            }
            var c = (char)ci;

            if (c == '\r')
            {
                continue;
            }
            if (c == '\n')
            {
                if (inQuotes)
                {
                    sb.Append('\n');
                    started = true;
                    continue;
                }
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
                    var peek = sr.Peek();
                    if (peek == '"')
                    {
                        sb.Append('"');
                        sr.Read();
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

    private byte[] GenerateCsvBytes(List<string> headers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers.Select(this.EscapeCsv)));
        foreach (var row in this.currentRows)
        {
            var vals = headers.Select(h => this.EscapeCsv(FormatCsv(row.TryGetValue(h, out var v) ? v : null)));
            sb.AppendLine(string.Join(',', vals));
        }
        // UTF-8 with BOM
        var utf8bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. utf8bom.GetPreamble(), .. utf8bom.GetBytes(sb.ToString())];
    }

    private string EscapeCsv(string? v)
    {
        v ??= string.Empty;
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
        {
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
        return v;
    }

    private static string FormatCsv(object? v) => v switch
    {
        null => string.Empty,
        DateTime dt => dt.ToString("o"),
        DateTimeOffset dto => dto.ToString("o"),
        bool b => b ? "true" : "false",
        _ => v.ToString() ?? string.Empty
    };
}




