using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public enum CardType {
        Attack,
        Skill,
        Power,
        Curse,
        Status,
    }
    public enum Tags {
        NonPermanent,
        CardDraw,
    }
    public enum Zone {
        Hand,
        Discard,
        Draw,
        Exhaust,
        Exhume,
        Power,
    }
    public class Card {
        public string id;
        public int misc;
        public int upgrades;

        public string name;
        public string type;
        public string cost;
        public string description;
        public float bias;
        public float upgradeBias;

        public Dictionary<string, float> tags;
        public CardType cardType;
        public int intCost = -1;
        public Zone zone;

        public void CopyFrom(Card other) {
            this.name = other.name;
            this.type = other.type;
            this.cost = other.cost;
            this.description = other.description;
            this.zone = other.zone;
            this.bias = other.bias;
            this.tags = other.tags;
            this.upgradeBias = other.upgradeBias;
        }

        public void OnLoad() {
            if (tags == null) {
                tags = new Dictionary<string, float>();
            }
            var damageRegex = new Regex(@"Deal (\d+) (\((\d+)\) )?damage");
            var damageMatch = damageRegex.Match(description);
            if (damageMatch.Success) {
                tags["damage"] = float.Parse(damageMatch.Groups[1].Value);
            }
            int.TryParse(cost, out intCost);

            switch (type) {
                case "Attack": {
                    cardType = CardType.Attack;
                    break;
                }
                case "Skill": {
                    cardType = CardType.Skill;
                    break;
                }
                case "Power": {
                    cardType = CardType.Power;
                    break;
                }
                case "Curse": {
                    cardType = CardType.Curse;
                    break;
                }
            }
        }
    }
    public enum NodeType {
        Question,
        Fight,
        Elite,
        MegaElite,
        Shop,
        Fire,
        Chest,
    }
    public class MapNode {
        public NodeType nodeType;
        public List<MapNode> children = new List<MapNode>();
        public Vector2Int position;
    }
    public enum PlayerCharacter {
        Ironclad,
        Silent,
        Defect,
        Watcher,
    }
    public class DamageTaken {
        public float damage;
        public string enemies;
        public float floor;
        public float turns;
    }
    public class PumpernickelSaveState {
        public static PumpernickelSaveState instance;

        public List<Card> cards = new List<Card>();
        public string path = null;
        public long seed;
        public string boss = null;
        public int room_x;
        public int room_y;
        public int gold;
        public int purgeCost;
        public int act_num;
        public float[] event_chances;
        public string[] elite_monster_list;
        public int elites1_killed;
        public int elites2_killed;
        public int elites3_killed;
        public int monsters_killed;
        public int current_health;
        public int max_health;
        public int floor_num;
        public List<string> relics = new List<string>();
        public string[] potions;
        public bool has_emerald_key;
        public bool has_ruby_key;
        public bool has_sapphire_key;
        public List<DamageTaken> metric_damage_taken;

        public PlayerCharacter character;
        public MapNode[,,] map = new MapNode[4, 7, 15];
        public float infiniteBlockPerCard;
        public int infiniteRoom;
        public int earliestInfinite;
        public bool buildingInfinite;
        public int missingCards;
        public bool expectingToRedBlue;

        public PumpernickelSaveState() {
            instance = this;
        }

        public int AddCardById(string name) {
            var upgrades = name.EndsWith("+") ? 1 : 0;
            var card = new Card() {
                id = name,
                upgrades = upgrades,
                misc = 0
            };
            card.CopyFrom(Database.instance.cardsDict[card.id]);
            card.OnLoad();
            cards.Add(card);
            return cards.Count - 1;
        }
        public void RemoveCardByIndex(int index) {
            cards.RemoveAt(index);
        }
        protected void ParseNodes(int act, int y, string line) {
            for (int x = 0; x < 7; x++) {
                var c = line[x * 3];
                switch (c) {
                    case ' ': {
                        map[act, x, y] = null;
                        break;
                    }
                    case 'R': {
                        map[act, x, y] = new MapNode() { nodeType = NodeType.Fire, position = new Vector2Int(x, y) };
                        break;
                    }
                    case 'M': {
                        map[act, x, y] = new MapNode() { nodeType = NodeType.Fight, position = new Vector2Int(x, y) };
                        break;
                    }
                    case '?': {
                        map[act, x, y] = new MapNode() { nodeType = NodeType.Question, position = new Vector2Int(x, y) };
                        break;
                    }
                    case 'E': {
                        map[act, x, y] = new MapNode() { nodeType = NodeType.Elite, position = new Vector2Int(x, y) };
                        break;
                    }
                    case 'T': {
                        map[act, x, y] = new MapNode() { nodeType = NodeType.Chest, position = new Vector2Int(x, y) };
                        break;
                    }
                    case '$': {
                        map[act, x, y] = new MapNode() { nodeType = NodeType.Shop, position = new Vector2Int(x, y) };
                        break;
                    }
                }
            }
        }
        protected void ParseConnections(int act, int toY, string line) {
            int targetY = toY - 1;
            for (int i = 0; i < line.Length; i++) {
                var targetX = (i + 1) / 3;
                switch (line[i]) {
                    case '\\': {
                        map[act, targetX, targetY].children.Add(map[act, targetX - 1, toY]);
                        break;
                    }
                    case '|': {
                        map[act, targetX, targetY].children.Add(map[act, targetX + 0, toY]);
                        break;
                    }
                    case '/': {
                        map[act, targetX, targetY].children.Add(map[act, targetX + 1, toY]);
                        break;
                    }
                }
            }
        }
        protected void ParseActMap(string[] lines, int act) {
            for (int row = 0; row < 15; row++) {
                var line = lines[row * 2].Substring(7);
                ParseNodes(act, 14 - row, line);
            }
            for (int row = 0; row < 14; row++) {
                var line = lines[row * 2 + 1].Substring(7);
                ParseConnections(act, 14 - row, line);
            }
        }
        public void ParsePath(string path) {
            var pathLines = path.Split(new char[] {'\n'});
            var actLines = new string[][] {
                pathLines[5..34],
                pathLines[39..68],
                pathLines[73..102],
            };
            ParseActMap(actLines[0], 1);
            ParseActMap(actLines[1], 2);
            ParseActMap(actLines[2], 3);
            var pathTexts = actLines.Select(actLines => {
                var lines = actLines.Select(x => x.Substring(7)).ToArray();
                return string.Join("\n", lines);
            }).ToArray();
            PumpernickelAdviceWindow.instance.Invoke(PumpernickelAdviceWindow.SetPathTexts, new object[] { pathTexts });
        }

        public void OnLoad() {
            foreach (var card in cards) {
                card.CopyFrom(Database.instance.cardsDict[card.id]);
                card.OnLoad();
            }
        }

        public MapNode GetCurrentNode() {
            if (room_x < 0) {
                if (room_y < 0) {
                    var startNode = new MapNode();
                    startNode.children = Enumerable.Range(0, map.GetLength(0)).Select(x => map[Save.state.act_num, x, 0]).Where(x => x != null).ToList();
                    return startNode;
                }
                return null;
            }
            var CurrentNode = map[Save.state.act_num, room_x, room_y];
            // If we're talking to neow, make up a fake node with all the starting nodes as children
            return CurrentNode;
        }

        public int TakePotion(string potion) {
            for (int i = 0; i < potions.Length; i++) {
                if (potions[i].Equals("Potion Slot")) {
                    potions[i] = potion;
                    return i;
                }
            }
            return -1;
        }
        public void RemovePotion(int potionIndex) {
            potions[potionIndex] = "Potion Slot";
        }
    }
}
