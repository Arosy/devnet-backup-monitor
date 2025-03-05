# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/runtime:8.0
ARG TARGETPLATFORM
# setup basic variables
ENV DEBUG=0

RUN apt-get update
RUN apt-get install sshpass -yq
    
# copy the actual binaries ..
RUN mkdir /var/app
COPY ./builds/backup-monitor/$TARGETPLATFORM/ /var/app/

# dummy file so the app knows its within a container
RUN touch /var/app/.docker

# register the startup script and make it executable
COPY ./scripts/startup.sh /bin/startup.sh
RUN chmod +x /bin/startup.sh

# runs the desired script everytime the container boots
ENTRYPOINT "bin/startup.sh"
