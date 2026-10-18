using Microsoft.Data.SqlClient;

namespace PADO.ContactFlow.Web.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}