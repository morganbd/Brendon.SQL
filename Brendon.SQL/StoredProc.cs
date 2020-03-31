using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace Brendon.SQL
{
    public class StoredProc
    {
        #region Declartions

        private string _connString = "";
        private string _spName = "";

        public Dictionary<string, object> Params { get; set; }

        #endregion

        #region Constructors 

        public StoredProc(string sConnString, string sSPName)
        {
            _connString = sConnString;
            _spName = sSPName;

            this.Params = new Dictionary<string, object>();
        }

        #endregion

        #region Public Functions

        public void ExecuteQuery()
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                using (SqlCommand sqlComm = new SqlCommand(_spName, conn))
                {
                    LoadParams(sqlComm);

                    sqlComm.CommandType = CommandType.StoredProcedure;

                    conn.Open();

                    sqlComm.ExecuteNonQuery();

                    conn.Close();
                }
            }
        }

        /// <summary>
        /// Translate SQL results into enumerable list of specified object type.
        /// </summary>
        /// <typeparam name="T">Result Type</typeparam>
        /// <returns>Enumerable List</returns>
        public IEnumerable<T> ExecuteQuery<T>()
        {
            Type type = typeof(T);
            List<T> list = new List<T>();

            using (SqlConnection conn = new SqlConnection(_connString))
            {
                using (SqlCommand sqlComm = new SqlCommand(_spName, conn))
                {
                    LoadParams(sqlComm);

                    sqlComm.CommandType = CommandType.StoredProcedure;

                    conn.Open();

                    using (SqlDataReader reader = sqlComm.ExecuteReader())
                    {
                        List<String> columns = GetColumnNames(reader);

                        while (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                T item = (T)Activator.CreateInstance(typeof(T));

                                for (int i = 0; i < type.GetProperties().Count(); i++)
                                {
                                    PropertyInfo prop = type.GetProperties()[i];

                                    //Validate property name exists in result set.
                                    if (!columns.Contains(prop.Name)) continue;

                                    //Validate value isn't null.
                                    if (reader.GetValue(reader.GetOrdinal(prop.Name)) == DBNull.Value) continue;

                                    prop.SetValue(item, reader.GetValue(reader.GetOrdinal(prop.Name)));
                                }

                                list.Add(item);
                            }

                            reader.NextResult();
                        }
                    }

                    conn.Close();
                }
            }

            return list;
        }

        /// <summary>
        /// Translate SQL result into object of specified type.
        /// </summary>
        /// <typeparam name="T">Result Type</typeparam>
        /// <returns>Object</returns>
        public T ExecuteSingle<T>()
        {
            T item = (T)Activator.CreateInstance(typeof(T));
            Type type = typeof(T);

            using (SqlConnection conn = new SqlConnection(_connString))
            {
                using (SqlCommand sqlComm = new SqlCommand(_spName, conn))
                {
                    LoadParams(sqlComm);

                    sqlComm.CommandType = CommandType.StoredProcedure;

                    conn.Open();

                    using (SqlDataReader reader = sqlComm.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        List<String> columns = GetColumnNames(reader);

                        while (reader.Read())
                        {
                            for (int i = 0; i < type.GetProperties().Count(); i++)
                            {
                                PropertyInfo prop = type.GetProperties()[i];

                                //Validate property name exists in result set.
                                if (!columns.Contains(prop.Name)) continue;

                                //Validate value isn't null.
                                if (reader.GetValue(reader.GetOrdinal(prop.Name)) == DBNull.Value) continue;

                                prop.SetValue(item, reader.GetValue(reader.GetOrdinal(prop.Name)));
                            }
                        }
                    }

                    conn.Close();
                }
            }

            return item;
        }

        /// <summary>
        /// Returns scalar result from a SQL query.
        /// </summary>
        /// <typeparam name="T">Result Type</typeparam>
        /// <returns>Object</returns>
        public T ExecuteScalar<T>()
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                SqlCommand sqlComm = new SqlCommand(_spName, conn);

                LoadParams(sqlComm);

                sqlComm.CommandType = CommandType.StoredProcedure;

                return (T)sqlComm.ExecuteScalar();
            }
        }

        #endregion

        #region Private Functions

        private void LoadParams(SqlCommand sqlComm)
        {
            if (this.Params == null) return;

            if (this.Params.Count > 0)
            {
                foreach (KeyValuePair<string, object> item in this.Params)
                {
                    sqlComm.Parameters.AddWithValue(item.Key, item.Value);
                }
            }
        }

        private List<String> GetColumnNames(SqlDataReader reader)
        {
            List<String> columns = new List<String>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            return columns;
        }

        #endregion

    }
}
