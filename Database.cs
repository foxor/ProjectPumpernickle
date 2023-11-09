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

        public void OnLoad() {
            foreach (var card in cards) {
                cardsDict[card.id] = card;
                card.OnLoad();
            }
            foreach (var encounter in encounters) {
                encounterDict[encounter.id] = encounter;
            }
            foreach (var relic in relics) {
                relicsDict[relic.id] = relic;
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
