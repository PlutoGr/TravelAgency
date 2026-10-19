using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;

namespace TravelAgency.Media.Infrastructure.Persistence;

/// <summary>
/// Пишет предупреждения PostgreSQL в лог приложения. Миграция снятия IsPublic
/// сообщает число изменённых записей через RAISE WARNING, а Npgsql сам пишет
/// такие сообщения только на уровне Debug.
/// </summary>
public sealed class PostgresNoticeLoggingInterceptor(ILogger logger) : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Subscribe(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Subscribe(connection);
        return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void Subscribe(DbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsql)
            return;

        npgsql.Notice -= OnNotice;
        npgsql.Notice += OnNotice;
    }

    private void OnNotice(object sender, NpgsqlNoticeEventArgs args)
    {
        var text = args.Notice.MessageText;
        if (text.Contains("IsPublic", StringComparison.Ordinal))
            logger.LogInformation("{Notice}", text);
    }
}
