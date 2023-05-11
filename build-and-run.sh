#!/bin/bash
# build the app binaries itself
./build-app.sh
# build the docker container
./build-docker.sh
# run the locally build container
./run-docker.sh