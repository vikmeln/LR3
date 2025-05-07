using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace ConsoleApp1
{
    class DBUtils
    {
        public static MySqlConnection GetDBConnection()
        {
            string host = "localhost";
            int port = 3306;
            string database = "Commercial hospital";
            string username = "monty";
            string password = "some_pass";
            return DBMySQLUtils.GetDBConnection(host, port, database, username, password);
        }
    }
}
