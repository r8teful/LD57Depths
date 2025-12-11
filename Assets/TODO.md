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



This all needs to be in the POPUP, it will need a major extention, maybe different verions of the popup prefab?



Code ability UI status

Hover effect on both of these





Implement some kind of ability inventory?? Then it pulls it from there?

Active abilities:



Shows all the time, two states



Passive abilities:

Shows all the time, one state



**Upgrades ATTACH to the abilityInstance, this way we can target a single "item" upgrade**

It's not have you have it programmed now, right now, when we upgrade the mining dmg, it just increases that stat globally

Not specifically to the mining dmg to of the tool we are using

Doing it this way also actually makes the passive abilities truly global



A tool applies an temporary buff when activated. Which can be said is a temporary ability

The laser tool is a permanent ability, which can unlock temporary ability, which result into a temporary buff

The player movement is a permanent ability, which can unlock a temporary dash ability, which results into a temporary buff



Temporary Stat changes: - THIS WE HAVE IMPLEMENTED

only shows when active



## The plan:

Change tools so that the are abilities. This makes it more flexible to add more tools and abilities later



**Core idea:**

* Abilities live within AbilityInstance scripts, they implement one or several IAbilityEffect/IPassiveEffect

 	- EffectPassive we do if its passive, which gets activated when we ADD the ability, through the Apply function

 	- EffectActive which implements the Execute function

* PlayerAbilities has a dictionary of all AbilityInstances, we use this script to add and remove abilities, we also use the effect of the abilities here, we also apply upgrades to the abilities here, abilities hold their own upgrades
* .



How buffs and upgrades work

base 10

lazer 0.5 -> 0.6

20 -> 12

1.5 - > 

5 -> 6

15 -> 16





First base stat -> then apply specific ability upgrades -> then apply buffs



First base stat -> then apply buffs



Different ways of actually holding the stats  either:

Have each ability hold its own stat or

Have the playerStatManager hold all the stats and each ability take from that list or



It also depends how generic you make the stats, you could have stats that are like, minigDamage, and then everything that uses miningDamage will get a buff. But 



## The Schedule:

**11/12/2025**

Finish rework, delete tool controller and replace it with PlayerAbilities,  try and make the lazer work with the new system

**12/12/2025**

Implement fish gun using the new system

**13/12/2025** record, edit, post, etc



