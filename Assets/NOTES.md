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



OKAY, I'm doing duration and recharge times now, BUT HOW THE FUCK ARE WE GOING TO DO IT??

In the upgrade node I just want to see the upgrade time, so 10 dur to 20 for example, nice and easy

BUT, the big question is, do we have the duration and recharge be their own separate stats? For example, one character could have a default duration time of 20, and if a different character has a duration of 10, and all the durations are based on the duration, than the second character would have all the durations halved. This would be quite cool right? So then under the hood, the upgrade really just multiplies the base duration by an amount, meaning if our base duration is 10s, and say the blast is 1 dur, than it would do 10 \* 1, and later, and upgrade, will increase the mult to 1.2x so then it would be 10\*1.2 = 12s BUT. We'd still want to show the END result, which is different compared to the blast damage for example, where we want to see the multiplication number, but here we want to see the end number.

 	We could also have it be based on the duration, but as a flat number, so we could start with a base dur of 10, and then a buff could say 5, which would ADD the base dur by 5 -> 15s. Then another character could have base dur be 5 and then we add 5 and it would be 10, but that would not be fair really and not actually give a point to it



Trigger buff -> It puts the stat mods of that buff on the instance

but then the stats mods are 0.2+0.2+0.2 which is 0.6, and then it will just do 5\*max(1,0.8) = 5 that's why its showing 5

It works for other buffs because when we create the buff instance







On the lazer instance we have statmod dur mul 1.6



Then on the brimstone, we have 3 stat modifiers, that being the 0.2 three times



Its good that it adds the damage and range buffs to the mining lazer, because we use that on the mining lazer, BUT, what about duration, that is on the blast





Use your own little brain. I like the new Stat class but you have to actually THINK of how you want the buffs to work

The biggest problem right now:



Why don't we simply have the buff upgrade target the buff instead of the ability?? Then we find that buff instance, but no, that buff instance can't exist. Where do we spawn the buff?? when we spawn the buff, if we have a modifier with the same stat effect as the buff we are applying, add that to the buff, then buff will be upgraded





We should treat the duration and cooldown the same way as any other stat. Say our base cooldown stat is 10, that means 10 seconds for all cooldowns, this would mean if we want brimstone to be 3 seconds we'd need to have the cooldown stat for brimstone be 0.3, then upgrading it by 0.5 would make it 6 seconds etc. Just have it as any other stat





CCooldown doesn't work right now because we multiply the value with 0.2 giving us max(1,0.2) so that wouldn't work

Solution would be to internally change the cooldown to go lower when its higher. Would it be speed? 10 seconds cooldown, 1.2 cooldown multiplier, that would then be (1.2-1 \* 10 ) = 8s say its 2 then we would get (2-1 \* 10 ) = 5? no

it will be base/cooldownStat





What does magnatism MEAN!??

We have the following:

* Magnatism force: This should be the force that is applied to the items, the speed they get to you
* Magnatism range: The range that this forcefield of yours is.

Do we have one variable determine these two values?



## Planning leveling system:

Actually these systems:



Experience gain system? Something that actually gains experience. Right now that is literally just from blocks, it should calculate how much XP value we get froma  certain block being destroyed. This could honestly simply be within the level system



Level system: Should just contain the player levels, and an XP counter. It should alert when we go to the next level



Reward system: Listens to level system, when we level up, we gain a reward. Reward should be picked from a pool of rewards, which we take from other systems. Upgrade tree should have a function that returns a random reward based on the cost





OK STEP BY STEP:

1. Add levelling system
2. Make reward system
3. Make UI NOW, show just a name that's all I care about atm
4. Make so you can choose it and get the reward!
5. Now make the ability rewards and actually have a POOL that we can choose from,



Okay but how will we do the pool? maybe from game setup manager? Because pool could expand depending on our unlocks



Its fucked because execute is the result, we do gain the buff and stuff, but nothing is actually tracked, like when we click on an upgrade node from the UI we call TryPurchase upgrade in upgrademanager which then adds to unlocked upgrades



BRUH, the upgrade nodes are so fucking badly coded and its not that fucking complicated.



You have the actual upgrade data, this is what the upgrade DOES. Then you also have the upgrade COSTS. This depends on some factors like what the upgrade tree increasing costs are, what item pool we draw from, what stage this upgrade is in. BUT THIS IS RUNTIME DATA. It shouldn't really be set in the scriptable object it just makes it so ugly. It would be better if it was some kind of plain C# class that simply caches these costs and then we can just get them when we hover over things, or when the tree needs to know the costs of a certain upgrade. Now we have to ask it to generate everytime it doesn't fucking store it anywhere



Its coded so fucking stupid why is everything calculated dynamically? I see literally no point in it. We use our unlocked upgrades to calculate everything. If we have the stage unlocked, we go to the next stage and then we calculate d



UpgradeNodeSO. Holds how many stages the node has.

UpgradeNode: Plain class that holds current upgrade data. cost, value, upgradeEffect, etc.

UIUpgradeNode: Holds upgradeNode, and simply displays the data it has.



Nah I'm not doing that anymore



Okay. Ability UPGRADES!!



Basically will be totally prodecural.

Creates an upgrade object? It chooses one of the players unlocked abilities they got from the upgrade POOL.

 

Them, it should look at that ability upgradable stats, and choose one, THEN, it checks which ratity this upgrade it should be, then its like boom, this is the upgrade, +0.4 damage to bouncing ball. Then this should obviously be a statmodifier, and then when we apply this, we use it as IExecutable and boom the ability simply adds it as an upgrade just like any other upgrade.



No need for scriptable objects, data is simply generated from the abilitySO data upgradeable values list





Okay, where the fuck do ORE upgrades go? Do we have a separate script that holds that data? Right now the drops are in a "droptable" which is fucking useless because its a scriptable object and we can't change that during runtime.



You need to fucking define how you want these upgrades to work otherwise how will you know how to code it!??



Should work like this: Each ore tile can be upgraded. Say copper node upgrade is "Chance for copper to yield more ores when broken" that's all you have to say. Then internally, you store that the copper tile level is now 1, or something, and that's it! Then in the chunk manager you say, what should I drop for this tile? And then that does it. This would be well fitted for an "Item drop" manager or something where the chunkmanager just says, "player broke this tile, what should I drop?" Then TileDropManager says, ah, this tile has an upgrade to it, and the player has 5 luck, that means you need to drop 3 copper, BOOM





BRUh, what if a tile has two drops? Can we just assume now it wont have two drop, when will you ever need a tile that drops different things?



Gamelength, how will we do it? From megabonk: Stage 1 and Stage 2 give you a timer to farm mobs, loot chests, and use shrines. You can call in the boss early, but most players prefer waiting until near the end to maximize XP and upgrades. Boss fights end the stage and move you to the next tier. By Tier 3, the game shifts. You can choose to fight the final boss to end your run, or keep farming enemies for leaderboard scores. If you keep going, enemy scaling continues indefinitely, and the final swarm becomes your test of survival.



I like that there is an element of "how long can you survive!?" And if you are a hardcore player that is basically what your entire run is based on, to get the most powerfull as you can so you can survive even the most insane enemies. But with Resurface its different. How will you do it? Megabonks goal is "how long can you survive?", while resurface is "can you get to the surface?" Which doesn't really give you a final stretch. Sure, you can make the journey there more difficult with modifiers etc. But so can you with megabonk and that will still give you that ending thing that really tests how your current strength is. So is resurface just flawed? Can you not get that in resurface? How do you make the block breaking power matter? You can treat the blocks as the enemies, and maybe they are in your way? But still, you've resurfaced, but maybe it could just be like slay the spire, you've done it, you've beaten the game, and that's it, you should be proud of yourself for getting there. And then maybe the leaderboards are there which determine your score based on the SPEED. Or something, maybe a balance of speed and level reached or something idk





I think we have to redo the entire fucking way we display the upgrade nodes because we are expecting a upgrade recipe data while we don't really have that when we show a basenode.



This you can get purely from the SO

* Icon
* Title
* Description

This is depending on gamestate

* Cost
* ALL Effect result
* State
* Stage progress

If we put ALL this in a class, then we can simply just say GetVisualData() and boom we get this all, and if its a base node, we can just fill it appropriately. Then the UI node just takes this data and displays it. We don't give a shit about anything else





If we can afford it? This we could derive from the cost









