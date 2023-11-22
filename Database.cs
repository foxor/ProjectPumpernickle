using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public class Move {
        public string type;
        public string description;
        public int damage;
        public void OnLoad() {
            if (type.Contains("a")) {
                var match = Regex.Match(description, @"\d+ \((\d+)\)");
                if (match.Success) {
                    damage = int.Parse(match.Groups[1].Value);
                }
                else {
                    match = Regex.Match(description, @"(\d+)");
                    if (match.Success) {
                        damage = int.Parse(match.Groups[1].Value);
                    }
                }
            }
        }
    }
    public class Monster {
        public string id;
        public string name;
        public string type;
        public string minHP;
        public string maxHP;
        public string minHPA;
        public string maxHPA;
        public Move[] moves;
    }
    public class Encounter {
        public string id;
        public string pool;
        public int act;
        public int weight;
        public string[] characters;
        public bool special;
        public float medianExpectedHealthLoss;
        public float medianWorstCaseHealthLoss;
    }
    public class Relic {
        public string id;
        public float bias;
        public string pool;
        public string tier;

        public PlayerCharacter forCharacter;
        public Rarity rarity;

        public void OnLoad() {
            switch (pool) {
                case "": {
                    forCharacter = PlayerCharacter.Any;
                    break;
                }
                case "Red": {
                    forCharacter = PlayerCharacter.Ironclad;
                    break;
                }
                case "Green": {
                    forCharacter = PlayerCharacter.Silent;
                    break;
                }
                case "Blue": {
                    forCharacter = PlayerCharacter.Defect;
                    break;
                }
                case "Purple": {
                    forCharacter = PlayerCharacter.Watcher;
                    break;
                }
            }
            switch (tier) {
                case "Special": {
                    rarity = Rarity.Special;
                    break;
                }
                case "Common": {
                    rarity = Rarity.Common;
                    break;
                }
                case "Uncommon": {
                    rarity = Rarity.Uncommon;
                    break;
                }
                case "Rare": {
                    rarity = Rarity.Rare;
                    break;
                }
                case "Starter": {
                    rarity = Rarity.Basic;
                    break;
                }
                case "Boss": {
                    rarity = Rarity.Boss;
                    break;
                }
            }
        }
    }
    internal class Database {
        public static Database instance;
        public Card[] cards = null;
        public Monster[] creatures = null;
        public Encounter[] encounters = null;
        public Relic[] relics = null;

        public Dictionary<string, Card> cardsDict = new Dictionary<string, Card>();
        public Dictionary<string, Encounter> encounterDict = new Dictionary<string, Encounter>();
        public Dictionary<string, Relic> relicsDict = new Dictionary<string, Relic>();

        public Encounter[][] EasyPools = new Encounter[4][];
        public Encounter[][] HardPools = new Encounter[4][];
        public Encounter[][] Elites = new Encounter[4][];
        public Encounter[][] Bosses = new Encounter[4][];

        public void OnLoad() {
            foreach (var card in cards) {
                cardsDict[card.id] = card;
                card.OnLoad();
            }
            var easyBuilder = new List<Encounter>[] {
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
            };
            var hardBuilder = new List<Encounter>[] {
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
            };
            var eliteBuilder = new List<Encounter>[] {
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
            };
            var bossBuilder = new List<Encounter>[] {
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
                new List<Encounter>(),
            };
            foreach (var encounter in encounters) {
                encounterDict[encounter.id] = encounter;
                switch (encounter.pool) {
                    case "easy": {
                        easyBuilder[encounter.act - 1].Add(encounter);
                        break;
                    }
                    case "hard": {
                        hardBuilder[encounter.act - 1].Add(encounter);
                        break;
                    }
                    case "both": {
                        // Thanks DARKLINGS
                        easyBuilder[encounter.act - 1].Add(encounter);
                        hardBuilder[encounter.act - 1].Add(encounter);
                        break;
                    }
                    case "elite": {
                        eliteBuilder[encounter.act - 1].Add(encounter);
                        break;
                    }
                    case "boss": {
                        bossBuilder[encounter.act - 1].Add(encounter);
                        break;
                    }
                }
            }
            EasyPools = easyBuilder.Select(x => x.ToArray()).ToArray();
            HardPools = hardBuilder.Select(x => x.ToArray()).ToArray();
            Elites = eliteBuilder.Select(x => x.ToArray()).ToArray();
            Bosses = bossBuilder.Select(x => x.ToArray()).ToArray();
            foreach (var relic in relics) {
                relicsDict[relic.id] = relic;
                relic.OnLoad();
            }
            foreach (var monster in creatures) {
                if (monster.moves != null) {
                    foreach (var move in monster.moves) {
                        move.OnLoad();
                    }
                }
            }
        }
    }
}
