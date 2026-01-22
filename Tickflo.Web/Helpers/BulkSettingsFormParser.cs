namespace Tickflo.Web.Helpers;

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Tickflo.Core.Services.Workspace;

/// <summary>
/// Helper for parsing bulk settings update forms.
/// </summary>
public static partial class BulkSettingsFormParser
{
    /// <summary>
    /// Parses a form collection into a BulkSettingsUpdateRequest.
    /// </summary>
    /// <param name="form">The form collection to parse</param>
    /// <returns>A BulkSettingsUpdateRequest containing all parsed data</returns>
    public static BulkSettingsUpdateRequest Parse(IFormCollection form)
    {
        var request = new BulkSettingsUpdateRequest
        {
            WorkspaceName = form["Workspace.Name"].ToString(),
            WorkspaceSlug = form["Workspace.Slug"].ToString(),
            StatusUpdates = ParseStatusUpdates(form),
            NewStatus = ParseNewStatus(form),
            PriorityUpdates = ParsePriorityUpdates(form),
            NewPriority = ParseNewPriority(form),
            TypeUpdates = ParseTypeUpdates(form),
            NewType = ParseNewType(form)
        };

        return request;
    }

    private static List<StatusUpdate> ParseStatusUpdates(IFormCollection form)
    {
        var statusMatches = form.Keys
            .Select(k => StatusKeyRegex().Match(k))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups[1].Value));

        var updates = new List<StatusUpdate>();
        foreach (var group in statusMatches)
        {
            var statusId = group.Key;
            var deleteFlag = form[$"statuses[{statusId}].delete"].ToString();
            var name = form[$"statuses[{statusId}].name"].ToString();
            var color = form[$"statuses[{statusId}].color"].ToString();
            var order = form[$"statuses[{statusId}].sortOrder"].ToString();
            var closed = form[$"statuses[{statusId}].isClosedState"].ToString();

            updates.Add(new StatusUpdate
            {
                Id = statusId,
                Delete = !string.IsNullOrEmpty(deleteFlag),
                Name = string.IsNullOrWhiteSpace(name) ? null : name,
                Color = string.IsNullOrWhiteSpace(color) ? null : color,
                SortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : null,
                IsClosedState = closed is "true" or "on" ? true : null
            });
        }

        return updates;
    }

    private static StatusCreate? ParseNewStatus(IFormCollection form)
    {
        var name = (form["NewStatusName"].ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var color = (form["NewStatusColor"].ToString() ?? "neutral").Trim();
        return new StatusCreate
        {
            Name = name,
            Color = color,
            IsClosedState = false
        };
    }

    private static List<PriorityUpdate> ParsePriorityUpdates(IFormCollection form)
    {
        var priorityMatches = form.Keys
            .Select(k => PriorityKeyRegex().Match(k))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups[1].Value));

        var updates = new List<PriorityUpdate>();
        foreach (var group in priorityMatches)
        {
            var priorityId = group.Key;
            var deleteFlag = form[$"priorities[{priorityId}].delete"].ToString();
            var name = form[$"priorities[{priorityId}].name"].ToString();
            var color = form[$"priorities[{priorityId}].color"].ToString();
            var order = form[$"priorities[{priorityId}].sortOrder"].ToString();

            updates.Add(new PriorityUpdate
            {
                Id = priorityId,
                Delete = !string.IsNullOrEmpty(deleteFlag),
                Name = string.IsNullOrWhiteSpace(name) ? null : name,
                Color = string.IsNullOrWhiteSpace(color) ? null : color,
                SortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : null
            });
        }

        return updates;
    }

    private static PriorityCreate? ParseNewPriority(IFormCollection form)
    {
        var name = (form["NewPriorityName"].ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var color = (form["NewPriorityColor"].ToString() ?? "neutral").Trim();
        return new PriorityCreate
        {
            Name = name,
            Color = color
        };
    }

    private static List<TypeUpdate> ParseTypeUpdates(IFormCollection form)
    {
        var typeMatches = form.Keys
            .Select(k => TypeKeyRegex().Match(k))
            .Where(m => m.Success)
            .GroupBy(m => int.Parse(m.Groups[1].Value));

        var updates = new List<TypeUpdate>();
        foreach (var group in typeMatches)
        {
            var typeId = group.Key;
            var deleteFlag = form[$"types[{typeId}].delete"].ToString();
            var name = form[$"types[{typeId}].name"].ToString();
            var color = form[$"types[{typeId}].color"].ToString();
            var order = form[$"types[{typeId}].sortOrder"].ToString();

            updates.Add(new TypeUpdate
            {
                Id = typeId,
                Delete = !string.IsNullOrEmpty(deleteFlag),
                Name = string.IsNullOrWhiteSpace(name) ? null : name,
                Color = string.IsNullOrWhiteSpace(color) ? null : color,
                SortOrder = int.TryParse(order, out var sortOrder) ? sortOrder : null
            });
        }

        return updates;
    }

    private static TypeCreate? ParseNewType(IFormCollection form)
    {
        var name = (form["NewTypeName"].ToString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var color = (form["NewTypeColor"].ToString() ?? "neutral").Trim();
        return new TypeCreate
        {
            Name = name,
            Color = color
        };
    }

    [GeneratedRegex(@"^statuses\[(\d+)\]\.(.+)$")]
    private static partial Regex StatusKeyRegex();

    [GeneratedRegex(@"^priorities\[(\d+)\]\.(.+)$")]
    private static partial Regex PriorityKeyRegex();

    [GeneratedRegex(@"^types\[(\d+)\]\.(.+)$")]
    private static partial Regex TypeKeyRegex();
}
