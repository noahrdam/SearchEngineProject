using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Indexer;
    public class App
    {
        public void Run()
        {
            IDatabase db = GetDatabase();
            Crawler crawler = new Crawler(db);

            var root = new DirectoryInfo(Config.FOLDER);

            DateTime start = DateTime.Now;

            crawler.IndexFilesIn(root, new List<string> { ".txt"});        

            TimeSpan used = DateTime.Now - start;
            Console.WriteLine("DONE! used " + used.TotalMilliseconds);

            var all = db.GetAllWords();

            Console.WriteLine($"Indexed {db.DocumentCounts} documents");
            Console.WriteLine($"Number of different words: {all.Count}");
            int count = 10;
            Console.WriteLine($"The first {count} is:");
            foreach (var p in all.Take(count)) {
                Console.WriteLine("<" + p.Key + ", " + p.Value + ">");
            }
        }

        private IDatabase GetDatabase()
        {
            var cs2 = Shared.Paths.POSTGRES_DATABASE_2;
            if (!string.IsNullOrEmpty(cs2))
            {
                Console.Write("Use single Postgres (1) or two-shard Postgres (2)? ");
                if (Console.ReadLine()?.Trim() == "2")
                    return new ReceptionDatabase(new DatabasePostgres(), new DatabasePostgres(cs2));
            }
            return new DatabasePostgres();
        }
    }