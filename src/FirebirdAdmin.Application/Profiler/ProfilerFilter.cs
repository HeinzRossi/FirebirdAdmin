namespace FirebirdAdmin.Application.Profiler;

public sealed record ProfilerFilter(
    string? SqlText = null,
    TraceEventType? Type = null,
    string? UserName = null,
    TimeSpan? MinimumDuration = null,
    long? AttachmentId = null,
    long? TransactionId = null)
{
    public bool Matches(ProfilerEvent profilerEvent)
    {
        if (!string.IsNullOrWhiteSpace(SqlText) &&
            (profilerEvent.Sql is null || !profilerEvent.Sql.Contains(SqlText, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (Type is not null && profilerEvent.Type != Type)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(UserName) &&
            !string.Equals(profilerEvent.UserName, UserName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (MinimumDuration is not null && (profilerEvent.Duration is null || profilerEvent.Duration < MinimumDuration))
        {
            return false;
        }

        if (AttachmentId is not null && profilerEvent.AttachmentId != AttachmentId)
        {
            return false;
        }

        return TransactionId is null || profilerEvent.TransactionId == TransactionId;
    }
}
