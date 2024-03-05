using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal static class Profiling {
        private static readonly string WORK = "WORK";
        private static double mainThreadTime;
        [ThreadStatic]
        private static Dictionary<string, Stopwatch> workerTimings;
        private static Dictionary<string, double> aggregateTimings;
        private static Dictionary<string, Stopwatch> mainThreadTimings;
        [ThreadStatic]
        private static bool isWorker;
        private static long numWorkers;

        public static void MainThreadStart(long numWorkers) {
            aggregateTimings = new Dictionary<string, double>();
            mainThreadTimings = new Dictionary<string, Stopwatch>();
            isWorker = false;
            Profiling.numWorkers = numWorkers;
            StartZone(WORK);
        }
        public static void WorkerStart() {
            workerTimings = new Dictionary<string, Stopwatch>();
            isWorker = true;
            StartZone(WORK);
        }
        public static void StartZone(string zone) {
            if (isWorker) {
                if (workerTimings.TryGetValue(zone, out var stopwatch)) {
                    stopwatch.Start();
                }
                else {
                    workerTimings[zone] = Stopwatch.StartNew();
                }
            }
            else {
                if (mainThreadTimings.TryGetValue(zone, out var stopwatch)) {
                    stopwatch.Start();
                }
                else {
                    mainThreadTimings[zone] = Stopwatch.StartNew();
                }
            }
        }
        public static void StopZone(string zone) {
            if (isWorker) {
                workerTimings[zone].Stop();
            }
            else {
                mainThreadTimings[zone].Stop();
            }
        }
        public static void StopWork() {
            StopZone(WORK);
            if (isWorker) {
                lock (aggregateTimings) {
                    foreach (var timing in workerTimings) {
                        aggregateTimings.TryGetValue(timing.Key, out var original);
                        aggregateTimings[timing.Key] = original + timing.Value.Elapsed.TotalSeconds;
                    }
                }
            }
            else {
                var averageWorkerTime = aggregateTimings[WORK] / numWorkers;
                var workerZoneFractions = aggregateTimings.Select(x => (x.Key, Average: x.Value / numWorkers / averageWorkerTime)).ToDictionary(x => x.Key, x => x.Average);
                var mainThreadZoneTime = mainThreadTimings.Select(x => (x.Key, Total: x.Value.Elapsed.TotalSeconds)).ToDictionary(x => x.Key, x => x.Total);
                Console.WriteLine();
            }
        }
    }
}
