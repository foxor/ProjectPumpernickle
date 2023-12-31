# ProjectPumpernickle

This project is intended to play Slay the Spire just like I do (Vegetablebread).  The intention is not to solve the game, just to play it like a human.

It should result in MAXIMALLY readable code, that reads just like I think about the game.  No AI training, no excessive simulation, just a realistic evaluation of how a human thinks about the game.  The intention is that this bot provides advice for specifically metagame decisions: events, card rewards, shops, etc.

I don't have any plans to integrate this as a bot that plays the game, just reads save files and outputs a recommendation.

The advisor window will provide advice on what to do in every situation, and assign a "pumpernickel points" to each decision.  The intention of pumpernickel points is that if you are able to get 100 pumpernickel points, that should be enough to win, and if you don't get that many, you should not be able to win.

Each card, relic and event has an evaluation function that decides how many points it is worth.  As a starting point, each thing has a "bias" that gets added together with contextual factors to determine the final score.

Bias ranges:
 - Cards: [+5, -1]
 - Relics: [+10, -1]
 - Events: [+7, -3]

This bot is designed to work only on Ascension 20.

Milestones:
 ✔ Get through a neow reward
 ✔ Complete a run
 ✔ Get through a decision with no changes
 - Complete a run with all classes
 - Complete a run with all aversarial action considered
 - Have all rote advice (biases for everything)
 - Have an evaluation function for EVERYTHING
 - Complete a run without crashing
 - Complete a run without changing code
 - Complete a run without changing anything
 - Re-run CBS
 - Re-run baalor run


TODO - Today:
 - Scores are too high
 - Not looking for elites even though they're "free"

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


Neow options:
 - Bias for costs
 - Bias for rewards
 - Power

Ability to compare important constraints (important to remove now vs important to get eruption+ before first elite)

Power rank relic pools
 - Disclude previously encountered relics

Track value of potions (value of potions in belt at moment)

Simulate shop!!!  Java plugin?

Saphire key "power" value that scales.

Power evaluation function per card and relic.

Display the analysis factors that make the decisions.

Things to consider when picking cards:
 - Power rating
 - Threat tracking
 - "Slot value" - Shouldn't pick follow up if we need flurry of blows.  Shouldn't pick cards if infinite planned

Shining light:
 - Compare random vs specific upgrade
 - Consider likelyhood of rest vs upgrade at next fire

Match and keep support

Bottles:
 - When to pick them
 - When to skip them
 - What to put in them

___ Tests ___

Don't pick block cards when you have a size constrained deck and white beast statue.  Also, this analysis needs to be readable.
Skip ragnarok with shuriken and mummified hand, but we need card removes and not damage.

 - Know how we're blocking
 - Know when it isn't good enough
 - Refactor fight strength to estimate damage and block
 - Convert potions to effective health
 - Use fires to mitigate risk