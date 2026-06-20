namespace BokutachiToOpenLR2ScoreImporter
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            Console.WriteLine("Selecting all.json file");
            var pbPath = SelectJsonFilePath();
            if (!File.Exists(pbPath))
            {
                Console.WriteLine("Failed to locate all.json file.");
                OnExit();
                return;
            }

            var json = File.ReadAllText(pbPath);
            if (string.IsNullOrEmpty(json))
            {
                Console.WriteLine("Failed to read all.json file.");
                OnExit();
                return;
            }

            var reader = new BokutachiPBReader();
            var scores = reader.LoadAllPBJson(json);
            if (scores == null || !scores.Any())
            {
                Console.WriteLine("No scores found in all.json file.");
                OnExit();
                return;
            }

            Console.WriteLine("Selecting player.db file");
            var playerDBPath = SelectDatabasePath();
            if (string.IsNullOrEmpty(playerDBPath))
            {
                Console.WriteLine("Failed to locate player.db file.");
                OnExit();
                return;
            }

            Console.WriteLine($"player.db path : {playerDBPath}");
            var aggregator = new PlayerDBAggregator();
            aggregator.AggregatePlayerDB(playerDBPath, scores);

            OnExit();
        }

        private static string? SelectJsonFilePath()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.Title = "Select your bokutachi IR's all.json file";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private static string? SelectDatabasePath()
        {
            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SQLite Database files (*.db)|*.db|All files (*.*)|*.*";
            openFileDialog.Title = "Select your player.db file";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private static void OnExit()
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }

    }
}