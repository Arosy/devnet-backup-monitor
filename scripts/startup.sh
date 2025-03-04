#!/bin/bash

if [ ! -d "/var/app" ]; then
  echo "cannot find local app directory .."
  exit 1
fi

if [ ! -f "/var/app/BackupMonitor.dll" ]; then
  echo "cannot find app binaries .."
  exit 1
fi

## uncomment when required, useful for debugging the supplied arguments
#echo "running app with: '$FINAL_ARGS'"
pushd /var/app
dotnet BackupMonitor.dll