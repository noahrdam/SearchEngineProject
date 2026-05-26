using System;
namespace Shared;
    public class Paths
    {
        public static readonly string POSTGRES_DATABASE =
            Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ??
            "Server=127.0.0.1;Port=5433;User Id=postgres;Password=1234;Database=searchlarge";

        public static readonly string POSTGRES_DATABASE_2 =
            Environment.GetEnvironmentVariable("POSTGRES_DATABASE_2");

        public static readonly string? REDIS_URL =
            Environment.GetEnvironmentVariable("REDIS_URL");
    }

