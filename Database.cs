using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    public class Monster {
        public string id;
        public string name;
        public string type;
        public string minHP;
        public string maxHP;
        public string minHPA;
        public string maxHPA;
    }
    public class Encounter {
        public string id;
        public string pool;
        public int act;
        public int weight;
        public string[] characters;
        public bool special;
    }
    internal class Database {
        public static Database instance;
        public Card[] cards = null;
        public Monster[] creatures = null;
        public Encounter[] encounters = null;
        public Dictionary<string, Card> cardsDict = new Dictionary<string, Card>();
        public Dictionary<string, Encounter> encounterDict = new Dictionary<string, Encounter>();

        public void OnLoad() {
            foreach (var card in cards) {
                cardsDict[card.id] = card;
            }
            foreach (var encounter in encounters) {
                encounterDict[encounter.id] = encounter;
            }
        }
    }
}
