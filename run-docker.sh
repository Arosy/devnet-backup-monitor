#!/bin/bash
IMG_NAME="inquinator/devnet-backup-monitor"

if [ ! -z "$1" ]; then
  IMG_NAME="$1"
fi

docker run  --rm --env-file ./.env.dev -v ./scripts:/backup/scripts -t $IMG_NAME:latest
