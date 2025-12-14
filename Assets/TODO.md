---------- MAKE THE GAME FUN!!! ----------------

* Hover over base nodes to see what it is and possibly their current stats
* 

Blocks ARE enemies, you MINE/defeat them and gain a REWARD



Procedural world -> your advantage! Utilize it! Is it bad to have a base? NO its good, but maybe you can teleport? You want to be able to go FAR from your base swimming back the same way is boring!



* you SHOULD have different ways to change up the generation as you progress, it could be cool to have UPGRADES focus on this.
* Would be cool if ENTITIES could effect gameplay, and maybe these could be generated as well



Item based and you just have the items show levels





Make the game PLAYABLE

1. Make it so the first stage takes about 10-20 minutes to complete



Code Temp status effects and make them show up in UI

* What temp stats do we show? What is a temp stat, its not the \_activeModifiers, because that is each StatModifier individualy, we'd need a collection of the actuall buff part, and show that to the player, not what is RAW under the hood



Active abilities:

Shows all the time, two states



Passive abilities:

Shows all the time, one state



**Upgrades ATTACH to the abilityInstance, this way we can target a single "item" upgrade**

It's not how you have it programmed now, right now, when we upgrade the mining dmg, it just increases that stat globally

Not specifically to the mining dmg to of the tool we are using

Doing it this way also actually makes the passive abilities truly global



A tool applies an temporary buff when activated. Which can be said is a temporary ability

The laser tool is a permanent ability, which can unlock temporary ability, which result into a temporary buff

The player movement is a permanent ability, which can unlock a temporary dash ability, which results into a temporary buff



Temporary Stat changes: - THIS WE HAVE IMPLEMENTED

only shows when active



TODO NOW ACTUALLY:



* Implement laser blast ability - DONE
* Make abilities with cooldown show up in a different part of the UI

Easiest way would be to have it event driven and just subscribe to ability changes, add/remove, and update for timed based ones

* Change the upgrades to use new ability system -> upgrades add modifiers to the AbilityInstance which store them into \_instanceMods, we use GetEffectiveStat within the ability logic to get the stat. If we want to upgrade a buff ( like the brimstone ) its modifiers will be on its own AbilityInstance, when we add a buff to the lazer AbilityInstance, we create a new buffInstance which takes into account the "upgrades" we've done on the brimstone, and add that to the lazer, and boom we have a better lazer ability
* Add compass
