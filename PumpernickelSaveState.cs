using System;
using System.Collections.Generic;
using System.Linq;
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
    }
    public struct Card {
        public string id;
        public int misc;
        public int upgrades;

        public string name;
        public string type;
        public string cost;
        public string description;

        public Dictionary<string, float> tags;
        public CardType cardType;
        public void CopyFrom(Card other) {
            this.name = other.name;
            this.type = other.type;
            this.cost = other.cost;
            this.description = other.description;
        }

        public void OnLoad() {
            if (tags == null) {
                tags = new Dictionary<string, float>();
            }
            var damageRegex = new Regex(@"Deal (\d+) (\((\d+)\) )?damage");
            var damageMatch = damageRegex.Match(description);
            if (damageMatch.Success) {
                if (upgrades > 0) {
                    tags["damage"] = float.Parse(damageMatch.Groups[3].Value);
                }
                else {
                    tags["damage"] = float.Parse(damageMatch.Groups[1].Value);
                }
            }

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
    }
    public enum Character {
        Ironclad,
        Silent,
        Defect,
        Watcher,
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

        public Character character;

        public MapNode[,] map = new MapNode[7, 15];

        public PumpernickelSaveState() {
            instance = this;
        }

        public int AddCardByName(string name) {
            var upgrades = name.EndsWith("+") ? 1 : 0;
            var card = new Card() {
                id = name,
                upgrades = upgrades,
                misc = 0
            };
            cards.Add(card);
            return cards.Count - 1;
        }
        public void RemoveCardByIndex(int index) {
            cards.RemoveAt(index);
        }
        protected void ParseNodes(int y, string line) {
            for (int x = 0; x < 7; x++) {
                var c = line[x * 3];
                switch (c) {
                    case ' ': {
                        map[x, y] = null;
                        break;
                    }
                    case 'R': {
                        map[x, y] = new MapNode() { nodeType = NodeType.Fire };
                        break;
                    }
                    case 'M': {
                        map[x, y] = new MapNode() { nodeType = NodeType.Fight };
                        break;
                    }
                    case '?': {
                        map[x, y] = new MapNode() { nodeType = NodeType.Question };
                        break;
                    }
                    case 'E': {
                        map[x, y] = new MapNode() { nodeType = NodeType.Elite };
                        break;
                    }
                    case 'T': {
                        map[x, y] = new MapNode() { nodeType = NodeType.Chest };
                        break;
                    }
                    case '$': {
                        map[x, y] = new MapNode() { nodeType = NodeType.Shop };
                        break;
                    }
                }
            }
        }
        protected void ParseConnections(int toY, string line) {
            int targetY = toY - 1;
            for (int i = 0; i < line.Length; i++) {
                var targetX = (i + 1) / 3;
                switch (line[i]) {
                    case '\\': {
                        map[targetX, targetY].children.Add(map[targetX - 1, toY]);
                        break;
                    }
                    case '|': {
                        map[targetX, targetY].children.Add(map[targetX + 0, toY]);
                        break;
                    }
                    case '/': {
                        map[targetX, targetY].children.Add(map[targetX + 1, toY]);
                        break;
                    }
                }
            }
        }
        protected void ParseActMap(string[] lines, int act) {
            if (act_num != act) {
                return;
            }
            for (int row = 0; row < 15; row++) {
                var line = lines[row * 2].Substring(7);
                ParseNodes(14 - row, line);
            }
            for (int row = 0; row < 14; row++) {
                var line = lines[row * 2 + 1].Substring(7);
                ParseConnections(14 - row, line);
            }
        }
        public void ParsePath(string path) {
            var pathLines = path.Split(new char[] {'\n'});
            ParseActMap(pathLines[5..34], 1);
            ParseActMap(pathLines[39..67], 2);
            ParseActMap(pathLines[73..101], 3);
        }

        public void OnLoad() {
            foreach (var card in cards) {
                card.CopyFrom(Database.instance.cardsDict[card.id]);
                card.OnLoad();
            }
        }

        public MapNode GetCurrentNode() {
            var CurrentNode = map[room_x, room_y];
            // If we're talking to neow, make up a fake node with all the starting nodes as children
            return CurrentNode;
        }
    }
}
