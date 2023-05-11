#!/bin/bash
# CONFIGURE THE OUTPUT DIRECTORY WHERE EACH ARCHITECTURE IS BEING DEPLOYED IN ITS OWN DIRECTORY
BINARY_OUT="./builds/backup-monitor"

# CONFIGURE THE PROJECT FILE WHICH IS USED FOR COMPILATION AND OTHER ACTIONS IN THIS SCRIPT
PROJ_FILE="./src/BackupMonitor.csproj"

# THE DOTNET VERSION USED FOR COMPILATION
NETVER="net6.0"

### DO NOT MODIFY VARIABLES BELOW !!!
PARAMS=""
OS_PREFIX=""
### DO NOT MODIFY VARIABLES ABOVE !!!


if [ "$BINARY_OUT" = "" ]; then
  echo "CANNOT CONTINUE WITHOUT OUTPUT DIRECTORY"
  exit 1
fi

if [ "$PROJ_FILE" = "" ] || [ ! -f "$PROJ_FILE" ]; then
  echo "CANNOT CONTINUE WITHOUT PROJECT FILE"
  exit 1
fi

#echo "clearing old builds, hang tight .."
#rm -f -r $BINARY_OUT*

if [ "$4" == "develop" ]; then
  PARAMS="--version-suffix dev"
elif [ "$4" == "staging" ]; then
  PARAMS="--version-suffix beta"
fi

if [ -d "/c/" ]; then
  OS_PREFIX="win-x64"
else
  OS_PREFIX="linux-x64"
fi

echo "detected '$OS_PREFIX' as host system"

# LINUX
dotnet publish $PROJ_FILE -c Debug -r linux-x64 -f $NETVER --output "$BINARY_OUT-debug/linux-x64" $PARAMS
dotnet publish $PROJ_FILE -c Release -r linux-x64 -f $NETVER --output "$BINARY_OUT/linux-x64" $PARAMS

# OSX
dotnet publish $PROJ_FILE -c Debug -r osx-x64 -f $NETVER --output "$BINARY_OUT-debug/osx-x64" $PARAMS
dotnet publish $PROJ_FILE -c Release -r osx-x64 -f $NETVER --output "$BINARY_OUT/osx-x64" $PARAMS

# WINDOWS
dotnet publish $PROJ_FILE -c Debug -r win-x64 -f $NETVER --output "$BINARY_OUT-debug/win-x64" $PARAMS
dotnet publish $PROJ_FILE -c Release -r win-x64 -f $NETVER --output "$BINARY_OUT/win-x64" $PARAMS




