# Guard pistol drops

In the full game, a guard drops a `pistol` with its remaining 9mm ammunition. When rounds remain, the pistol has one chambered round and the rest in its `smallmagazine`. At zero rounds, the pistol has an empty magazine. The game keeps its original conditions for spawning the separate spare magazine and other loot.

Dropped pistols start at a random condition between 50% and 90%. NewFirearms applies the ammunition during initialization. In Together, the host initializes the drop and sends the gun state through the usual synchronization.
