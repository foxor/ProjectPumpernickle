# ProjectPumpernickle

This project is intended to play Slay the Spire just like I do (Vegetablebread).  The intention is not to solve the game, just to play it like a human.

It should result in MAXIMALLY readable code, that reads just like I think about the game.  No AI training, no excessive simulation, just a realistic evaluation of how a human thinks about the game.  The intention is that this bot provides advice for specifically metagame decisions: events, card rewards, shops, etc.

I don't have any plans to integrate this as a bot that plays the game, just reads save files and outputs a recommendation.

The advisor window will provide advice on what to do in every situation, and assign a "pumpernickel points" to each decision.  The intention of pumpernickel points is that if you are able to get 100 pumpernickel points, that should be enough to win, and if you don't get that many, you should not be able to win.  Furthermore, the bot assumes you get points on a linear scale, starting at 0 on floor one.

If we define an "average" run in terms of room types and points per room as:
 - 10 elites            - 2.5pts    -> 25 pts total
 - 8 upgrade fires      - 2.5pts    -> 20 pts total
 - 2 rest fires         - 0pts      -> 0 pts total
 - 5 bosses             - 0pts      -> 0 pts total
 - 2 boss chest         - 5pts      -> 10 pts total
 - 15 fights            - 1.5pts    -> 22.5 pts total
 - 3 mid-act chest      - 3pts      -> 9 pts total
 - 6 question marks     - .75pt     -> 4.5 pts total
 - 3 shops              - 3pts      -> 9 pts total
 - 55 floors            - 1.9 pts   -> 100 pts total

Each card, relic and event has an evaluation function that decides how many points it is worth.  As a starting point, each thing has a "bias" that gets added together with contextual factors to determine the final score.  There ends up being 5 "evaluation depths":
 - Ruled out: Things like skipping a relic are normally not considered at all
 - Gut feeling: Rely on just a bias of how good the thing normally is
 - Situational: Call a specific function to evaluate how good that thing is in the current situation
 - Deep: Hypothetically gain the thing, then call all of the evaluation functions of our cards and relics and compare the result
 - Complete: Imagine gaining the thing, then simulate the rest of the game, every floor and fight, and figure out a chance of victory, as well as a score
The depth the bot goes to depends on how important the thing is.  All possible paths and rewards get a complete evaluation.  Potential tansforms and cards to add get a deep evaluation.  Potential events get a situational evaluation.

Bias ranges:
 - Cards: [+5, -1]
 - Relics: [+10, -1]
 - Events: [+7, -3]

This bot is designed to work only on Ascension 20.

Milestones (Difficulty):
 ✔ Get through a neow reward (20)
 ✔ Complete a run (45)
 ✔ Get through a decision with no changes (5)
 ✔ Complete a run with all classes (15)
 ✔ Complete a run with all aversarial action considered (40)
 ✔ Have all rote advice (biases for everything) (10)
 - Have an evaluation function for EVERYTHING (80)
 - Complete a run without crashing (5)
 - Complete a run without changing code (150)
 - Complete a run without changing anything (10)
 - Re-run SBC (30)
 - Re-run baalor run (5)

Total streams: 37
Difficulty complete: 155
Difficulty left: 280
estimated remaining streams: 67
Estimated remaining weeks: 34
Estimated completion date: 9/25

TODO:
eval functions for corruption and snecko
synnergy stuff

___ Architecture ___

System for "power rating" the deck.  Includes card-card combos, relic-card combos, etc.
 - Bias for each card / relic
 - Include context (class, deck, etc)
 - Wrath enter vs exit ratio
 - Combos (bias cog) + partial satiation
 - "Anticombos"
  - solving a problem that's already solved?
  - Bad upgrades + lesson learned / apotheosis
 - Hypothetical cards (deckbuilding integration)
 - Duplicate solution

Threat tracking (weighting upcoming fights)
 - Extension of the power rating system for specific fights
 - Damage by turn breakpoints
 - Block on turn breakpoints
 - Gremlin nob 4 -> 3
 - Guardian turn 2
 - Fight pools
 - Act 4 turn 2
 - Compromise between solving immediate problems and building a long-term plan.  Unlock later fights when earlier ones are solved.
 - Can't hit same elite as we just fought

Deckbuilding plan
 - Are we doing an infinite?
 - What are the best cards we could see?  How likely are we to see it?
 - What relics would be run changing?

Pathing (expected health / floor)
 - Includes value from potions
 - Alternate paths
 - Expected value of various reward types.
 - Expected potion availability
 - Risk of death
  - Consistency vs high risk system
 - How scary are wounds?
 - Expected gold (avoid double shop paths)
 - Expected flexibility for next act (act 3 with no green key)

Fight planning
 - Coincidence (+ or earlier) rate
 - Expected play rates
 -- Adjust positive power values down based on play rate
 - Expected health loss per fight
 - Damage on turn expectation (with per fight adjustments)
 - Are we a turn one deck?  Do we scale well for bosses?
 - Expected cards seen per turn
 - Expected energy available per turn

___ TODO ___

Power rank relic pools
 - Disclude previously encountered relics

Saphire key "power" value that scales.

Match and keep support