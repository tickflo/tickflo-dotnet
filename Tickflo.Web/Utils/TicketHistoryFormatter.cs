namespace Tickflo.Web.Utils;

public static class TicketHistoryFormatter
{
    public static string FormatFieldName(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
            return "unknown field";

        return field switch
        {
            "Subject" => "subject",
            "Description" => "description",
            "Type" => "type",
            "Priority" => "priority",
            "Status" => "status",
            "ContactId" => "contact",
            "AssignedUserId" => "assignee",
            "AssignedTeamId" => "assigned team",
            "LocationId" => "location",
            "Inventory" => "inventory",
            "DueDate" => "due date",
            _ => field.ToLower()
        };
    }

    public static string FormatActionDescription(string? action, string? field, string? note)
    {
        if (string.IsNullOrWhiteSpace(action))
            return "performed an action";

        return action switch
        {
            "created" => "created this ticket",
            "field_changed" => $"changed {FormatFieldName(field)}",
            "assigned" => "assigned this ticket",
            "team_assigned" => "assigned this ticket to a team",
            "unassigned" => "removed the assignee",
            "reassignment_note" => "added a reassignment note",
            "closed" => "closed this ticket",
            "reopened" => "reopened this ticket",
            "resolved" => "marked this ticket as resolved",
            "cancelled" => "cancelled this ticket",
            _ => $"{action.Replace("_", " ")} {note ?? ""}".Trim()
        };
    }

    public static string FormatValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(empty)";

        return value;
    }

    public static bool ShouldShowValueChange(string? action)
    {
        return action == "field_changed";
    }
}
