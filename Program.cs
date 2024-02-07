using Newtonsoft.Json;
using ProjectPumpernickle;
using System.Diagnostics;
using System.Text;

namespace ProjectPumpernickle {
    internal static class Program {
        public static float LastUpdatedTime;
        internal static PumpernickelAdviceWindow? mainWindow;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            ApplicationConfiguration.Initialize();
            mainWindow = new PumpernickelAdviceWindow();

            Application.Run(mainWindow);
        }
        public static void OnStartup() {
            ParseDatabase();

            TcpListener.control = mainWindow;
            var pipeListener = new Thread(new ThreadStart(TcpListener.Run));
            pipeListener.Start();
        }
        public static void ParseNewFile(int floorNum, bool expectFightOver) {
            var path = @"C:\Program Files (x86)\Steam\steamapps\common\SlayTheSpire\saves";
            var lastWritten = Directory.GetFiles(path).Where(x => x.EndsWith(".autosave") || x.EndsWith(".autosaveBETA")).OrderBy(x => File.GetLastWriteTime(x)).Last();
            var writtenTime = File.GetLastWriteTime(lastWritten);
            ParseFile(lastWritten);
            var loadedFloorNum = PumpernickelSaveState.parsed.floor_num;
            var didFightThisFloor = PumpernickelSaveState.parsed.metric_damage_taken.Any(x => x.floor == floorNum);
            if (loadedFloorNum != floorNum || (expectFightOver && !didFightThisFloor)) {
                var waitStartTime = DateTime.Now;
                while (true) {
                    var newWriteTime = File.GetLastWriteTime(lastWritten);
                    if (newWriteTime != writtenTime) {
                        break;
                    }
                    var currentTime = DateTime.Now;
                    var deltaTime = currentTime - waitStartTime;
                    if (deltaTime > TimeSpan.FromSeconds(10)) {
                        throw new Exception("File wasn't updated within 10 seconds, when we expected it to be");
                    }
                    Thread.Sleep(10);
                }
                ParseFile(lastWritten);
            }
        }
        private static void ParseDatabase() {
            using (StreamReader sr = new StreamReader("data.json")) {
                Database.instance = JsonConvert.DeserializeObject<Database>(sr.ReadToEnd());
                Database.instance.OnLoad();
            }
        }
        private static void OnChanged(object sender, FileSystemEventArgs e) {
            if (e.ChangeType != WatcherChangeTypes.Changed) {
                return;
            }
            ParseFile(e.FullPath);
        }

        private static void OnCreated(object sender, FileSystemEventArgs e) {
            ParseFile(e.FullPath);
        }
        public static void ParseLatestFile() {
            var path = @"C:\Program Files (x86)\Steam\steamapps\common\SlayTheSpire\saves";
            var lastWritten = Directory.GetFiles(path).Where(x => x.EndsWith(".autosave") || x.EndsWith(".autosaveBETA")).OrderBy(x => File.GetLastWriteTime(x)).Last();
            ParseFile(lastWritten);
        }
        private static void ParseFile(string filename) {
            if (filename.EndsWith(".vdf") || filename.EndsWith("backUp")) {
                return;
            }
            Task<string> readText = null;
            for (int i = 0; i < 100; i++) {
                try {
                    readText = File.ReadAllTextAsync(filename);
                    readText.Wait();
                    break;
                }
                catch {
                    Thread.Sleep(10);
                }
                if (i == 99) {
                    throw new Exception("Can't read save file");
                }
            }
            PumpernickelSaveState save = null;
            var jsonText = readText.Result;
            if (!filename.EndsWith("BETA")) {
                var saveBytes = Convert.FromBase64String(jsonText);
                var keyBytes = Encoding.UTF8.GetBytes("key");
                for (int i = 0; i < saveBytes.Length; i++) {
                    saveBytes[i] ^= keyBytes[i % keyBytes.Length];
                }
                jsonText = Encoding.UTF8.GetString(saveBytes);
            }
            try {
                save = JsonConvert.DeserializeObject<PumpernickelSaveState>(jsonText);
            }
            catch {
                return;
            }

            if (filename.Contains("WATCHER")) {
                save.character = PlayerCharacter.Watcher;
            }
            else if (filename.Contains("THE_SILENT")) {
                save.character = PlayerCharacter.Silent;
            }
            else if (filename.Contains("IRONCLAD")) {
                save.character = PlayerCharacter.Ironclad;
            }
            else if (filename.Contains("DEFECT")) {
                save.character = PlayerCharacter.Defect;
            }

            save.OnLoad();
            GenerateMap();
        }

        public static void GenerateMap() {
            var psi = new ProcessStartInfo("sts_map_oracle.exe", "--seed " + PumpernickelSaveState.parsed!.seed);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            var proc = Process.Start(psi);

            var psr = proc!.StandardOutput;
            PumpernickelSaveState.parsed.ParsePath(psr.ReadToEnd());

            proc.WaitForExit();
        }
    }
}

public static class ExceptExtension {
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, Func<T, bool> predicate) {
        foreach (var item in source) {
            if (!predicate(item)) {
                yield return item;
            }
        }
    }
}

public static class Save {
    public static PumpernickelSaveState state {
        get {
            if (PumpernickelSaveState.instance == null) {
                return PumpernickelSaveState.parsed;
            }
            else {
                return PumpernickelSaveState.instance;
            }
        }
    }
}