namespace Capstone.EntityFrameworkCore.Seed;

public static class SeedOptions
{
        public static bool SeedSimpleSampleData { get; set; }

        public static bool SeedLogbookSampleData { get; set; }

        public static string SystemAdminInitialPassword { get; set; }

        public const string DevelopmentPassword = "Fpms@2026";
}
