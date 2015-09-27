﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TableDependency.Enums;
using TableDependency.EventArgs;
using TableDependency.Mappers;
using TableDependency.SqlClient.IntegrationTest.Helpers;
using TableDependency.SqlClient.IntegrationTest.Model;

namespace TableDependency.SqlClient.IntegrationTest
{
    [TestClass]
    public class EventForSpecificColumns
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
        private const string TableName = "Issue0000";
        private static int _counter;
        private static Dictionary<string, Tuple<Issue_0000_Model, Issue_0000_Model>> _checkValues = new Dictionary<string, Tuple<Issue_0000_Model, Issue_0000_Model>>();

        [TestInitialize]
        public void TestInitialize()
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText =
                        $"IF OBJECT_ID('{TableName}', 'U') IS NULL BEGIN CREATE TABLE [{TableName}]( " +
                        "[Id][int] IDENTITY(1, 1) NOT NULL, " +
                        "[First Name] [nvarchar](50) NOT NULL, " +
                        "[Second Name] [nvarchar](50) NOT NULL, " +
                        "[Born] [datetime] NULL); END";
                    sqlCommand.ExecuteNonQuery();

                    sqlCommand.CommandText = $"DELETE FROM [{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        [TestMethod]
        public void EventForSpecificColumnsTest()
        {
            SqlTableDependency<Issue_0000_Model> tableDependency = null;
            string naming = null;

            try
            {
                var mapper = new ModelToTableMapper<Issue_0000_Model>();
                mapper.AddMapping(c => c.Name, "FIRST name").AddMapping(c => c.Surname, "Second Name");

                tableDependency = new SqlTableDependency<Issue_0000_Model>(_connectionString, TableName, mapper, new List<string>() { "second name" });
                tableDependency.OnChanged += TableDependency_Changed;
                tableDependency.Start();
                naming = tableDependency.DataBaseObjectsNamingConvention;

                Thread.Sleep(5000);

                var t = new Task(ModifyTableContent);
                t.Start();
                t.Wait(20000);
            }
            finally
            {
                tableDependency?.Dispose();
            }

            Assert.AreEqual(_counter, 2);
            Assert.IsNull(_checkValues[ChangeType.Insert.ToString()].Item2.Name);
            Assert.AreEqual(_checkValues[ChangeType.Insert.ToString()].Item2.Surname, _checkValues[ChangeType.Insert.ToString()].Item1.Surname);
            Assert.IsNull(_checkValues[ChangeType.Delete.ToString()].Item2.Name);
            Assert.AreEqual(_checkValues[ChangeType.Delete.ToString()].Item2.Surname, _checkValues[ChangeType.Delete.ToString()].Item1.Surname);
            Assert.IsTrue(Helper.AreAllDbObjectDisposed(_connectionString, naming));
        }

        private static void TableDependency_Changed(object sender, RecordChangedEventArgs<Issue_0000_Model> e)
        {
            _counter++;

            switch (e.ChangeType)
            {
                case ChangeType.Insert:
                    _checkValues[ChangeType.Insert.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Insert.ToString()].Item2.Surname = e.Entity.Surname;
                    break;
                case ChangeType.Delete:
                    _checkValues[ChangeType.Delete.ToString()].Item2.Name = e.Entity.Name;
                    _checkValues[ChangeType.Delete.ToString()].Item2.Surname = e.Entity.Surname;
                    break;
            }
        }

        private static void ModifyTableContent()
        {
            _checkValues.Add(ChangeType.Insert.ToString(), new Tuple<Issue_0000_Model, Issue_0000_Model>(new Issue_0000_Model { Name = "Christian", Surname = "Del Bianco" }, new Issue_0000_Model()));
            _checkValues.Add(ChangeType.Update.ToString(), new Tuple<Issue_0000_Model, Issue_0000_Model>(new Issue_0000_Model { Name = "Velia" }, new Issue_0000_Model()));
            _checkValues.Add(ChangeType.Delete.ToString(), new Tuple<Issue_0000_Model, Issue_0000_Model>(new Issue_0000_Model { Name = "Velia", Surname = "Del Bianco" }, new Issue_0000_Model()));

            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = sqlConnection.CreateCommand())
                {
                    sqlCommand.CommandText = $"INSERT INTO [{TableName}] ([First Name], [Second Name]) VALUES ('{_checkValues[ChangeType.Insert.ToString()].Item1.Name}', '{_checkValues[ChangeType.Insert.ToString()].Item1.Surname}')";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(1000);

                    sqlCommand.CommandText = $"UPDATE [{TableName}] SET [First Name] = '{_checkValues[ChangeType.Update.ToString()].Item1.Name}'";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(1000);

                    sqlCommand.CommandText = $"DELETE FROM [{TableName}]";
                    sqlCommand.ExecuteNonQuery();
                    Thread.Sleep(1000);
                }
            }
        }
    }
}