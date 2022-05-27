using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RatexTestTask
{
    public class MainViewModel : ViewModel
    {
        public Command OpenConnectionCommand { get; set; }
        public Command TruncateCommand { get; set; }
        public Command InsertCommand { get; set; }
        public Command UpdateCommand { get; set; }

        public bool IsControlPanelEnabled
        {
            get
            {
                return _isControlPanelEnabled;
            }
            set
            {
                _isControlPanelEnabled = value;
                SetProperty(value);
            }
        }
        private bool _isControlPanelEnabled = false;

        public string ButtonContent
        {
            get
            {
                return _buttonContent;
            }
            set
            {
                _buttonContent = value;
                SetProperty(value);
            }
        }
        private string _buttonContent = "Start";

        public DataTable Table
        {
            get
            {
                return _table;
            }
            set
            {
                _table = value;
                SetProperty(value);
            }
        }
        private DataTable _table;
        private DBNotifier _notifier;
        public MainViewModel()
        {
            OpenConnectionCommand = new Command(OpenConnection);
            TruncateCommand = new Command(TruncateDB);
            InsertCommand = new Command(Insert1000Fields);
            UpdateCommand = new Command(ChangeFlag);
        }

        async void OpenConnection()
        {
            if (_notifier == null)
            {
                string connStr = Microsoft.VisualBasic.Interaction.InputBox("Connection string to DataBase", "DataBase");
                string table = Microsoft.VisualBasic.Interaction.InputBox("Table Name", "DataBase");
                _notifier = new DBNotifier(connStr, table);
            }
            if (!_notifier.IsConnected)
            {
                await _notifier.ConnectAsync();
                _notifier.OnChange += OnDbChanged;
            }
            else
            {
                await _notifier.DisconnectAsync();
                _notifier.OnChange -= OnDbChanged;
            }

            IsControlPanelEnabled = _notifier.IsConnected;
            ButtonContent = _notifier.IsConnected ? "Stop" : "Start";

        }
        private void TruncateDB()
        {
            Task.Run(() =>
            {
                using (var conn = new SqlConnection(_notifier.ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand($"TRUNCATE TABLE [dbo].[{_notifier.Table}]", conn);
                    cmd.ExecuteNonQuery();
                }
            }).ConfigureAwait(false);
        }
        private void Insert1000Fields()
        {
            Task.Run(() =>
            {
                using (var conn = new SqlConnection(_notifier.ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand($"DECLARE @i INT SET @i = 1 WHILE(@i <= 1000) BEGIN INSERT INTO[dbo].[{_notifier.Table}] DEFAULT VALUES SET @i = @i + 1 END", conn);
                    cmd.ExecuteNonQuery();
                }
            }).ConfigureAwait(false);
        }
        private void ChangeFlag()
        {
            Task.Run(() =>
            {
                using (var conn = new SqlConnection(_notifier.ConnectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand($"UPDATE[dbo].[{_notifier.Table}] SET Flag = ~Flag; ", conn);
                    cmd.ExecuteNonQuery();
                }
            }).ConfigureAwait(false);
        }

        public void OnDbChanged(object sender, DataTable table)
        {
            Table = table;
        }
    }
}
