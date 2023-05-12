#!/bin/bash
IMG_NAME=$(cat ./IMAGE)
IMG_VER=$(cat ./VERSION)


usage() { 
	echo "Usage: $0 --image NAME_OF_IMAGE";
	echo "--image | the name of the image to build"
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
	image )		   needs_arg; IMG_NAME="$OPTARG" ;;
    ??* )          die "Illegal option --$OPT" ;;  # bad long option
    ? )            exit 2 ;;  # bad short option (error reported via getopts)
  esac
done

echo $IMG_NAME

if [ "$IMG_NAME" == "" ]; then
  echo "no image name was specified!"
  exit 1
fi

if [ "$IMG_VER" == "" ]; then
  echo "no image version was specified!"
  exit 1
fi

if [ ! -f "./Dockerfile" ]; then
  echo "cannot find docker build script!"
  exit 1
fi

docker build -t $IMG_NAME:latest . --no-cache
docker tag $IMG_NAME:latest $IMG_NAME:$IMG_VER
