#!/bin/bash
IMG_NAME="inquinator/devnet-backup-monitor"

if [ ! -z "$1" ]; then
  IMG_NAME="$1"
fi

docker run  --rm --env-file ./dev.env -v ${HOME}/backup-test/my-dir:/backup/my-dir -v ${HOME}/backup-test/my-dir2:/backup/my-dir2 -v ${HOME}/backup-test:/output -t $IMG_NAME:latest
