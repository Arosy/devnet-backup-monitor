#!/bin/bash

if [ ! -d "/var/app" ]; then
  echo "cannot find local app directory .."
  exit 1
fi

if [ ! -f "/var/app/BackupMonitor.dll" ]; then
  echo "cannot find app binaries .."
  exit 1
fi

FINAL_ARGS=""

if [ "$NAME" != "" ]; then
  FINAL_ARGS="--name=$NAME "
fi

if [ "$INTERVAL" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--interval=$INTERVAL "
fi

if [ "$PASSWORD" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--password=$PASSWORD "
fi

if [ "$MQTT_HOST" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--mqtt-host=$MQTT_HOST --mqtt-port=$MQTT_PORT "
fi

if [ "$MQTT_HOSTID" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--mqtt-hostid=$MQTT_HOSTID "
fi

if [ "$MQTT_CLIENT_ID" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--mqtt-id=$MQTT_CLIENT_ID "
fi

if [ "$MQTT_USER" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--mqtt-user=$MQTT_USER "
fi

if [ "$MQTT_PASS" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--mqtt-pass=$MQTT_PASS "
fi

if [[ "$WITH_DATE" -eq 1 ]]; then
  FINAL_ARGS="$FINAL_ARGS--with-date "
fi

if [[ "$WITH_TIME" -eq 1 ]]; then
  FINAL_ARGS="$FINAL_ARGS--with-time "
fi

if [ "$ARCHIVE_TYPE" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--archive-type=$ARCHIVE_TYPE "
fi

if [ "$ARCHIVE_ENDPOINT" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--archive-endpoint=$ARCHIVE_ENDPOINT "
fi


if [ "$ARCHIVE_USER" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--archive-user=$ARCHIVE_USER "
fi


if [ "$ARCHIVE_PASS" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--archive-pass=$ARCHIVE_PASS "
fi


if [ "$ARCHIVE_PATH" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--archive-path=$ARCHIVE_PATH "
fi


if [ "$RUN_AT_TIME" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS--run-at=$RUN_AT_TIME"
fi

## uncomment when required, useful for debugging the supplied arguments
#echo "running app with: '$FINAL_ARGS'"
dotnet /var/app/BackupMonitor.dll $FINAL_ARGS
