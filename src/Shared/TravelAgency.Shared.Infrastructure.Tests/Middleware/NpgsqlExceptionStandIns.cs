namespace Npgsql
{
    internal sealed class InvalidOperationException(string message) : System.InvalidOperationException(message);

    internal sealed class InvalidCastException(string message) : System.InvalidCastException(message);
}

namespace Npgsql.Internal
{
    internal sealed class NpgsqlOperationException(string message) : System.Exception(message);
}
