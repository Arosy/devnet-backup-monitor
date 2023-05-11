# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/runtime:6.0.7-jammy-amd64

# setup basic variables
ENV NAME="backup" \
    INTERVAL=0 \
    PASSWORD="" \
    MQTT_HOST="" \
    MQTT_PORT=1883 \
    MQTT_HOSTID="" \
    MQTT_CLIENT_ID="" \
    MQTT_USER="" \
    MQTT_PASS="" \
    ADD_ARGS=""
    
# copy the actual binaries ..
RUN mkdir /var/app
COPY ./builds/backup-monitor/linux-x64/ /var/app/

# register the startup script and make it executable
COPY ./scripts/startup.sh /bin/startup.sh
RUN chmod +x /bin/startup.sh

# runs the desired script everytime the container boots
ENTRYPOINT "bin/startup.sh"
