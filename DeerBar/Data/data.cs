using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dark.Modules.Data
{
    public class Database
    {
        private string table { get; set; }
        private const string server = "127.0.0.1";
        private const string database = "telegram";
        private const string username = "root";
        private const string password = ""; // Put the database password here
        private MySqlConnection dbConnection;

        public Database(string table)
        {
            this.table = table;
            MySqlConnectionStringBuilder stringBuilder = new MySqlConnectionStringBuilder();
            stringBuilder.Server = server;
            stringBuilder.UserID = username;
            stringBuilder.Password = password;
            stringBuilder.Database = database;
            stringBuilder.SslMode = MySqlSslMode.None;

            var connectionString = stringBuilder.ToString();
            dbConnection = new MySqlConnection(connectionString);
            dbConnection.Open();
        }

        public MySqlDataReader FireCommand(string query)
        {
            if (dbConnection == null)
            {
                return null;
            }

            MySqlCommand command = new MySqlCommand(query, dbConnection);

            var mySqlReader = command.ExecuteReader();

            return mySqlReader;
        }

        public void CloseConnection()
        {
            if (dbConnection != null)
            {
                dbConnection.Close();
            }
        }

        public static string EnterUser(int teleid)
        {
            var database = new Database("dragonscale");

            var str = string.Format("INSERT INTO `currency` (t_id) VALUES ('{0}')", teleid);
            var table = database.FireCommand(str);

            database.CloseConnection();

            return null;
        }

        public static string DelMsg(long chatid, int messageid)
        {
            var database = new Database("dragonscale");

            var str = string.Format("INSERT INTO `del` (chat_id, message_id) VALUES ('{0}', '{1}')", chatid, messageid);
            var table = database.FireCommand(str);

            database.CloseConnection();

            return null;
        }

        public static string DelDelMsg(long chatid)
        {
            var database = new Database("dragonscale");

            var str = string.Format("DELETE FROM `del` WHERE chat_id = '{0}'", chatid);
            var table = database.FireCommand(str);

            database.CloseConnection();

            return null;
        }


        public static string AddCoins(int teleid, int coins)
        {
            var database = new Database("dragonscale");

            var str = string.Format("UPDATE `currency` SET coins = coins + '{1}' WHERE t_id = '{0}'", teleid, coins);
            var table = database.FireCommand(str);

            database.CloseConnection();

            return null;
        }

        public static string RemoveCoins(int teleid, int coins)
        {
            var database = new Database("dragonscale");

            var str = string.Format("UPDATE `currency` SET coins = coins - '{1}' WHERE t_id = '{0}'", teleid, coins);
            var table = database.FireCommand(str);

            database.CloseConnection();

            return null;
        }

        public static string UpdateDaily(int teleid, int day)
        {
            var database = new Database("dragonscale");

            var str = string.Format("UPDATE `currency` SET claimed_daily = '{1}' WHERE t_id = '{0}'", teleid, day);
            var table = database.FireCommand(str);

            database.CloseConnection();

            return null;
        }

        public static List<coins> GetCoins(int telegramid)
        {
            var result = new List<coins>();

            var database = new Database("telegram");

            var str = string.Format("SELECT * FROM `currency` WHERE t_id = '{0}'", telegramid);
            var tableName = database.FireCommand(str);

            while (tableName.Read())
            {
               
                var Coins = (long)tableName["coins"];
                var Daily = (int)tableName["claimed_daily"];
                
                result.Add(new coins
                {
                    
                    Coins = Coins,
                    Daily = Daily,
                });
            }
            database.CloseConnection();

            return result;
        }

        public static List<delete> GetMessageID(long chatid)
        {
            var result = new List<delete>();

            var database = new Database("telegram");

            var str = string.Format("SELECT * FROM `del` WHERE chat_id = '{0}'", chatid);
            var tableName = database.FireCommand(str);

            while (tableName.Read())
            {

                var CID = (long)tableName["chat_id"];
                var MID = (long)tableName["message_id"];

                result.Add(new delete
                {

                    CID = CID,
                    MID = MID,
                });
            }
            database.CloseConnection();

            return result;
        }


    }
}
