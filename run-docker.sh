#!/bin/bash
IMG_NAME="inquinator/devnet-backup-monitor"

if [ ! -z "$1" ]; then
  IMG_NAME="$1"
fi

docker run --rm -v /home/arosy:/home/arosy:ro -v /tmp/backups:/tmp/backups:rw -v ./templates:/templates:ro -t $IMG_NAME:latest
