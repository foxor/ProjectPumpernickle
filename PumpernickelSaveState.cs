﻿using System.Reflection.Metadata;
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
        Poison,
        Speculative,
        Weak,
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
        public float evaluatedScore;

        public Dictionary<string, float> tags;
        public CardType cardType;
        public int intCost = -1;
        public Rarity cardRarity;
        public Color cardColor;
        public bool isNew;
        public Dictionary<string, float> setup;
        public Dictionary<string, float> payoff;
        public Dictionary<string, float> goodAgainst;
        public Dictionary<string, float> combo;
        public List<ArchetypeMembership> archetypes;

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
            this.combo = fromDatabase.combo;
            this.archetypes = fromDatabase.archetypes;
        }

        public void ParseDescription() {
            // FIXME: we should split the card and the upgraded card at database load time
            tags = tags.ToDictionary(x => x.Key, x => x.Value);
            var damageRegex = new Regex(@"Deal (\d+) (\((\d+)\) )?damage");
            var damageMatch = damageRegex.Match(description);
            if (damageMatch.Success) {
                var damageUpgadeRegexGroup = damageMatch.Groups[2].Length > 0 ? 3 : 1;
                var groupIndex = upgrades == 0 ? 1 : damageUpgadeRegexGroup;
                tags[Tags.Damage.ToString()] = float.Parse(damageMatch.Groups[groupIndex].Value);
            }
            var blockRegex = new Regex(@"Gain (\d+) (\((\d+)\) )?Block");
            var blockMatch = blockRegex.Match(description);
            if (blockMatch.Success) {
                var blockUpgadeRegexGroup = blockMatch.Groups[2].Length > 0 ? 3 : 1;
                var groupIndex = upgrades == 0 ? 1 : blockUpgadeRegexGroup;
                tags[Tags.Block.ToString()] = float.Parse(blockMatch.Groups[groupIndex].Value);
            }
            if (description.Contains("Weak") && !tags.ContainsKey(Tags.Block.ToString())) {
                tags[Tags.Block.ToString()] = 1f;
            }
            var costRegex = new Regex(@"(\d+)( \((\d+)\))?");
            var costMatch = costRegex.Match(cost);
            if (costMatch.Success) {
                var upgradeCostRegexGroup = costMatch.Groups[2].Length > 0 ? 3 : 1;
                var groupIndex = upgrades == 0 ? 1 : upgradeCostRegexGroup;
                intCost = int.Parse(costMatch.Groups[groupIndex].Value);
            }
            else {
                intCost = int.MaxValue;
            }
            var drawRegex = new Regex(@"Draw (\d+) (\((\d+)\) )?card");
            var drawMatch = drawRegex.Match(description);
            if (drawMatch.Success) {
                var drawRegexGroup = drawMatch.Groups[2].Length > 0 ? 3 : 1;
                var groupIndex = upgrades == 0 ? 1 : drawRegexGroup;
                tags[Tags.CardDraw.ToString()] = float.Parse(drawMatch.Groups[drawRegexGroup].Value);
            }
            var selfExhaustRegex = new Regex(@"\n(Exhaust|Ethereal)\.");
            var selfExhaustMatch = selfExhaustRegex.Match(description);
            if (selfExhaustMatch.Success) {
                tags[Tags.NonPermanent.ToString()] = 1f;
            }
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
            if (combo == null) {
                combo = new Dictionary<string, float>();
            }
            if (archetypes == null) {
                archetypes = new List<ArchetypeMembership>();
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
            ParseDescription();
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
        public MapNode[,,] map = new MapNode[5, MAX_MAP_X, MAX_MAP_Y];
        public float infiniteBlockPerCard;
        public int infiniteMaxSize;
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
        public Dictionary<string, float> archetypeIdentities;
        public List<ArchetypeSlotEntries> archetypeSlots;
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
            archetypeIdentities = null;
            archetypeSlots = null;
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
            if (upgrades > 0) {
                card.ParseDescription();
            }
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
        protected void CreateActFourMap() {
            map[4, 3, 3] = new MapNode() {
                nodeType = NodeType.Boss,
                position = new Vector2Int(3, 3),
            };
            map[4, 3, 2] = new MapNode() {
                nodeType = NodeType.Elite,
                position = new Vector2Int(3, 2),
                children = new List<MapNode>() {
                    map[4, 3, 3]
                }
            };
            map[4, 3, 1] = new MapNode() {
                nodeType = NodeType.Shop,
                position = new Vector2Int(3, 1),
                children = new List<MapNode>() {
                    map[4, 3, 2]
                }
            };
            map[4, 3, 0] = new MapNode() {
                nodeType = NodeType.Fire,
                position = new Vector2Int(3, 0),
                children = new List<MapNode>() {
                    map[4, 3, 1]
                }
            };
        }
        protected string FakeActFourPathText() {
            return string.Join("\n", Enumerable.Range(0, 29).Select(r => {
                return new string(Enumerable.Range(0, 19).Select(c => {
                    if (c != 10) {
                        return ' ';
                    }
                    if (r == 4) {
                        return 'H';
                    }
                    if (r < 4) {
                        return ' ';
                    }
                    if (r == 10) {
                        return 'E';
                    }
                    if (r == 20) {
                        return 'S';
                    }
                    if (r == 25) {
                        return 'F';
                    }
                    return '|';
                }).ToArray());
            }));
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
            CreateActFourMap();
            if (Program.lastReportedGreenKeyLocation != null) {
                var location = Program.lastReportedGreenKeyLocation.Value;
                map[location.actNum, location.x, location.y].nodeType = NodeType.MegaElite;
            }
            var pathTexts = actLines.Select(actLines => {
                var lines = actLines.Select(x => x.Substring(7)).ToArray();
                return string.Join("\n", lines);
            }).Append(FakeActFourPathText()).ToArray();
            PumpernickelAdviceWindow.instance.Invoke(PumpernickelAdviceWindow.SetPathTexts, new object[] { pathTexts });
        }

        public void OnLoad() {
            foreach (var card in cards) {
                card.MergeWithDatabaseCard(Database.instance.cardsDict[card.id]);
                card.ParseDescription();
            }
        }
        public static readonly string NEW_ACT_ROOM = "new act";
        public MapNode GetCurrentNode() {
            var talkingToNeow = current_room.Equals("com.megacrit.cardcrawl.neow.NeowRoom");
            var newAct = current_room.Equals(NEW_ACT_ROOM);
            var bossChest = current_room.Equals("com.megacrit.cardcrawl.rooms.TreasureRoomBoss");
            var bossRoom = current_room.Equals("com.megacrit.cardcrawl.rooms.MonsterRoomBoss");
            if (talkingToNeow || newAct) {
                var fakeNode = new MapNode();
                fakeNode.position = new Vector2Int(3, -1);
                fakeNode.children = Enumerable.Range(0, map.GetLength(1)).Select(x => map[act_num, x, 0]).Where(x => x != null).ToList();
                Save.state.room_y = -1;
                return fakeNode;
            }
            if (bossChest) {
                return new MapNode() { nodeType = NodeType.BossChest, position = new Vector2Int(-1, 16) };
            }
            if (bossRoom) {
                return new MapNode() { nodeType = NodeType.Boss, position = new Vector2Int(-1, 16) };
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
        public int DropPotion(string potion) {
            for (int i = 0; i < potions.Length; i++) {
                if (potions[i].Equals(potion)) {
                    potions[i] = "Potion Slot";
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
        public void AddArchetypeSlotMember(ArchetypeMembership member) {
            if (archetypeSlots == null) {
                archetypeSlots = new List<ArchetypeSlotEntries>();
            }
            var existingId = archetypeSlots.FirstIndexOf(x => x.archetypeId == member.archetypeId && x.slotId == member.slotId);
            if (existingId != -1) {
                archetypeSlots[existingId].entries++;
                return;
            }
            archetypeSlots.Add(new ArchetypeSlotEntries() {
                archetypeId = member.archetypeId,
                slotId = member.slotId,
                entries = 1
            });
        }
        public int GetArchetypeSlotMembership(ArchetypeMembership membership) {
            return GetArchetypeSlotMembership(membership.archetypeId, membership.slotId);
        }
        public int GetArchetypeSlotMembership(string archetypeId, string slotId) {
            if (archetypeSlots == null) {
                return 0;
            }
            var existingId = archetypeSlots.FirstIndexOf(x => x.archetypeId == archetypeId && x.slotId == slotId);
            if (existingId != -1) {
                return archetypeSlots[existingId].entries;
            }
            return 0;
        }
        public IEnumerable<string> GetArchetypeSatisfiedTags(string archetypeId) {
            if (archetypeSlots == null) {
                yield break;
            }
            var slots = Database.instance.archetypeDict[archetypeId].slots;
            foreach (var archetypeSlot in archetypeSlots) {
                if (archetypeSlot.archetypeId == archetypeId) {
                    var slotFulfilment = slots[archetypeSlot.slotId].count;
                    if (archetypeSlot.entries >= slotFulfilment) {
                        yield return archetypeSlot.slotId;
                    }
                }
            }
        }
    }
}
