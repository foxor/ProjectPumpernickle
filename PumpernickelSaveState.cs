using System.Text.RegularExpressions;

namespace ProjectPumpernickle {
    public enum CardType {
        Attack,
        Skill,
        Power,
        Curse,
        Status,
    }
    public enum Rarity {
        Common,
        Uncommon,
        Rare,
        Basic,
        Special,
        Randomable,
        Boss,
    }
    public enum Tags {
        NonPermanent,
        CardDraw,
        Damage,
        Block,
        BottleEquity,
        ExhaustCost,
        Unpurgeable,
    }
    public enum Color {
        Red,
        Green,
        Blue,
        Purple,
        Colorless,
        Curse,
        Any,
        COUNT,
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
        public float upgradePowerMultiplier;
        public float upgradeBias;
        public string rarity;
        public string color;
        public bool bottled;

        public Dictionary<string, float> tags;
        public CardType cardType;
        public int intCost = -1;
        public Rarity cardRarity;
        public Color cardColor;

        public void CopyFrom(Card other) {
            this.name = other.name;
            this.type = other.type;
            this.cost = other.cost;
            this.description = other.description;
            this.bias = other.bias;
            this.upgradePowerMultiplier = other.upgradePowerMultiplier;
            this.upgradeBias = other.upgradeBias;
            this.rarity = other.rarity;
            this.color = other.color;

            this.tags = other.tags;
            this.cardType = other.cardType;
            this.intCost = other.intCost;
            this.cardRarity = other.cardRarity;
            this.cardColor = other.cardColor;
        }

        public void OnLoad() {
            if (tags == null) {
                tags = new Dictionary<string, float>();
            }
            var damageRegex = new Regex(@"Deal (\d+) (\((\d+)\) )?damage");
            var damageMatch = damageRegex.Match(description);
            if (damageMatch.Success) {
                tags[Tags.Damage.ToString()] = float.Parse(damageMatch.Groups[upgrades == 0 ? 1 : 2].Value);
            }
            var blockRegex = new Regex(@"Gain (\d+) (\((\d+)\) )?Block");
            var blockMatch = blockRegex.Match(description);
            if (blockMatch.Success) {
                tags[Tags.Block.ToString()] = float.Parse(blockMatch.Groups[upgrades == 0 ? 1 : 2].Value);
            }
            var costRegex = new Regex(@"(\d+) ?(\((\d+)\) )?");
            var costMatch = costRegex.Match(cost);
            if (costMatch.Success) {
                intCost = int.Parse(costMatch.Groups[upgrades == 0 ? 1 : 2].Value);
            }
            else {
                intCost = int.MaxValue;
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
                    tags[Tags.NonPermanent.ToString()] = 1f;
                    break;
                }
                case "Curse": {
                    cardType = CardType.Curse;
                    break;
                }
            }

            switch (rarity) {
                case "Common": {
                    cardRarity = Rarity.Common;
                    break;
                }
                case "Uncommon": {
                    cardRarity = Rarity.Uncommon;
                    break;
                }
                case "Rare": {
                    cardRarity = Rarity.Rare;
                    break;
                }
                case "Basic": {
                    cardRarity = Rarity.Basic;
                    break;
                }
                case "Special": {
                    cardRarity = Rarity.Special;
                    break;
                }
            }

            switch (color) {
                case "Red": {
                    cardColor = Color.Red;
                    break;
                }
                case "Green": {
                    cardColor = Color.Green;
                    break;
                }
                case "Blue": {
                    cardColor = Color.Blue;
                    break;
                }
                case "Purple": {
                    cardColor = Color.Purple;
                    break;
                }
                case "Colorless": {
                    cardColor = Color.Colorless;
                    break;
                }
                case "Curse": {
                    cardColor = Color.Curse;
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
        Boss,
        BossChest,
        Unknown,
        Animation,
    }
    public static class NodeTypeExtensions {
        public static bool IsFight(this NodeType nodeType) {
            return nodeType switch {
                NodeType.Fight => true,
                NodeType.Elite => true,
                NodeType.MegaElite => true,
                _ => false,
            };
        }
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
        Any,
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
        public int card_random_seed_randomizer;
        public bool chose_neow_reward;

        public PlayerCharacter character;
        public MapNode[,,] map = new MapNode[4, 7, 15];
        public float infiniteBlockPerCard;
        public float infiniteMaxSize;
        public bool infiniteDoesDamage;
        public int earliestInfinite;
        public bool buildingInfinite;
        public bool expectingToRedBlue;
        public int missingCardCount;
        public List<string> huntingCards = new List<string>();
        public float chanceOfOutcome;
        public float addedDamagePerTurn;
        public float addedBlockPerTurn;
        public bool badBottle;
        public float[] transformValues;
        public string[] averageTransformValueIds;


        public PumpernickelSaveState() {
            instance = this;
        }

        public int AddCardById(string name) {
            var upgrades = name.EndsWith("+") ? 1 : 0;
            name = name.Replace("+", "");
            var card = new Card() {
                id = name,
                upgrades = upgrades,
                misc = 0
            };
            card.CopyFrom(Database.instance.cardsDict[card.id]);
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
            }
        }
        public MapNode[] Act4() {
            var heart = new MapNode() {
                nodeType = NodeType.Boss,
                position = new Vector2Int(3, 3),
            };
            var elite = new MapNode() {
                nodeType = NodeType.Elite,
                position = new Vector2Int(3, 2),
                children = new List<MapNode>() { heart },
            };
            var shop = new MapNode() {
                nodeType = NodeType.Shop,
                position = new Vector2Int(3, 1),
                children = new List<MapNode>() { elite },
            };
            var fire = new MapNode() {
                nodeType = NodeType.Fire,
                position = new Vector2Int(3, 0),
                children = new List<MapNode>() { shop },
            };
            return new MapNode[] {
                fire, shop, elite, heart
            };
        }
        public MapNode GetCurrentNode() {
            var talkingToNeow = (room_x == 0 && room_y == -1);
            var newAct = (room_x == -1 && room_y == -1);
            var bossChest = (room_x == -1 && room_y > 10);
            if (talkingToNeow || newAct) {
                var fakeNode = new MapNode();
                fakeNode.children = Enumerable.Range(0, map.GetLength(1)).Select(x => map[Save.state.act_num, x, 0]).Where(x => x != null).ToList();
                return fakeNode;
            }
            if (bossChest) {
                return null;
            }
            if (Save.state.act_num == 4) {
                return Act4()[room_y];
            }
            var CurrentNode = map[Save.state.act_num, room_x, room_y];
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
        public int EmptyPotionSlots() {
            return potions.Where(x => x.Equals("Potion Slot")).Count();
        }

        public void HuntForCard(string cardId) {
            if (huntingCards == null) {
                huntingCards = new List<string>();
            }
            if (!huntingCards.Contains(cardId)) {
                huntingCards.Add(cardId);
            }
        }

        public float GetTransformValue(Color color, out string averageCardId, int removeIndex = -1) {
            var index = (byte)color;
            if (transformValues == null) {
                transformValues = new float[(byte)Color.COUNT];
                averageTransformValueIds = new string[transformValues.Length];
                for (int i = 0; i < transformValues.Length; i++) {
                    transformValues[i] = float.NaN;
                }
            }
            if (float.IsNaN(transformValues[index])) {
                transformValues[index] = Evaluators.AverageTransformValue(color, out averageCardId, removeIndex);
                averageTransformValueIds[index] = averageCardId;
            }
            averageCardId = averageTransformValueIds[index];
            return transformValues[index];
        }
    }
}
