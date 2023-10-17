using Newtonsoft.Json;
using ProjectPumpernickle;
using System.Diagnostics;
using System.Text;

namespace ProjectPumpernickle {
    internal static class Program {
        internal static PumpernickelAdviceWindow? mainWindow;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            ApplicationConfiguration.Initialize();
            mainWindow = new PumpernickelAdviceWindow();

            ParseDatabase();

            var watcher = new FileSystemWatcher();
            watcher.Path = @"C:\Program Files (x86)\Steam\steamapps\common\SlayTheSpire\saves";
            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;

            var lastWritten = Directory.GetFiles(watcher.Path).Where(x => x.EndsWith(".autosave") || x.EndsWith(".autosaveBETA")).OrderBy(x => File.GetLastWriteTime(x)).Last();
            if (lastWritten != null) {
                ParseFile(lastWritten);
            }

            TcpListener.control = mainWindow;
            var pipeListener = new Thread(new ThreadStart(TcpListener.Run));
            pipeListener.Start();

            Application.Run(mainWindow);
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
        private static void ParseFile(string filename) {
            if (filename.EndsWith(".vdf") || filename.EndsWith("backUp")) {
                return;
            }
            var readText = File.ReadAllTextAsync(filename);
            readText.Wait();
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

            var psi = new ProcessStartInfo("sts_map_oracle.exe", "--seed " + save!.seed);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            var proc = Process.Start(psi);

            var psr = proc!.StandardOutput;
            PumpernickelSaveState.instance.ParsePath(psr.ReadToEnd());

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
    public static PumpernickelSaveState state => PumpernickelSaveState.instance;
}