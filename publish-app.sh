#!/bin/bash

SKIP_DOCKER=0
SKIP_BUILD=0
IMG_NAME=$(cat IMAGE)
IMG_VER=$(cat VERSION)
USER=$(cat ~/.docker-user)
PASS=$(cat ~/.docker-pass)

usage() { 
	echo "Usage: $0 --image NAME_OF_IMAGE";
	echo "--image | the name of the image to build"
	echo "--skip-build | skips the build process for the app itself"
	echo "--skip-docker | skips the docker build process"
	exit 1; 
}

die() { echo "$*" >&2; exit 2; }  # complain to STDERR and exit with error
needs_arg() { if [ -z "$OPTARG" ]; then die "No arg for --$OPT option"; fi; }


while getopts t:-: OPT; do
  # support long options: https://stackoverflow.com/a/28466267/519360
  if [ "$OPT" = "-" ]; then   # long option: reformulate OPT and OPTARG
    OPT="${OPTARG%%=*}"       # extract long option name
    OPTARG="${OPTARG#$OPT}"   # extract long option argument (may be empty)
    OPTARG="${OPTARG#=}"      # if long option argument, remove assigning `=`
  fi
  case "$OPT" in
    skip-docker )   SKIP_DOCKER=1 ;;
    skip-build )    SKIP_BUILD=1 ;;
	image )			needs_arg; IMG_NAME="$OPTARG" ;;
    ??* )          die "Illegal option --$OPT" ;;  # bad long option
    ? )            exit 2 ;;  # bad short option (error reported via getopts)
  esac
done

if [ -z "$IMG_NAME" ]; then
  echo "please provide an image name!"
  usage
fi

if [[ "$SKIP_BUILD" -eq 0 ]]; then
  ./build-app.sh
fi

if [[ "$SKIP_DOCKER" -eq 0 ]]; then
  sudo ./build-docker.sh $IMG_NAME
fi

sudo docker login
sudo docker push $IMG_NAME:$IMG_VER
sudo docker push $IMG_NAME:latest

sudo docker run --rm -v $PWD:/workspace \
  -e DOCKERHUB_USERNAME=$USER \
  -e DOCKERHUB_PASSWORD=$PASS \
  -e DOCKERHUB_REPOSITORY=$IMG_NAME \
  -e README_FILEPATH=/workspace/README.md \
  peterevans/dockerhub-description:3
