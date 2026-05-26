using System;
using System.IO;

namespace Indexer;

public class Config
{
    public static string FOLDER = Path.GetFullPath(
        Environment.GetEnvironmentVariable("DATA_FOLDER")
        ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Data", "Data", "large"));
}