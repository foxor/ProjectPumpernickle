using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPumpernickle {
    internal class Database {
        public static Database instance;
        public Card[] cards = null;
        public Dictionary<string, Card> cardsDict = new Dictionary<string, Card>();

        public void OnLoad() {
            foreach (var card in cards) {
                cardsDict[card.id] = card;
            }
        }
    }
}
