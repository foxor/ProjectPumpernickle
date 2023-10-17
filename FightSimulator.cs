using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class FightSimulator {
        public static void SimulateFight(string encounterId, out float expectedHealthLoss, out float worstCaseHealthLoss) {
            var encounter = Database.instance.encounterDict[encounterId];
            if (encounter.pool.Equals("elite")) {
                expectedHealthLoss = 15;
                worstCaseHealthLoss = 30;
                return;
            }
            if (encounter.pool.Equals("hard")) {
                expectedHealthLoss = 10;
                worstCaseHealthLoss = 20;
                return;
            }
            expectedHealthLoss = 0;
            worstCaseHealthLoss = 0;
        }
    }
}
