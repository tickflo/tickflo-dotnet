namespace Tickflo.Web.Utils;

public static class TicketHistoryFormatter
{
    private const string ActionCreated = "created";
    private const string ActionFieldChanged = "field_changed";
    private const string ActionAssigned = "assigned";
    private const string ActionTeamAssigned = "team_assigned";
    private const string ActionUnassigned = "unassigned";
    private const string ActionReassignmentNote = "reassignment_note";
    private const string ActionClosed = "closed";
    private const string ActionReopened = "reopened";
    private const string ActionResolved = "resolved";
    private const string ActionCancelled = "cancelled";

    private const string FieldSubject = "Subject";
    private const string FieldDescription = "Description";
    private const string FieldType = "Type";
    private const string FieldPriority = "Priority";
    private const string FieldStatus = "Status";
    private const string FieldContactId = "ContactId";
    private const string FieldAssignedUserId = "AssignedUserId";
    private const string FieldAssignedTeamId = "AssignedTeamId";
    private const string FieldLocationId = "LocationId";
    private const string FieldInventory = "Inventory";
    private const string FieldDueDate = "DueDate";

    private const string DefaultFieldName = "unknown field";
    private const string DefaultActionText = "performed an action";
    private const string EmptyValueText = "(empty)";

    public static string FormatFieldName(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return DefaultFieldName;
        }

        return field switch
        {
            FieldSubject => "subject",
            FieldDescription => "description",
            FieldType => "type",
            FieldPriority => "priority",
            FieldStatus => "status",
            FieldContactId => "contact",
            FieldAssignedUserId => "assignee",
            FieldAssignedTeamId => "assigned team",
            FieldLocationId => "location",
            FieldInventory => "inventory",
            FieldDueDate => "due date",
            _ => field.ToLower(System.Globalization.CultureInfo.CurrentCulture)
        };
    }

    public static string FormatActionDescription(string? action, string? field, string? note)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return DefaultActionText;
        }

        return action switch
        {
            ActionCreated => "created this ticket",
            ActionFieldChanged => $"changed {FormatFieldName(field)}",
            ActionAssigned => "assigned this ticket",
            ActionTeamAssigned => "assigned this ticket to a team",
            ActionUnassigned => "removed the assignee",
            ActionReassignmentNote => "added a reassignment note",
            ActionClosed => "closed this ticket",
            ActionReopened => "reopened this ticket",
            ActionResolved => "marked this ticket as resolved",
            ActionCancelled => "cancelled this ticket",
            _ => FormatUnknownAction(action, note)
        };
    }

    private static string FormatUnknownAction(string action, string? note)
    {
        var actionText = action.Replace("_", " ");
        return string.IsNullOrEmpty(note) ? actionText : $"{actionText} {note}".Trim();
    }

    public static string FormatValue(string? value) => string.IsNullOrWhiteSpace(value) ? EmptyValueText : value;

    public static bool ShouldShowValueChange(string? action) => action == ActionFieldChanged;
}
