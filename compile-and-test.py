# Some kind of unit testing
# Really, i feel like im reinventing the wheel here
import os

path = "/home/rushell/Documents/CU"
if not os.path.exists(path + "/CasualtiesUnknown_Data/Managed/Assembly-CSharp.dll"):
    path = os.getenv("CU_PATH")

if not os.path.exists(path + "/BepInEx/plugins/RshLib.dll"):
    os.rename(path + "/BepInEx/plugins/RshLib.dll~", path + "/BepInEx/plugins/RshLib.dll")
if not os.path.exists(path + "/BepInEx/plugins/CUCoreLib.dll"):
    os.rename(path + "/BepInEx/plugins/CUCoreLib.dll~", path + "/BepInEx/plugins/CUCoreLib.dll")
if not os.path.exists(path + "/BepInEx/plugins/Together/CasualtiesMP.dll"):
    os.rename(path + "/BepInEx/plugins/Together/CasualtiesMP.dll~", path + "/BepInEx/plugins/Together/CasualtiesMP.dll")
if not os.path.exists(path + "/BepInEx/plugins/WarmBoot.dll"):
    os.rename(path + "/BepInEx/plugins/WarmBoot.dll~", path + "/BepInEx/plugins/WarmBoot.dll")
if os.path.exists(path + "/BepInEx/patchers/KrokMP/autoupdater_patcher.dll"):
    os.rename(path + "/BepInEx/patchers/KrokMP/autoupdater_patcher.dll", path + "/BepInEx/patchers/KrokMP/autoupdater_patcher.dll~")
if os.path.exists(path + "/BepInEx/plugins/Together/Multiupdater.dll"):
    os.rename(path + "/BepInEx/plugins/Together/Multiupdater.dll", path + "/BepInEx/plugins/Together/Multiupdater.dll~")

if 0 != os.system("dotnet build GunMinigame/GunMinigame.csproj"):
    print("Compilation of GunMinigame failed")
    exit()
if 0 != os.system("dotnet build NewFirearms/NewFirearms.csproj"):
    print("Compilation of NewFirearms failed")
    exit()

os.rename(path + "/BepInEx/plugins/Together/CasualtiesMP.dll", path + "/BepInEx/plugins/Together/CasualtiesMP.dll~")

print("Testing RshLib configuration")
os.rename(path + "/BepInEx/plugins/CUCoreLib.dll", path + "/BepInEx/plugins/CUCoreLib.dll~")
os.system(f'WINEDLLOVERRIDES="winhttp=n,b" timeout 14s wine {path}/CasualtiesUnknown.exe -logFile /tmp/log.tmp &> /dev/null')
print("Reading log file")
with open("/tmp/log.tmp", 'r') as f:
    log = f.read()
if "Fatal" in log:
    print("Fatal error detected in log file on RshLib test")
    exit()
if "Error" in log:
    print("Error detected in log file on RshLib test")
    exit()
if "Warning" in log:
    print("Warning detected in log file on RshLib test")
else:
    print("No warnings detected in log file on RshLib test")

print("Testing CUCoreLib configuration")
os.rename(path + "/BepInEx/plugins/CUCoreLib.dll~", path + "/BepInEx/plugins/CUCoreLib.dll")
os.rename(path + "/BepInEx/plugins/RshLib.dll", path + "/BepInEx/plugins/RshLib.dll~")
os.system(f'WINEDLLOVERRIDES="winhttp=n,b" timeout 14s wine {path}/CasualtiesUnknown.exe -logFile /tmp/log.tmp &> /dev/null')
print("Reading log file")
with open("/tmp/log.tmp", 'r') as f:
    log = f.read()
if "Fatal" in log:
    print("Fatal error detected in log file on CUCoreLib test")
    exit()
if "Error" in log:
    print("Error detected in log file on CUCoreLib test")
    exit()
if "Waning" in log:
    print("Warning detected in log file on CUCoreLib test")
else:
    print("No warnings detected in log file on CUCoreLib test")

print("Test passed")
os.rename(path + "/BepInEx/plugins/Together/CasualtiesMP.dll~", path + "/BepInEx/plugins/Together/CasualtiesMP.dll")
