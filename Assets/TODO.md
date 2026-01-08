---------- MAKE THE GAME FUN!!! ----------------

Blocks ARE enemies, you MINE/defeat them and gain a REWARD



Procedural world -> your advantage! Utilize it! Is it bad to have a base? NO its good, but maybe you can teleport? You want to be able to go FAR from your base swimming back the same way is boring!



* you SHOULD have different ways to change up the generation as you progress, it could be cool to have UPGRADES focus on this.
* Would be cool if ENTITIES could effect gameplay, and maybe these could be generated as well



Item based and you just have the items show levels





Make the game PLAYABLE, first two stages, game finish at reaching 3rd stage

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



Now a brimstone upgrade works like so:

1. Player purchases brimstone dmg UpgradeRecipeSO
2. StatModAbilityEffectSO triggers Apply method, this creates a new StatModifier, and applies it to the brimstoneAbility with AddInstanceModifier

3\. Next time we use the brimstone, it will use the value through GetFinalAbilityMultiplier()

Now the same has to happen with other upgrades, like the lazer, we add a statModifier to the ability, and the lazer will use that modifier in its new calculations



A tool applies an temporary buff when activated. Which can be said is a temporary ability

The laser tool is a permanent ability, which can unlock temporary ability, which result into a temporary buff

The player movement is a permanent ability, which can unlock a temporary dash ability, which results into a temporary buff



Temporary Stat changes: - THIS WE HAVE IMPLEMENTED

only shows when active



TODO NOW ACTUALLY:



LOCK IN!!!



* Implement laser blast ability - DONE
* Make abilities with cooldown show up in a different part of the UI - DONE
* Change the upgrades to use new ability system -> upgrades add modifiers to the AbilityInstance which store them into \_instanceMods, we use GetEffectiveStat within the ability logic to get the stat. If we want to upgrade a buff ( like the brimstone ) its modifiers will be on its own AbilityInstance, when we add a buff to the lazer AbilityInstance, we create a new buffInstance which takes into account the "upgrades" we've done on the brimstone, and add that to the lazer, and boom we have a better lazer ability - DONE (need to add lots of other upgrades)
* Hover over base nodes to see what it is and possibly their current stats - Wait for this
* Add biome buffs - DONE

Make internal playtest:

* Update upgrade to support new stats

 	- Unlocks now add ABILITIES through PlayerAbilities.AddAbility.

* Make biomes spawn randomly \& Make artifacts, spawn at random spots, you mine it, you get the biome ability - DONE
* implement specific biome abilities



Hide un obtainable upgrades, darken ones you don't have money for



What do we do now? Do we go back to the upgrades? It does need work. Especially the tree because it is very broken right now

After that we can add the level up system

* Make sure ALL upgrades work!



How should the upgrades actually change the values?

* "Normal" lazer upgrades, should just say the mining damage, or the range it has, don't have it 
* Blast upgrade should say how much the blast multiplies the value by 



What should the blast do? 

First, read the base blast buff damage mult. This is 1.5 and on the buff instance





What the fuck man, I need a flow of the buffs,

Trigger buff -> create a new buffInstance from the base stats -> Add upgrades to the buff which we take FROM the ability instance, which has modifiers on which we APPLY to the buff. 

This modified buff now gets triggered ONTO the target ability, which is the lazer



How do upgrade work with buffs?

We said that we needed to add statMods to the ability that we where going to upgrade. Well, the brimstone upgrade gets added to the brimstone ability instance, so that would say multiply damage by +2, so if the base is 1.5x then it would be 3.5x



SO, when we calculate the actual value, all we would need to do is 







