namespace Tickflo.Web.Pages.Workspaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Workspace;

/// <summary>
/// PROTOTYPE MOCKUP — conversation-centric inbox with 100% mock data.
/// Only the workspace lookup/membership check is real; all tickets,
/// messages, contacts and views are fake. See migration plan Phase 2
/// for swapping in real services.
/// </summary>
[Authorize]
public class InboxModel(IWorkspaceService workspaceService) : WorkspacePageModel
{
    private readonly IWorkspaceService workspaceService = workspaceService;

    public string WorkspaceSlug { get; private set; } = string.Empty;
    public Workspace? Workspace { get; private set; }

    [BindProperty(SupportsGet = true)]
    public string View { get; set; } = "mine";

    [BindProperty(SupportsGet = true)]
    public int? Ticket { get; set; }

    public IReadOnlyList<InboxViewItem> Views { get; private set; } = [];
    public string ActiveViewLabel { get; private set; } = string.Empty;
    public IReadOnlyList<MockTicket> Tickets { get; private set; } = [];
    public MockTicket? SelectedTicket { get; private set; }
    public IReadOnlyList<MockMessage> Messages { get; private set; } = [];
    public IReadOnlyList<MockTicket> RecentContactTickets { get; private set; } = [];
    public bool HasSelection => this.SelectedTicket != null;

    public static IReadOnlyList<string> Agents { get; } = ["You", "Mike Chen", "Sarah Jones"];

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        this.WorkspaceSlug = slug;

        this.Workspace = await this.workspaceService.GetWorkspaceBySlugAsync(slug);
        if (this.Workspace == null)
        {
            return this.NotFound();
        }

        if (!this.TryGetUserId(out var currentUserId))
        {
            return this.Forbid();
        }

        var hasMembership = await this.workspaceService.UserHasMembershipAsync(currentUserId, this.Workspace.Id);
        if (!hasMembership)
        {
            return this.Forbid();
        }

        var all = MockInboxData.Tickets();
        this.Views = MockInboxData.BuildViews(all);
        this.ActiveViewLabel = this.Views.FirstOrDefault(v => v.Key == this.View)?.Label ?? "Inbox";

        this.Tickets = all.Where(t => MockInboxData.MatchesView(t, this.View)).ToList();

        // Auto-select the first ticket so the mockup shows the full layout immediately.
        var selectedId = this.Ticket ?? (this.Tickets.Count > 0 ? this.Tickets[0].Id : null);
        this.SelectedTicket = all.FirstOrDefault(t => t.Id == selectedId);

        if (this.SelectedTicket != null)
        {
            this.Messages = MockInboxData.ThreadFor(this.SelectedTicket);
            this.RecentContactTickets = all
                .Where(t => t.Contact.Name == this.SelectedTicket.Contact.Name && t.Id != this.SelectedTicket.Id)
                .Take(3)
                .ToList();
        }

        return this.Page();
    }
}

public record InboxViewItem(string Key, string Label, string Icon, int Count, bool IsCustom);

public record MockContact(
    string Name,
    string Email,
    string Phone,
    string Company,
    string Title,
    string Initials,
    string[] Tags);

public record MockTicket(
    int Id,
    string Subject,
    string Status,
    string StatusColor,
    string Priority,
    string PriorityColor,
    string Type,
    string? Assignee,
    bool AssignedToMe,
    bool IsClosed,
    bool Mentioned,
    bool IsVip,
    bool IsBug,
    bool Unread,
    string LastActivity,
    string Snippet,
    MockContact Contact,
    string Location);

public enum MockMessageKind
{
    Inbound,
    Outbound,
    Note,
    System
}

public record MockMessage(
    MockMessageKind Kind,
    string Author,
    string Initials,
    string Body,
    string Time,
    string? Channel);

public static class MockInboxData
{
    private static readonly MockContact Alice = new(
        "Alice Freeman", "alice@acmecorp.com", "+1 (555) 010-2233", "Acme Corp",
        "Operations Manager", "AF", ["VIP", "Portal"]);

    private static readonly MockContact Bob = new(
        "Bob Martinez", "bob@globex.com", "+1 (555) 441-8871", "Globex",
        "Office Administrator", "BM", ["Billing"]);

    private static readonly MockContact Carol = new(
        "Carol White", "carol@initech.com", "+1 (555) 902-1144", "Initech",
        "IT Coordinator", "CW", ["Onboarding"]);

    private static readonly MockContact David = new(
        "David Kim", "david@umbrella.io", "+1 (555) 331-7765", "Umbrella",
        "Facilities Lead", "DK", ["Hardware"]);

    private static readonly MockContact Emma = new(
        "Emma Roth", "emma@hooli.com", "+1 (555) 208-9900", "Hooli",
        "CTO", "ER", ["VIP", "API"]);

    public static List<MockTicket> Tickets() =>
    [
        new(1042, "Cannot login to portal", "Open", "info", "High", "error", "Support",
            "You", true, false, true, true, false, true, "12m",
            "I've tried resetting my password twice but never get the email…", Alice, "HQ — Floor 2"),
        new(1041, "Billing invoice discrepancy", "Open", "info", "Normal", "warning", "Billing",
            null, false, false, false, false, false, false, "45m",
            "Invoice #INV-2024-088 shows a different amount than our quote…", Bob, "Main Office"),
        new(1039, "Feature request: export to CSV", "Open", "info", "Low", "success", "Feature",
            "Mike Chen", false, false, false, false, false, false, "2h",
            "It would be great if we could export the monthly report to CSV…", Carol, "Annex"),
        new(1038, "Printer jam on 3rd floor", "Pending", "warning", "Normal", "warning", "Hardware",
            "You", true, false, false, false, false, false, "3h",
            "The big Ricoh printer keeps jamming on every second page…", David, "HQ — Floor 3"),
        new(1037, "API returning 500 errors", "Open", "info", "High", "error", "Bug",
            null, false, false, false, true, true, true, "5h",
            "Since 14:00 UTC our integration gets 500s on /v2/orders…", Emma, "Remote"),
        new(1035, "Onboarding: new employee setup", "Open", "info", "Normal", "warning", "Support",
            "Sarah Jones", false, false, false, false, false, false, "Yesterday",
            "We have 3 new hires starting Monday, can you provision accounts…", Carol, "Annex"),
        new(1033, "Password reset email not arriving", "Resolved", "success", "Normal", "warning", "Support",
            "You", true, true, false, false, false, false, "Yesterday",
            "Reset emails stopped arriving since yesterday afternoon…", Bob, "Main Office"),
        new(1031, "Security audit questionnaire", "Open", "info", "Normal", "warning", "Support",
            "You", true, false, true, true, false, false, "2d",
            "Our compliance team needs the SOC2 questionnaire filled…", Emma, "Remote"),
        new(1029, "Update payment method", "Closed", "neutral", "Low", "success", "Billing",
            "Sarah Jones", false, true, false, false, false, false, "3d",
            "Our corporate card expired, need to update before renewal…", Alice, "HQ — Floor 2"),
        new(1027, "Laptop battery replacement", "Open", "info", "Low", "success", "Hardware",
            null, false, false, false, false, false, false, "4d",
            "Battery on the Dell XPS 13 swells, needs replacement…", David, "HQ — Floor 3"),
        new(1025, "SSO configuration help", "Open", "info", "Normal", "warning", "Support",
            "Mike Chen", false, false, false, false, false, false, "5d",
            "We're switching to Okta and need help with the SAML setup…", Bob, "Main Office"),
    ];

    public static bool MatchesView(MockTicket t, string view) => view switch
    {
        "mine" => t.AssignedToMe && !t.IsClosed,
        "unassigned" => t.Assignee == null && !t.IsClosed,
        "open" => !t.IsClosed,
        "closed" => t.IsClosed,
        "mentioned" => t.Mentioned,
        "vip" => t.IsVip && !t.IsClosed,
        "bugs" => t.IsBug && !t.IsClosed,
        _ => !t.IsClosed,
    };

    public static IReadOnlyList<InboxViewItem> BuildViews(List<MockTicket> all)
    {
        int Count(string key) => all.Count(t => MatchesView(t, key));
        return
        [
            new("mine", "My inbox", "fa-user-check", Count("mine"), false),
            new("unassigned", "Unassigned", "fa-user-slash", Count("unassigned"), false),
            new("open", "All open", "fa-inbox", Count("open"), false),
            new("mentioned", "Mentioned", "fa-at", Count("mentioned"), false),
            new("closed", "Closed", "fa-check", Count("closed"), false),
            new("vip", "VIP customers", "fa-star", Count("vip"), true),
            new("bugs", "Bugs this week", "fa-bug", Count("bugs"), true),
        ];
    }

    public static IReadOnlyList<MockMessage> ThreadFor(MockTicket ticket)
    {
        if (ticket.Id == 1042)
        {
            return
            [
                new(MockMessageKind.Inbound, "Alice Freeman", "AF",
                    "Hi, since this morning I cannot login to the portal. I've tried resetting my password twice but I never get the reset email. This is blocking our whole team — can you help urgently?\n\nThanks,\nAlice",
                    "9:41 AM", "email"),
                new(MockMessageKind.System, "System", "",
                    "Ticket created via email · Assigned to You", "9:41 AM", null),
                new(MockMessageKind.Outbound, "You", "YO",
                    "Hi Alice, sorry to hear that. I've checked the mail logs and it looks like our reset emails are being quarantined by your spam filter. I've whitelisted our domain on our side — could you try the reset once more?",
                    "10:02 AM", "email"),
                new(MockMessageKind.Inbound, "Alice Freeman", "AF",
                    "Just tried again — still nothing in inbox or spam. Getting a bit desperate here 😅",
                    "10:15 AM", "email"),
                new(MockMessageKind.Note, "You", "YO",
                    "@Mike Chen can you check the Mailgun suppression list for acmecorp.com? Bounce rate spiked yesterday.",
                    "10:18 AM", null),
                new(MockMessageKind.System, "System", "",
                    "Mike Chen joined the conversation", "10:20 AM", null),
                new(MockMessageKind.Outbound, "Mike Chen", "MC",
                    "Found it — alice@acmecorp.com was on the suppression list after a hard bounce last week. Removed it and triggered a fresh reset email. Alice, you should have it within a minute.",
                    "10:31 AM", "email"),
                new(MockMessageKind.Inbound, "Alice Freeman", "AF",
                    "In!! Thank you both so much 🙏",
                    "10:34 AM", "email"),
            ];
        }

        // Generic thread for every other mock ticket.
        return
        [
            new(MockMessageKind.Inbound, ticket.Contact.Name, ticket.Contact.Initials,
                $"Hi team,\n\n{ticket.Snippet.TrimEnd('…')}. Could you take a look when you get a chance?\n\nThanks,\n{ticket.Contact.Name.Split(' ')[0]}",
                "9:12 AM", "email"),
            new(MockMessageKind.System, "System", "",
                ticket.Assignee == null ? "Ticket created via email" : $"Ticket created via email · Assigned to {ticket.Assignee}",
                "9:12 AM", null),
            new(MockMessageKind.Outbound, ticket.Assignee ?? "You", ticket.Assignee == "Mike Chen" ? "MC" : ticket.Assignee == "Sarah Jones" ? "SJ" : "YO",
                $"Hi {ticket.Contact.Name.Split(' ')[0]}, thanks for the details — we're looking into it now and will get back to you shortly.",
                "9:48 AM", "email"),
            new(MockMessageKind.Note, "You", "YO",
                "Worth checking if this is related to the similar report from last week.",
                "9:50 AM", null),
        ];
    }
}
