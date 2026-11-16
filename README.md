A source code for mod of mine [New Firearms](https://www.nexusmods.com/scavprototype/mods/122)

Setting up compile:

Export CU_PATH to be path to game folder
E.g. C:\Program Files (x86)\Steam\steamapps\common\Casualties Unknown\

You need a release with Together

`dotnet build NewFirearms/`

It would compile gunminigame And newfirearms

Symbolic link the
GunMinigame/bin/Debug/GunMinigame.dll -> CU_PATH/BepInEx/plugins/GunMinigame.dll
GunMinigame/bin/Debug/GunMinigame.pdb -> CU_PATH/BepInEx/plugins/GunMinigame.pdb
NewFirearms/bin/Debug/NewFirearms.dll -> CU_PATH/BepInEx/plugins/NewFirearms.dll
NewFirearms/bin/Debug/NewFirearms.pdb -> CU_PATH/BepInEx/plugins/NewFirearms.pdb
Ship -> CU_PATH/BepInEx/plugins/NewFirearms/
GunMinigame/Resources -> CU_PATH/BepInEx/plugins/NewFirearms/Resources/
GunMinigame/Guns -> CU_PATH/BepInEx/plugins/NewFirearms/Guns/

Move out the NewFirearms.dll to test standalone version

smth like this

The normal description is on nexus

