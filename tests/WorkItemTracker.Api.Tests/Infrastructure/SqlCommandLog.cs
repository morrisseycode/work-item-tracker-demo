using System.Collections.Concurrent;
using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WorkItemTracker.Api.Tests.Infrastructure;

// EF Core creates one DbCommand for every SQL command it sends to the database.
public sealed class SqlCommandLog : DbCommandInterceptor
{
    private readonly ConcurrentQueue<string> _texts = new();
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public IReadOnlyCollection<string> Texts => _texts;

    public void Reset()
    {
        Interlocked.Exchange(ref _count, 0);
        _texts.Clear();
    }

    public override DbCommand CommandCreated(CommandEndEventData eventData, DbCommand result)
    {
        Interlocked.Increment(ref _count);
        return result;
    }

    // The command text is set by the time EF Core reports the command as initialized.
    public override DbCommand CommandInitialized(CommandEndEventData eventData, DbCommand result)
    {
        _texts.Enqueue(result.CommandText);
        return result;
    }
}
