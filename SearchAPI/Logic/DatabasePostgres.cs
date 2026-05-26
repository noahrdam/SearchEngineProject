using System.Collections.Generic;
using Shared;


namespace SearchAPI.Logic;
using Shared.Model;
using Npgsql;


public class DatabasePostgres : IDatabase
{
    private NpgsqlConnection _connection;
    private readonly string _connectionString;

        private Dictionary<string, int> mWords = null;

        public DatabasePostgres() : this(Paths.POSTGRES_DATABASE) { }

        public DatabasePostgres(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new NpgsqlConnection(connectionString);
            _connection.Open();
        }

        private void Execute(string sql)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }





        // key is the id of the document, the value is number of search words in the document
        public List<(int docId, int hits)> GetDocuments(List<int> wordIds, int maxAmount, int offset)
        {
            var res = new List<(int docId, int hits)>();
            var sql = "SELECT docId, COUNT(wordId) as count FROM Occ where ";
            sql += "wordId in " + AsString(wordIds) + " GROUP BY docId ";
            sql += $"ORDER BY count DESC LIMIT {maxAmount} OFFSET {offset};";

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                    res.Add((reader.GetInt32(0), reader.GetInt32(1)));
            }
            return res;
        }

        public int CountDocuments(List<int> wordIds)
        {
            var sql = "SELECT COUNT(DISTINCT docId) FROM Occ where wordId in " + AsString(wordIds);
            var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private string AsString(List<int> x) => $"({string.Join(',', x)})";


        public List<string> GetHits(int docId, List<int> wordIds)
        {
            if (wordIds.Count == 0)
                return new List<string>();

            var sql = "SELECT wordId FROM Occ WHERE ";
            sql += "wordId in " + AsString(wordIds) + " AND docId = " + docId;

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            var present = new List<int>();
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                    present.Add(reader.GetInt32(0));
            }

            return WordsFromIds(present);
        }

        public Dictionary<string, int> GetAllWords()
        {
            Dictionary<string, int> res = new Dictionary<string, int>();

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM word";

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var w = reader.GetString(1);

                    res.Add(w, id);
                }
            }
            return res;
        }

        public BEDocument GetDocDetails(int docId)
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = $"SELECT id, url, idxTime, creationTime FROM document where id = {docId}";

            using (var reader = selectCmd.ExecuteReader())
            {
                if (reader.Read())
                    return new BEDocument { Id = reader.GetInt32(0), Url = reader.GetString(1), IdxTime = reader.GetDateTime(2), CreationTime = reader.GetDateTime(3) };
            }
            return null;
        }

        public string? GetFileContent(int docId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT content FROM document WHERE id = @docId";
            cmd.Parameters.AddWithValue("docId", docId);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result as string;
        }

        /* Return a list of id's for words; all them among wordIds, but not present in the document
         */
        public List<string> GetMissing(int docId, List<int> wordIds)
        {
            var sql = "SELECT wordId FROM Occ where ";
            sql += "wordId in " + AsString(wordIds) + " AND docId = " + docId;
            sql += " ORDER BY wordId;";

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;

            List<int> present = new List<int>();

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var wordId = reader.GetInt32(0);
                    present.Add(wordId);
                }
            }
            var result = new List<int>(wordIds);
            foreach (var w in present)
                result.Remove(w);


            return WordsFromIds(result);
        }

        private List<string> WordsFromIds(List<int> wordIds)
        {
            List<string> result = new List<string>();

            if (wordIds.Count == 0)
                return result;
            var sql = "SELECT name FROM Word where ";
            sql += "id in " + AsString(wordIds);

            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = sql;
            
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var wordId = reader.GetString(0);
                    result.Add(wordId);
                }
            }
            return result;
        }

    
}