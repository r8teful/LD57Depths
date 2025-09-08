SIMPLIFY
Simplified inventory **DONE**
Change upgrade so that each stat is increased in one tree **DONE**
Implement movement of ship to predetermined parts in each zone **DONE**
Implement ship control UI
	- Make general overview with buttons and tabs **DONE**
	- Implement place holder zone data  **DONE**
	- Make upgrade screen **DONE**
	- implement move screen & multiplayer confirmation **DONE**
	- Finish & Fix overview screen

##Ship view details:
	1. Overview, shows where you are, what level the ship is, and what needs upgrading
	2. Sub fix/upgrade screen
		- Sub level and fixing is a communal variable, it should be the same for all clients
		- Fixing should be a progression bar with stages, clicking on a button will contrubute to that fix
	3. Sub Move screen
		- Shows all trench zones, each zone should have information about their unique/most common ores
		- For multiplayer, moving needs confirmation from all players. The sequence goes like this
		1. Player initiales ship movement and a global message is sent to all clients, the sub computer starts blinking
		2. When another client interacts with the computer, they get the information screen about what zone will be traveled to, they can then accept or deny
		3. If all clients accept, the ship moves, if any of them deny, we tell all clients who declined, and move on