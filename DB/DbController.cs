using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Threading;

namespace BatchModelCheck.DB
{
    public static class DbController
    {
        public static void WriteValue(string document_id, string value)
        {
            try
            {
                Thread thread = new Thread(() =>
                {
                    bool exist = File.Exists(string.Format(@"Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite", document_id));
                    SQLiteConnection db = new SQLiteConnection(string.Format(@"Data Source=Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite;Version=3;", document_id));
                    try
                    {
                        if (!exist)
                        {
                            db.Open();
                            SQLiteCommand cmd_create = new SQLiteCommand("CREATE TABLE TB_CHECK (ID INTEGER PRIMARY KEY AUTOINCREMENT, DATETIME TEXT, DATA TEXT)", db);
                            cmd_create.ExecuteNonQuery();
                            db.Close();
                        }
                        db.Open();
                        SQLiteCommand cmd_insert = new SQLiteCommand("INSERT INTO TB_CHECK ([DATETIME], [DATA]) VALUES(@DATETIME, @DATA)", db);
                        cmd_insert.Parameters.Add(new SQLiteParameter() { ParameterName = "DATETIME", Value = DateTime.Now.ToString() });
                        cmd_insert.Parameters.Add(new SQLiteParameter() { ParameterName = "DATA", Value = value });
                        cmd_insert.ExecuteNonQuery();
                        db.Close();
                    }
                    catch (Exception)
                    {
                        db.Close();
                    }
                });
                thread.IsBackground = true;
                thread.Name = "sql_insert_data";
                thread.Start();
            }
            catch (Exception) { }
        }
        public static List<DbRowData> GetRows(string document_id)
        {
            List<DbRowData> values = new List<DbRowData>();
            if (File.Exists(string.Format(@"Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite", document_id)))
            {
                SQLiteConnection db = new SQLiteConnection(string.Format(@"Data Source=Z:\Отдел BIM\03_Скрипты\09_Модули_KPLN_Loader\DB\BatchModelCheck\doc_id_{0}.sqlite;Version=3;", document_id));
                try
                {
                    db.Open();
                    SQLiteCommand cmd_insert = new SQLiteCommand("SELECT * FROM TB_CHECK", db);
                    using (SQLiteDataReader rdr = cmd_insert.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            values.Add(new DbRowData(rdr.GetString(1), rdr.GetString(2)));
                        }
                    }
                    db.Close();
                }
                catch (Exception)
                {
                    db.Close();
                }
            }
            return values;
        }
    }
}
