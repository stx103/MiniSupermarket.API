using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MiniSupermarket.API.Data
{
    public sealed class AdoDatabase
    {
        private readonly SupermarketDbContext _context;
        public AdoDatabase(SupermarketDbContext context) => _context = context;
        // [Tiêu chí 32] Lấy kết nối đã cấu hình UseSqlServer từ DbContext: đối tượng thực tế là SqlConnection, dùng chung chuỗi kết nối JSON.
        public DbConnection Connection => _context.Database.GetDbConnection();

        public async Task<ConnectionLease> OpenAsync(CancellationToken cancellationToken)
        {
            var opened = Connection.State != ConnectionState.Open;
            if (opened) await Connection.OpenAsync(cancellationToken);
            return new ConnectionLease(Connection, opened);
        }

        public static DbCommand Command(DbConnection connection, string sql, DbTransaction? transaction = null)
        {
            // [Tiêu chí 32] SqlConnection.CreateCommand tạo SqlCommand để thực thi truy vấn ADO.NET (barcode, báo cáo, thanh toán).
            var command = connection.CreateCommand();
            command.CommandText = sql;
            // [Tiêu chí 35] Gắn command vào transaction truyền vào để mọi lệnh thanh toán cùng được Commit hoặc Rollback.
            command.Transaction = transaction;
            return command;
        }

        public static void Parameter(DbCommand command, string name, object? value, DbType type, int size = 0)
        {
            DbParameter parameter;
            // [Tiêu chí 33] SQL Server dùng Parameters.AddWithValue cho giá trị truyền vào; khai báo thêm kiểu/kích thước bên dưới để tránh suy luận sai.
            if (command is SqlCommand sqlCommand)
                parameter = sqlCommand.Parameters.AddWithValue(name, value ?? DBNull.Value);
            else
            {
                parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
            // Specify the type/size even with AddWithValue; do not depend on inferred SQL types.
            parameter.DbType = type;
            if (size > 0) parameter.Size = size;
            if (type == DbType.Decimal) { parameter.Precision = 18; parameter.Scale = 2; }
        }

        public static async Task<DbTransaction> BeginTransactionAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            // SQL Server uses SqlTransaction; the alternative provider is used by isolated tests.
            if (connection is SqlConnection sqlConnection)
            {
                // [Tiêu chí 35] Giao dịch SQL Server thực sự là SqlTransaction; CheckoutService dùng nó cho hóa đơn, tồn kho và điểm thành viên.
                SqlTransaction transaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync(cancellationToken);
                return transaction;
            }
            return await connection.BeginTransactionAsync(cancellationToken);
        }

        public sealed class ConnectionLease : IAsyncDisposable
        {
            public DbConnection Connection { get; }
            private readonly bool _opened;
            public ConnectionLease(DbConnection connection, bool opened) { Connection = connection; _opened = opened; }
            public async ValueTask DisposeAsync() { if (_opened) await Connection.CloseAsync(); }
        }
    }
}
