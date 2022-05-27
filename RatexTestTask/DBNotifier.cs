using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatexTestTask
{
    public class DBNotifier : IDisposable
    {
        public bool IsConnected { get; private set; }

        public string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            private set
            {
                _connectionString = value;
            }
        }
        private string _connectionString;

        public string Table
        {
            get
            {
                return _table;
            }
            private set
            {
                _table = value;
            }
        }
        private string _table;

        private SqlConnection _connection;
        private event EventHandler<DataTable> _onChange;
        private DataTable _dataTable;

        public event EventHandler<DataTable> OnChange
        {
            add
            {
                this._onChange += value;
                _onChange.Invoke(this, _dataTable);
            }
            remove
            {
                this._onChange -= value;
            }
        }

        public DBNotifier(string connectionString, string table)
        {
            _connectionString = connectionString;
            _table = table;
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            await Task.Run(() =>
            {
                try
                {
                    _connection = new SqlConnection(_connectionString);
                    _connection.Open();

                    SqlDependency.Stop(ConnectionString);
                    SqlDependency.Start(_connectionString);

                    IsConnected = true;

                    RegisterDependency();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }).ConfigureAwait(false);
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected) return;

            await Task.Run(() =>
            {
                try
                {
                    SqlDependency.Stop(_connectionString);
                    _connection.Close();
                    IsConnected = false;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }

        private void OnDependencyChange(object sender, SqlNotificationEventArgs e)
        {
            SqlDependency dependency = sender as SqlDependency;

            dependency.OnChange -= new OnChangeEventHandler(OnDependencyChange);
            if (_table != null && _onChange != null)
                _onChange.Invoke(this, _dataTable);

            RegisterDependency();
        }

        private void RegisterDependency()
        {
            try
            {
                using (SqlCommand command = new SqlCommand($"SELECT * FROM [dbo].[{_table}]", _connection))
                {
                    SqlDependency dependency = new SqlDependency(command);
                    dependency.OnChange += new
                       OnChangeEventHandler(OnDependencyChange);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        DataTable dt = new DataTable();
                        dt.Load(reader);
                        _dataTable = dt;
                    }
                }
            }
            catch 
            { 
            
            }
            
        }

        public void Dispose()
        {
            _connection.Close();
            SqlDependency.Stop(_connectionString);
        }
    }
}
