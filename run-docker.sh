#!/bin/bash
IMG_NAME="inquinator/devnet-backup-monitor"

if [ ! -z "$1" ]; then
  IMG_NAME="$1"
fi

docker run --env-file ./.env.dev --rm -t $IMG_NAME:latest
