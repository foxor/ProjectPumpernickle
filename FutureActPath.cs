using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ProjectPumpernickle {
    internal class FutureActPath {
        public static NodeType ExpectedNode(int floorNum) {
            var expectedAct = Evaluators.FloorToAct(floorNum);
            var actBeginning = Evaluators.ActToFirstFloor(expectedAct);
            var floorsIntoAct = floorNum - actBeginning;
            return expectedAct switch {
                1 => Act1(floorsIntoAct),
                2 => Act2(floorsIntoAct),
                3 => Act3(floorsIntoAct),
                4 => Act4(floorsIntoAct),
            };
        }

        public static NodeType EstimateNodeType(int i, float endOfActGold) {
            // There are 12 unknown nodes per act
            // A typical act has:
            // - 1-3 elites
            // - 2 fires
            // - 0-2 shops
            // - 2-5 hallway fights
            // - Rest ? marks
            var floor = Save.state.floor_num + i + 1;
            var act = Evaluators.FloorToAct(floor);
            var floorIntoAct = Evaluators.FloorsIntoAct(floor);
            var overallPower = Evaluators.EstimateOverallPower();
            var elites = Evaluators.DesiredElites(overallPower, act);
            var shops = Evaluators.DesiredShops(endOfActGold);
            var fights = Evaluators.DesiredFights(overallPower);
            var wantsEarlyShop = shops > 0 && Evaluators.WantsEarlyShop(endOfActGold);
            var wantsLateShop = shops == 2 || (shops > 0 & !wantsEarlyShop);
            if (act == 3 && Save.state.act_num == 1) {
                // Our projections of power and gold aren't accurate that far into the future
                wantsEarlyShop = true;
                wantsLateShop = false;
                elites = 3;
                fights = 5;
            }
            switch (floorIntoAct) {
                case 2: {
                    return wantsEarlyShop ? NodeType.Shop : NodeType.Question;
                }
                case 3: {
                    return NodeType.Fight;
                }
                case 4: {
                    return fights > 4 ? NodeType.Fight : NodeType.Question;
                }
                case 5: {
                    return NodeType.Fire;
                }
                case 6: {
                    return elites > 1 ? NodeType.Elite : NodeType.Fight;
                }
                case 7: {
                    return fights > 2 ? NodeType.Fight : NodeType.Question;
                }
                case 9: {
                    return fights > 3 ? NodeType.Fight : NodeType.Question;
                }
                case 10: {
                    return elites > 2 ? NodeType.Elite : NodeType.Question;
                }
                case 11: {
                    return wantsLateShop ? NodeType.Shop : NodeType.Fight;
                }
                case 12: {
                    return NodeType.Fire;
                }
                case 13: {
                    return NodeType.Elite;
                }
                default: {
                    throw new NotImplementedException("Floor " + floorIntoAct + " not expected to be unknown");
                }
            }
        }

        protected static NodeType Act1(int localFloor) {
            switch (localFloor) {
                case 16: {
                    return NodeType.Boss;
                }
                case 17: {
                    return NodeType.BossChest;
                }
                default: {
                    throw new NotImplementedException();
                }
            }
        }

        protected static NodeType Act2(int localFloor) {
            switch (localFloor) {
                case 0: {
                    return NodeType.Fight;
                }
                case 1: {
                    // InitializePossibleThreats is using fight nodes to consume the easy pool,
                    // so we'll use those up early in acts 2 and 3
                    return NodeType.Fight;
                }
                case 2: {
                    return NodeType.Unknown;
                }
                case 3: {
                    return NodeType.Unknown;
                }
                case 4: {
                    return NodeType.Unknown;
                }
                case 5: {
                    return NodeType.Unknown;
                }
                case 6: {
                    return NodeType.Unknown;
                }
                case 7: {
                    return NodeType.Unknown;
                }
                case 8: {
                    return NodeType.Chest;
                }
                case 9: {
                    return NodeType.Unknown;
                }
                case 10: {
                    return NodeType.Unknown;
                }
                case 11: {
                    return NodeType.Unknown;
                }
                case 12: {
                    return NodeType.Unknown;
                }
                case 13: {
                    return NodeType.Unknown;
                }
                case 14: {
                    return NodeType.Fire;
                }
                case 15: {
                    return NodeType.Boss;
                }
                case 16: {
                    return NodeType.BossChest;
                }
                default: {
                    throw new NotImplementedException("Unexpected next act index: " + localFloor);
                }
            }
        }

        protected static NodeType Act3(int localFloor) {
            switch (localFloor) {
                case 0: {
                    return NodeType.Fight;
                }
                case 1: {
                    // InitializePossibleThreats is using fight nodes to consume the easy pool,
                    // so we'll use those up early in acts 2 and 3
                    return NodeType.Fight;
                }
                case 2: {
                    return NodeType.Unknown;
                }
                case 3: {
                    return NodeType.Unknown;
                }
                case 4: {
                    return NodeType.Unknown;
                }
                case 5: {
                    return NodeType.Unknown;
                }
                case 6: {
                    return NodeType.Unknown;
                }
                case 7: {
                    return NodeType.Unknown;
                }
                case 8: {
                    return NodeType.Chest;
                }
                case 9: {
                    return NodeType.Unknown;
                }
                case 10: {
                    return NodeType.Unknown;
                }
                case 11: {
                    return NodeType.Unknown;
                }
                case 12: {
                    return NodeType.Unknown;
                }
                case 13: {
                    return NodeType.Unknown;
                }
                case 14: {
                    return NodeType.Fire;
                }
                case 15: {
                    return NodeType.Boss;
                }
                case 16: {
                    return NodeType.BossChest;
                }
                default: {
                    throw new NotImplementedException("Unexpected next act index: " + localFloor);
                }
            }
        }

        protected static NodeType Act4(int localFloor) {
            switch (localFloor) {
                case 0: {
                    return NodeType.Fire;
                }
                case 1: {
                    return NodeType.Shop;
                }
                case 2: {
                    return NodeType.Elite;
                }
                case 3: {
                    return NodeType.Boss;
                }
                default: {
                    throw new NotImplementedException("Unexpected next act index: " + localFloor);
                }
            }
        }
    }
}
