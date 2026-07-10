using System.Globalization;
using Microsoft.Data.SqlClient;

namespace Oversight;

// Convert-based so the same helper reads int and bigint DMV columns.
internal static class SqlDataReaderExtensions
{
    internal static int ReadInt32(this SqlDataReader reader, string name) =>
        reader[name] is DBNull ? 0 : Convert.ToInt32(reader[name], CultureInfo.InvariantCulture);

    internal static long ReadInt64(this SqlDataReader reader, string name) =>
        reader[name] is DBNull ? 0 : Convert.ToInt64(reader[name], CultureInfo.InvariantCulture);

    internal static double ReadDouble(this SqlDataReader reader, string name) =>
        reader[name] is DBNull ? 0 : Convert.ToDouble(reader[name], CultureInfo.InvariantCulture);

    internal static string ReadString(this SqlDataReader reader, string name) =>
        reader[name] as string ?? string.Empty;
}
