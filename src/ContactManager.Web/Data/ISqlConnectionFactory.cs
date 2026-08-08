using Microsoft.Data.SqlClient;

namespace ContactManager.Web.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}