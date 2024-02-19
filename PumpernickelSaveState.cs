using System.Reflection.Metadata;
using System;
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
        Shop,
    }
    public enum Tags {
        NonPermanent,
        CardDraw,
        Damage,
        Block,
        BottleEquity,
        ExhaustCost,
        Unpurgeable,
        PickLimit,
    }
    public enum Color {
        Red,
        Green,
        Blue,
        Purple,
        Colorless,
        Curse,
        Any,
        Eligible,
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
        public float scaling;

        public Dictionary<string, float> tags;
        public CardType cardType;
        public int intCost = -1;
        public Rarity cardRarity;
        public Color cardColor;
        public bool isNew;
        public Dictionary<string, float> setup;
        public Dictionary<string, float> payoff;
        public Dictionary<string, float> goodAgainst;

        public void MergeWithDatabaseCard(Card fromDatabase) {
            this.name = fromDatabase.name;
            this.type = fromDatabase.type;
            this.cost = fromDatabase.cost;
            this.description = fromDatabase.description;
            this.bias = fromDatabase.bias;
            this.upgradePowerMultiplier = fromDatabase.upgradePowerMultiplier;
            this.upgradeBias = fromDatabase.upgradeBias;
            this.rarity = fromDatabase.rarity;
            this.color = fromDatabase.color;
            this.bottled = fromDatabase.bottled;
            this.scaling = fromDatabase.scaling;

            this.tags = fromDatabase.tags;
            this.cardType = fromDatabase.cardType;
            this.intCost = fromDatabase.intCost;
            this.cardRarity = fromDatabase.cardRarity;
            this.cardColor = fromDatabase.cardColor;
            this.isNew = fromDatabase.isNew;
            this.setup = fromDatabase.setup;
            this.payoff = fromDatabase.payoff;
            this.goodAgainst = fromDatabase.goodAgainst;
        }

        public void OnLoad() {
            if (tags == null) {
                tags = new Dictionary<string, float>();
            }
            if (setup == null) {
                setup = new Dictionary<string, float>();
            }
            if (payoff == null) {
                payoff = new Dictionary<string, float>();
            }
            if (goodAgainst == null) {
                goodAgainst = new Dictionary<string, float>();
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
            if (description.Contains("Weak") && !tags.ContainsKey(Tags.Block.ToString())) {
                tags[Tags.Block.ToString()] = 1f;
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
                case "Status": {
                    cardType = CardType.Status;
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
        public static List<Card> DeepcopyList(List<Card> original) {
            if (original == null) {
                return null;
            }
            return original.Select(x => {
                var c = new Card();
                c.MergeWithDatabaseCard(x);
                c.upgrades = x.upgrades;
                c.misc = x.misc;
                c.id = x.id;
                return c;
            }).ToList();
        }
        public bool Upgradable() {
            if (id.Equals("Searing Blow")) {
                return true;
            }
            return upgrades == 0 && cardType != CardType.Curse;
        }
        public string Descriptor() {
            if (upgrades != 0) {
                return name + "+";
            }
            return name;
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
        public long? totalChildOptions;
        public bool branchUpgrades;
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
        public static readonly int MAX_MAP_X = 7;
        public static readonly int MAX_MAP_Y = 15;
        public static PumpernickelSaveState parsed;
        [ThreadStatic]
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
        public List<int> relic_counters = new List<int>();
        public string[] potions;
        public bool has_emerald_key;
        public bool has_ruby_key;
        public bool has_sapphire_key;
        public List<DamageTaken> metric_damage_taken;
        public int card_random_seed_randomizer;
        public bool chose_neow_reward;
        public int potion_chance;
        public int card_seed_count;
        public string current_room;

        public PlayerCharacter character;
        public MapNode[,,] map = new MapNode[4, MAX_MAP_X, MAX_MAP_Y];
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
        public List<string> choosingNow;
        public List<int> upgraded;
        public PumpernickelSaveState() {
            parsed = this;
        }
        public PumpernickelSaveState(PumpernickelSaveState original) {
            instance = this;

            // immutable or non-referential
            path = original.path;
            seed = original.seed;
            boss = original.boss;
            room_x = original.room_x;
            room_y = original.room_y;
            gold = original.gold;
            purgeCost = original.purgeCost;
            act_num = original.act_num;
            elites1_killed = original.elites1_killed;
            elites2_killed = original.elites2_killed;
            elites3_killed = original.elites3_killed;
            monsters_killed = original.monsters_killed;
            current_health = original.current_health;
            max_health = original.max_health;
            floor_num = original.floor_num;
            has_emerald_key = original.has_emerald_key;
            has_ruby_key = original.has_ruby_key;
            has_sapphire_key = original.has_sapphire_key;
            card_random_seed_randomizer = original.card_random_seed_randomizer;
            chose_neow_reward = original.chose_neow_reward;
            potion_chance = original.potion_chance;
            character = original.character;
            infiniteBlockPerCard = original.infiniteBlockPerCard;
            infiniteMaxSize = original.infiniteMaxSize;
            infiniteDoesDamage = original.infiniteDoesDamage;
            earliestInfinite = original.earliestInfinite;
            buildingInfinite = original.buildingInfinite;
            expectingToRedBlue = original.expectingToRedBlue;
            missingCardCount = original.missingCardCount;
            chanceOfOutcome = original.chanceOfOutcome;
            addedDamagePerTurn = original.addedDamagePerTurn;
            addedBlockPerTurn = original.addedBlockPerTurn;
            badBottle = original.badBottle;

            // intentionally shared
            event_chances = original.event_chances;
            elite_monster_list = original.elite_monster_list;
            metric_damage_taken = original.metric_damage_taken;
            map = original.map;

            // deep copied
            cards = Card.DeepcopyList(original.cards);
            relics = original.relics?.ToList();
            relic_counters = original.relic_counters?.ToList();
            potions = original.potions?.ToArray();
            huntingCards = original.huntingCards?.ToList();

            // not initialized
            choosingNow = null;
            upgraded = null;
        }

        public int AddCardById(string name) {
            var upgrades = name.EndsWith("+") ? 1 : 0;
            name = name.Replace("+", "");
            var card = new Card() {
                id = name,
                upgrades = upgrades,
                misc = 0
            };
            card.MergeWithDatabaseCard(Database.instance.cardsDict[card.id]);
            card.isNew = true;
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
                card.MergeWithDatabaseCard(Database.instance.cardsDict[card.id]);
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
        public static readonly string NEW_ACT_ROOM = "new act";
        public MapNode GetCurrentNode() {
            var talkingToNeow = current_room.Equals("com.megacrit.cardcrawl.neow.NeowRoom");
            var newAct = current_room.Equals(NEW_ACT_ROOM);
            var bossChest = current_room.Equals("com.megacrit.cardcrawl.rooms.TreasureRoomBoss");
            if (talkingToNeow || newAct) {
                var fakeNode = new MapNode();
                fakeNode.position = new Vector2Int(3, -1);
                fakeNode.children = Enumerable.Range(0, map.GetLength(1)).Select(x => map[act_num, x, 0]).Where(x => x != null).ToList();
                return fakeNode;
            }
            if (bossChest) {
                return new MapNode() { nodeType = NodeType.BossChest, position = new Vector2Int(-1, 16) };
            }
            if (act_num == 4) {
                return Act4()[room_y];
            }
            var CurrentNode = map[act_num, room_x, room_y];
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
            if (potions == null) {
                return 2;
            }
            return potions.Where(x => x.Equals("Potion Slot")).Count();
        }
        public IEnumerable<string> Potions() {
            return potions.Except(x => x.Equals("Potion Slot"));
        }

        public void HuntForCard(string cardId) {
            if (huntingCards == null) {
                huntingCards = new List<string>();
            }
            if (!huntingCards.Contains(cardId)) {
                huntingCards.Add(cardId);
            }
        }
        public void AddChoosingNow(string cardId) {
            if (choosingNow == null) {
                choosingNow = new List<string>();
            }
            choosingNow.Add(cardId);
        }
        public bool ChoosingNow(string cardId) {
            return choosingNow?.Contains(cardId) == true;
        }
        public IEnumerable<Card> CardsNotJustChosen() {
            if (choosingNow == null) {
                foreach (var card in cards) {
                    yield return card;
                }
                yield break;
            }
            var notSeen = choosingNow.ToList();
            foreach (var card in ((IEnumerable<Card>)cards).Reverse()) {
                var foundIndex = notSeen.FirstIndexOf(x => x == card.id);
                if (foundIndex != -1) {
                    notSeen.RemoveAt(foundIndex);
                    continue;
                }
                yield return card;
            }
        }
        public IEnumerable<Card> CardsJustChosen() {
            if (choosingNow == null) {
                yield break;
            }
            var notSeen = choosingNow.ToList();
            foreach (var card in ((IEnumerable<Card>)cards).Reverse()) {
                var foundIndex = notSeen.FirstIndexOf(x => x == card.id);
                if (foundIndex != -1) {
                    notSeen.RemoveAt(foundIndex);
                    yield return card;
                }
            }
        }
    }
}
