Add description to the upgrades?



* Need some kind of tutorial / reminder of using the brimstone ability
* Test how the game feels with block break sounds - EH idk its more satisfying but the sound has to be really good





* Add exploration entities



* Add biome essence to recipes
* Implement chest loot system
* Implement buff loot system
* Implement events





Things that are good:

First upgrade with the player damage and upgrade fixing is nice. You can do it in one oxygen run. Okay the first 10 minutes or so is good now. Up until the lazer blast and oxygen length. Now next would be to think about how we can go from here...

 

consider the fact that players could be going down, have some kind of bedrock layer. Edit world ore spawns in GetOreDefinitions to start from bedrock etc....



For the nodes its important that whatever its connected is somewhat related to the node

POLISH NEEDED FOR DEMO:

* Upgrade progress should look animated / cooler, more exciting!!
* Use more particles in the ui, even when its just stationary, like extra cool subtle things around a big upgrade node



Steamdeck notes:

* Icons not updating correctly for controller
* Should a start menu with settings to change full screen mode etc...
* Also the block texture seems zoomed in... - WHY!? This also happens in windows build

**Smaller visual stuff:**

* Don't let submarine spawn inside a rock, and don't let anything "block" the sub, like plants spawn in front of it
* Make popup fade out instead of disappear instant
* Items go From bob to PC? -> Visually show it go from bob to pc, and make the PC bounce or something (show that its receiving it). I've tried this but its really hard to have it look good!!!!
* New color node with one upgrade but not getting one upgrade
* Hitstop when buying a node!?
* Change popup particle colour to match the purchase particle color
* Animate interaction (fade in, move up slightly)
* Screenshake?
* Add MORE POLISH. NOTHING should instantly appear, it should have MOVEMENT

UX:

* Not sure what to do at the start, needs a bit more handholding. Arrow pointing at upgrade machine!?
* Forget to use lazer blast \& what button it needs to activate
* UI Doesn't tell you anything, it needs to be clear how to progress further!!
* A machine that shows the biome artifacts and which one's you've gotten

Misc:

* Should not fix the upgrade panel from the upgrade panel
* Cactus particle effect?? When you turn into one
* MAKE SETTINGS (good for debugging, like disabling the background, fullscreen modes etc
* ^^^^
* Chest should explode into lil pickups which then give you that same pickup sound
* Big node doesn't really look like you can buy it

Coding:

* The other upgrades, like the block oxygen, lazer blast, lazer chain, make them better, it feels wrong, it just costs like 2 iron to buy, its not satisfying, make it cost somewhat mored
* crit damage?
* Biome placement should not change with same seed -> Maybe fixed it need to check!!



PACING:

* Make movement slower on the next layer, higher up -> More dense. Could have the same with oxygen!!

ADD MORE STUFF!

* Add exploration entity content



FOR POLISED DEMO:

* Needs proper game start animation
* Needs music
* Some kind of animation when going into the submarine?
* The pitch is a bit too loud and not very soothing
* Decide what the fuck you want to do with the stone upgrade
* Lazer chain is a level 2 thing
* Add upgrades that you'll be able to get for level 2
* Sub UI needs to be updated. Zone ui should show appropriate resources
* Make lazer not go so thick.
* Make you go much slower in the next level, if players try to go their they'll be like what the fuck
* Environment should feel ALIVE, it feels DEAD. Make fish swim around, little blobs here and there. Make the plants move. MAKE IT LOOK ALIVE. POLISH THE ENVIRONMENT 
* Hide exploration structures inside submarin
* Big button should be dark green when you can't purchase
* lazer can go into blocks if player gets positioned correctly

For web build

* Can't fullscreen properly
* Loop audio is weird on web build it leaves a tiny gap where there is no sound. It's a thing https://discussions.unity.com/t/seamless-audio-looping-in-webgl/1497212/3
* A lot of weird tilemap tearing
* All the start of the sounds seem to be getting cut. Same here, first 1024 samples of a sound gets cut and fucked with, so just have a slight pause before the audio actually starts playing
