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
  FINAL_ARGS="--name=$NAME"
fi

if [ "$INTERVAL" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --interval=$INTERVAL"
fi

if [ "$PASSWORD" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --password=$PASSWORD"
fi

if [ "$MQTT_HOST" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --mqtt-host=$MQTT_HOST --mqtt-port=$MQTT_PORT"
fi

if [ "$MQTT_HOSTID" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --mqtt-hostid=$MQTT_HOSTID"
fi

if [ "$MQTT_CLIENT_ID" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --mqtt-id=$MQTT_CLIENT_ID"
fi

if [ "$MQTT_USER" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --mqtt-user=$MQTT_USER"
fi

if [ "$MQTT_PASS" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS --mqtt-pass=$MQTT_PASS"
fi

if [ "$ADD_ARGS" != "" ]; then
  FINAL_ARGS="$FINAL_ARGS $ADD_ARGS"
fi

echo "running app with: '$FINAL_ARGS'"
dotnet /var/app/BackupMonitor.dll $FINAL_ARGS
