#!/bin/bash
IMG_NAME="inquinator/devnet-backup-monitor"
IMG_VER=$(cat ./VERSION)

if [ ! -z "$1" ]; then
  IMG_NAME="$1"
fi

docker build -t $IMG_NAME:latest . --no-cache
docker tag $IMG_NAME:latest $IMG_NAME:$IMG_VER
