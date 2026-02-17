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

* Cost - Ingredient status !!
* ALL Effect result - stat change status
* State
* Stage progress

If we put ALL this in a class, then we can simply just say GetVisualData() and boom we get this all, and if its a base node, we can just fill it appropriately. Then the UI node just takes this data and displays it. We don't give a shit about anything else





If we can afford it? This we could derive from the cost



Okay let us think of the upgrades we want the players to have when. For example, what node that is disgustingly cheap, which one to have disgustingly expensive



Tool:

* Damage
* Range
* Mobility

Tool special:

* Range
* Damage
* Time



Player speed:

* Max speed
* Acceleration
* Dash



Other:

* Luck
* Ore upgrading



That's basically it. The golden rules we want with the upgrades are:

* ALWAYS have something to upgrade when the player gets back to the submarine
* ALWAYS make the upgrade give a meaningful change
* ALWAYS keep layering gameplay mechanics. As soon as players feel like they're getting the hang of things, add another layer. Basically just flow

 	- This can happen in several ways. 1. Through upgrades. The player can unlock a new ability, maybe they get the lazer ability, or the dash. 2. Through location. Player can discover new biomes, upgrading the submarine to get to a new area of the trench needs to feel refreshing



You can basically time it to perfection.



First 5 minutes players get used to the tool, after this they can unlock the tool special.

maybe 2-3 minutes after the tool special, they've used it about twice, they understand how the tool works. So they've been playing for 8 minutes now, they should have levelled up 3 times, that's about once ever 2.5 min, giving a nice little boost everytime, keeping them engaged.  Now after this they know what they're doing. they've got the ability, they've gotten some upgrades already. About this time is when they should be finding some biomes. Also maybe different ores should spawn when we get further away?? Then after exploring one biome, probably want to go there again, the biome needs to be exiting enough to get back to. then when they've seen enough, you'll want them to head to the next zone.





OH idea, we could have an unlock that teleports the player to the submarine just before they pass out, this could be such a good dopamine reaction, and a huge mindset change, we basically allow them to double the distance they can go and it will feel different compared to all the backtracking they are used to doing. We should have this available as soon as the backtracking becomes about 1/3 or so the time that they are exploring, maybe even less, basically, as soon as it becomes a problem.



Upgrade tree has too many nodes, it is just too overwhelming, you need to drop feed the tree to them at the start you can't just show them all this its too much



Biomes need to be EXCITING, you want to go back to them, because they are so good. BUT WHAT IS THAT?



\-



What could make something good in this game? S

* Something that gives you loads of resources
* Something that gives you a cool item or buff
* Something that makes you stronger







We're changing how going into the submarine works now. Don't ask my why, the game's broke. WHAT SHOULD HAPPEN:

* Dissable timelap collider \& outer submarine collider - done
* Enable submarine colliders/ or simply teleport it there and then out the way - done
* Stop mining things (disable tool \& abilities)
* For the camera, cull the following layers: Default, no player collisions, mining hit - done

That's it. Now code it and get back to balancing!!!



Okay but what managers do we need? Its all so bloated atm, old shit code



PlayerLayerController is the one executing the logic and handling state, its what evokes the event



One subinterior script: It subscribes to when player goes into the sub, then it teleports there, if they go out the sub it teleports away. easy peasy.





Other things should happen in tiny scripts, listening to the event. That's it, that is all you need





## USEFULL ENTITIES

I'm thinking we could have three categories:

1. Resource gaining: Treasure chests
2. Character stat buff: "Blessing"?
3. Mystery!!: Event Cave, mix of above two + extras, but could be negatives



My explanation: The first two are simple, you know what your getting, its something positive, something comforting. You gain something good, they make exploring and swimming around more interesting. And they also encourage you to explore. The last one is for players that want to go a step further, they have a negative effect, you can go there if you want, but at your own risk. They could give you a greater reward than the other two. But also they could have negative effect, like removing some of your resources, or giving you a NEGATIVE buff for a period of time. But that's okay, because the player was ready for that.





Okay what can you do, like, right NOW? Polish something, something easy and something that doesn't require much thought Maybe some screenshake? Make the ui nicer to navigate? Make a pause screen. I thought maybe removing more old shit code we wont need. Yes thats a good thing we





We need proper world gen order because we are doing everything in one go and its messy and not working. General order:



Idk I'm too tired for this shit wtf. We need to actually look deep into what this order does and how we get it right every time



OKAY GOOD MORNING IM CHARGED UP AGAIN



What we need to happen:

1. We create runtime instances of the SETTINGS that we have chosen
2. We then send these values to the shaders, basically, we want the shaders to read these values



Things we have to think about with world init order

* We need to wait for world gen to finish generating the initial state before putting player in control. This will require the following:
* EntityManager needs to be setup because WorldGen needs to send the entities to them









BRUH.

X = distance to center

Y = chance

t = ratio: 0 - 1 depending on distance

r = target radius

d = maxDepth

n = depthRATIO

b = bandwidth

p = widthPrecent -> Basically determines how flat the curve is







Resource pacing:

OK 1.63 mine damage breaks 120 blocks a minute. At IDEAL speed, in practice that would be 80. So 1/3 is error



Start - Get used to controls \& concepts

2min -> Understands Tool

**\*recipes require silver \& gold\***

3Min -> Understands Ability

4Min -> Understands Biomes \& movement

10min -> Completed first stage





Start - Get used to controls \& concepts

2min -> 120 blocks

3Min -> 180 blocks

4Min -> 240 blocks

10min -> 1200 blocks



2min -> 2 upgrades

3Min -> 3 upgrades

4Min -> 4 upgrades

10min -> 10 upgrades + submarine







First minute of oxygen I can get 17 stone 12 copper. First minute should be maybe even shorter, maybe 30 seconds? And then you would get half that so 7 stone and 6 copper. Then you want to be able to fix the upgrade machine, 2 stone for first upgrade, that would have to increase linearly first, and then exponentially!! woo exiting



We need these different overlapping goals. I feel like you should design it around going out of the submarine and then coming back. Each time, you have a goal on your mind, you'll get stone, you'll get iron, or you'll go to that biome, all because you want to achieve a certain task, maybe you want to buy that one upgrade, or fix you submarine. etc.



The amount of time you are out of the submarine should increase linearly, but maybe fall off at a certain time. That is kind of what we should aim for.





PROBLEM NOW:



Early game is good, First four upgrades are nice, quick and easy to get. But then there is the probem with iron, I'm getting it but its not really meaningfull I need to much iron right now. I don't know where to go from here I can't seem to upgrade anything interesting anymore.



Maybe because the damage is the most important we either split it into more stages, maybe one or two more so we get 5 total upgrades, and then we unlock the lazer blast



There's a bit too much copper right now,



How do we want this lazer blast to give the buff but ONLY when we fire it? I feel like the easiest solution is that within the abilityInstance Tick, if there's a flag on the buff itself saying that we should only tick it when a certain bool is true, that wold mean the duration would only go down when we've actually pressed the shoot button. But the problem is that we check b.expiresAt. We don't reduce a counter or timer at all. So the solution would be to have expiresAt be -1 while this bool is false, and as soon as we start shooting, we set expires at to the Time.Time + dur. So the buff so has to have a flag saying "only







Just played through it and it actually was really good. until the lazer blast, only things I would change is making the oxygen a bit less copper, and possibly making the range either a bit harder to get or less long, also iron isn't working properly when you pick it up. The ability is also at a good spot it isn't too powerfull but just gives you that extra novelty of something helping you







Ok you little dummy, what do you want the entity manager to actually do??



EntityManager should ultimately be a pool, when we generate them, we keep their position which is in the dictionary that maps persistantID to PersistantEntityData. This dictionary is never destroyed or reset, only until we exit the current run. This data is saved exiting. When we enter the biome at a later point, we check a different dictionary, cachedEntityIdsByChunk, which simply "activates" these entities, this should be then taken from the pool, and when we leave the chunk, we just move, or remove them, whatever



Need to have biome slightly closer



### REALITY CHECK:

Hello yes this is Rik again, we have done the first 8 minutes of gameplay, well done. Now that was pretty difficult to get right. But now you have to do 20 more minutes, maybe even 40 more minutes. How the fuck are we going to do that? Well, I have some tricks up my sleave. So, lets write out what we need to do next:



What is our baseline now??



Player has a powerful lazer, maybe it grows a bit too powerfull to quickly? But maybe that's good, idk. They can go outside the submarine for 70 seconds, combine that with a speed up and they can easily go to a biome and back to the submarine (that SHOULD be possible). They have a somewhat powerfull brimstone ability they can use SOMETIMES (it shouldn't completely demolish every block in its path like its doing now, NERF IT).



So where can we go from here? Players understand the loop, maybe its time to break it up a bit? We could focus more on the exploration part at this point perhaps. When they explore they can find the useful entities. Which will be quite cool to find. Perhaps the sub fixing should be around this time? Its hard to actually see because you're so focused on the upgrade tree. So maybe that could be part of the upgrade tree?





When the player goes to the next floor the stone should be a lot harder to mine, basically make it so you feel like you've gone back to the starting point





Maybe we add elements that spelunky has? A secret sequence of actions you can take



What if Data.x = 90? Then we select bio, but then we lose that we need durability = 300 or whatever. Basically you want to separate what you see "bio tile" vs what you get "stone + 300 durability" Solution is simply have the X be the "what you get" part, while the biome "what you see!". The problem with that is that right now the X basically just represents the TileID, which we still want to happen, but now the Y simply determines the TEXTUREINDEX. But how would we do that with TILESO??







UAIUGHAIGUHAIUGOH I feel like I need to redo the entire chunk manager it is so fucking messy I hate it all it has to do is store some stupid data its not so difficult. When I say that the tiles are layered, like the ore is dropped when we have an ore in that cell why can't I simply say the same but with the fucking durability







Basically we need to have the level up thing but also for the shrine, event, and possibly chest. Its within the same range because shrine and event will pause the game and show a screen, which is basically what a level up is aswell so we should re use the same code to make it easier for ourselves. We could make UILevelupscreen into its own managed thing, or we expand it into some kind of other manager that handles which thing we should spawn, yeah that probably makes sense





Chill out all you need left to do is the exploration entities, and then balance again, and your done that's basically the demo, atleast all the gameplay aspects of the demo. Then polishing and boom you have a demo and all you need to do is add replayablitiy, add more content, and you have a game. Its so easy just stick in there and youll be all good



What was that beep!?

Don't know what to fix first

Oxygen should be easier to upgrade because worried that Copper runs out ??

Not sure what I'm aiming for next?

Oxygen bar should tell you its oxygen



Don't feel like going outside of the trench because resource are close to it

Forgets to use lazer blast

Each trip should be shorter but give you more

Doesn't really care about passing out because it doesn't clearly show the consequences

Using lazer blast - Oh that was fun

I didn't read what it said when upgrading it so I don't know what button to press

Unlocl computer on the unlock gree??

Particles don't break?





Thoughts

Didn't feel the urge of getting stronger fast enough, didn't feel like I was making progress

Also couldn't see how many resources I had so it was had to tell if you got more in one run

Couldn't see any numbers go up, wasn't looking at that costs of the upgrades.

fever upgrades at the start but easier to get.





Make abilities come later? Try exploring other systems of gaining them



**Smaller visual stuff:**

* Ladder to floor
* sub Background is too plain \& submarine seems boring and lifeless
* Interactable things should also have a stroke "highlight"
* Popup is tiny
* Upgrade tree header in ui
* No units in upgrades
* Don't let submarine spawn inside a rock
* World background too blurry
* Particles look like copper



Gameplay

* Not sure what to do at the start, needs a bit more handholding
* You slide too much when moving and its very annoying
* Hard to notice improvement
* Don't know how much resources you have
* Forget to use lazer blast \& what button it needs to activate
* Lazer blast too powerful



Upgrades

* Make the pacing faster
* Add sub fixing to the tree because you forget it easy and you don't really notice it being there



For the inventory:

For yOUR inventory. Items should pop in when you pick them up.







Okay for the sub upgrade as nodes we simply want to do the following:



Submarine manager NEEDS certain upgrades before it can move, it doesn't have to hold progress or anything, this is already done by the upgrade tree nodes system thing. Basically when we get the effect, we sat to the submarine manager: Look! We've gotten upgrade with ID X, then it's like, cool, now we can change the sprite of this and this to make the sub look a bit better. Also, we can now move to the next zone because we have upgrade X. that's it, you don't need anything else. So how would the upgrading work? You simply have a node with 3 stages. Each stage fixes that specific thing one upgrade. when you've fully completed the node, that's when you can move the submarine further. How does



How do me make the pacing faster?? We simply make you improve faster DUH





* Remove world popup. Instead have it as a node
* Smaller sub outside hitbox
* Ores less hard
* Have the tree expand more, it stays too long within lazer upgrade + blast upgrade. We need more exiting things!! Could add magnetism, ore drop chance, luck.
* You have a lot of stone in the end maybe do something with that?



Lucy said that dropchance would be a good idea to add early. I like that, make  number go up quick!!



If oxygen is 40 to start, and you increase it by 40 to 80, then to get the add you need to get the END VALUE 80 and DEVIDE IT BY THE STARTING VALUE 40 = 2 = 200%??



If 10% is at the 40, then going to 80 is 20%



#### Solo playtest number #3

* nodes should update state as sub inventory changes
* pickup from SUB? add logic that prevents
* Better sub add, needs sounds think balatro
* nerf speed \& acceleration
* nerf iron cost (should cost iron
* add gold next!!
* maybe stay with iron for a bit longer?
* 2 ores SHOULD drop when you mine ONE not just one that counts like 2, maybe do this until like 4 or something and then you can start merging
* Lazer blast is still too strong, now that the lazer is very strong
* Magnatism doesn't have a value
* Make items actually disappear when you die, maybe you can pick them up when you come to the same spot?

Where to go from here?

* Now we start to get into BIOMES, also at around 10 minutes you've gotten the copper upgrade, then maybe the iron one aswell, as soon as you get the gold, you're chilling, you should be able to go to the next one...
* Tree should expand MORE!! Add more handling and range upgrades, but what ELSE??? Could have you unlock the abilities, that could be cool??
* 



#### Lucy playtest number #2 07/02/2026



* Nothing should block the sub
* Should not fix the upgrade panel from the upgrade panel
* Speed should not be connected to oxygen? Brain things that upgrade is related to oxygen because its branching FROM the oxygen
* Bar should have more divisions
* Doesn't have the feeling of getting strong really fast
* Ores should feel more rare? just feels like you find everything all the time
* Oxygen should have a value instead of?
* Oxygen bar should flash when we run out
* Expected the blast to fire when pressing space, not when holding the lazer button
* Dies -> Frustrating but deserved. But seems like she made it, but still didn't. Maybe have even extra long when you're close to sub
* Rename acceleration to movement control
* Doesn't feel like I'm upgrading enough, I feel like I should upgrade more things each time
* Switch lazer damage with range
* New color node with one upgrade but not getting one upgrade
* Background doesn't match with
* Cactus particle effect?? When you turn into one
* Still drags out for too long you want something else
* Make movement slower on the next layer, higher up -> More dense. Could have the same with oxygen!!
* Clarify that all three upgrades need to happen for you to move
* Items go From bob to PC? -> Visually show it go from bob to pc, and make the PC bounce or something (show that its receiving it)



Upgrade that shows the way back?

Upgrade that lets you only lose some of your items





The shader is just not considering the position, need to look if its also not taking the background. I think its going wrong because the position is not what matters its what INDEX they are within the biome array,



Desert is index 2

Forest is index 0





IDea: You have a random item pool which give interesting extra bonuses for the run, the point of these is to make each run different, but how would you get these items? Maybe from rewards you get from mining, or from the exploration entities



You could do some crazy stuff by "unlocking" iron ore, and it would reset the world and then generate it with it there, kind of like an early prestige



I'm making the world more dense because its all about mining and having lots of air kind of ruins the flow because you'd just be swimming which is not really what it is about



Divide 2nd oxygen into two instead of 1, its too big of a jump

Make acceleration give less drag



it feels weird when you've got so much movement and you've kind of seen the trench all the way to the top you don't feel like you need to do anything anmore, you've done your thing, you've made it to the top, there is nothing left to do. As soon as you can go to the top and back is kind of the point that you should be done with the level, so you would either make it really far up, like you'd not even come close with the speed and oxygen, or simply make the oxygen and speed slow enough so that you can't really reach the top. Also just add more upgrades, make the damage more, make the range longer, keep having those upgrades with the current scaling and see how it goes. Also these stages you have with stone, copper, iron, etc. As soon as you "clear" that stage, like there are no more upgrades with that resource so it feels kind of pointless to have them but I guess maybe the sub upgrades can have those resources?



For crit: Have a crit damage stat, add it to the tools crit damage, and times it by the normal damage, also crit chance is also added with tool crit chance, multiplied by crit chance buff etc



Fucking hate how the "ability" shit is structured its so messy with the active and passive shit its horrible I hate it



You're missing a lot of controll having the behaviours all be separate, they should be small scripts that are controlled through abilitymanager. Gives you more control



Notes from shelldiver juice

* Transfer has a very slight screenshake, the items go behind the character and pop in and rotate a bit , also loads of particles when that happens





BRUH, MINING CHAINNN. How do we do it. It should only fire when we're actually hitting a block, then do a random tile check within a circle, right? or should it do the closest adjacent. Think of it, its A CHAIN, like a lightning bolt, it searches the CLOSEST it possibly can, then damages that tile. Edge cases:

Have to keep track of our current damaging tile, it can't be the same as the chain target. If we stop shooting reset the chain target. Should only switch target when too far away from range

9 -110

8 -110



HIT: 22, -97 BUT ACTUALLY DAMAGE 22, -96





make speed a bit more expensive? Could literally do two in one go and speed is SO quick at tier 3







OK very good until tier 6, this is when you need arund 150 points for an upgrade. BUT, we need to really up the power after this point, because its 300 -> 600 -> 1200 -> 2400. About this time would be nice for some extras though, like the blast, the chain, the crit. Also things are getting so far away its a bit annoying to have to swim all the way over to them



I might just remove the entire RecipeiesUpgrade step because it is literally pointless to have. It adds no extra data. We could literally have all the data within the node, and that's it, we don't have to have this extra step of



each stage is given an ID at runtime, this is then used to track state, much like your current logic, which is a bit fucked but does work. OMG

Dictionary<ushort, UpgradeNodeState> IS PERFECT



UpgradeNodeSO holds DATA, how to construct the actual run instance

UpgradeNode is where the public List<ItemQuantity> would live Idk though I like the upgrade data being in the UpgradeNodeSO, then we just dynamically calculate it every time it is nicee to balance it when you're balancing.



So basically just make the UpgradeNodeSO and UpgradeNodeState which holds what stage a specific node is in, just simple runtime data. This can also then be read from and saved more easily if you actually store it somewhere instead of calculating the state dynamically everytime solely based on a list of unlocked upgrades



All we need to do is calculate the upgradeCost, that is all we need to calculate, nothing else. Its not complicated, remove all the fucking stupid places where you call the same code, then simply add it once, and you're done. Where do we calculate it!? IDK We could simply store it in UpgradeNode, and we generate it once when we init, and if we are balancing we just let it constantly generate new ones



HOW REWARDS? Will it just look at the cost of the highest node that is avaible and generate some random resources based on that?? it should be resources that are USEFULL for the player. THey open the chest, then come back, and then they can immediately upgrade lots of things with those resources. So following that logic, we should look at the current unlocked nodes, then randomly pick 3? Then just take resources from their required costs, put it in the chest and boom you've got a chest with good stuff. NO CANT JUST GET 3 RANDOM ONES, what if it picks the one upgrade that unreasonable price and then suddenly you're way ahead of what you should be. We should keep track of the Cost tier we have purchased last, then pick (non maxed) nodes around that cost tier +-1. That will make it balanced



8inch wide

7inch tall

4inch deep



OK How will we do event caves? They should be set templates, right? do we just make scriptable objects. Its kind of like an upgrade, but it can have different effects based on your choice. So one scriptble object per event cave, then it can have a list of effects, and the same effects that upgrades have, so then you can have negative stat modifiers etc



Decrease MAX SPEED BY A LOT,

You get gold a bit too late? Idk the cool stuff need gold but you're not really looking what you need so you kind of lose that pasing if they don't stumble upon it earlier



### Lucy Playtest #3

* Start in sub
* Stone -> stone density
* Fix tilemap tearing? no texture is creating lots of lines
* "Don't like how much he's bouncing off of stuff!!"
* Pitch should not reset after each category
* I can't go exploring far enough!
* Keeps going to the same spot
* I need more oxygen!! It's driving me a bit insane
* Forgot I had the brimstone
* Destroy shrine when block breaks under it? Or simply destroy it when you
* as pacing starts to slow down "It should kind of tell me how to proceed now"
* drops look like particles, not something I need to pickup
* Density upgrade didn't feel worth it because I felt like I was never running out of stone
* Ores still don't work really, maybe try and have them unique to a biome
* Outside of the upgrade tree sub consciously feels like it should be harder to upgrade. While the things close should be easier
* Make stone density not increase tile HP? Because it doesn't feel nice getting less strong, to compensate just scale the damage less quickly up

* For ore spawning, you should try two options: 
1. Ores spawn in specific biomes
2. Ores spawn in specific layers

For 1. how do we want to implement it? Pass biome ID into ore generator. Have ores specify which biome ID they can spawn into

Make copper more hard, make gold ALOT more hard. At a point where you're like wtf, I should not be here yet. Maybe even make the STONE somewhat harder for the biome you're not at 






 

