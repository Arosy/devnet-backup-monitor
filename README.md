# devnet-backup-monitor

### Description

A lightweight and dockerized application which allows the hassle free creation of backups for any folder structure on your system(s).

### Features

- The backup files created are simple zip archives, meaning there should be no problem in accessing them on any platform.
- Protect your precious backup files by specifying a password.
- Either run the container a single time or keep it running with the interval option.
- An integrated MQTT client to push messages to a configured broker whenever certain actions are performed by this container.

### Tested Platforms

- Ubuntu Desktop 22.04 amd64
- Ubuntu Desktop 20.04 amd64
- Raspberry Pi: 2/3/4
- Theoretically any: amd64, arm64 or arm/v7 system.

### Requirements

- [docker](https://docs.docker.com/get-docker/)
- Ideally, but not required: [docker-compose](https://docs.docker.com/compose/install/)

##### `Install on ubuntu/debian/raspbian`
```bash
# install docker only
sudo apt-get update; sudo apt-get install docker.io -yq

# OR! install with compose
sudo apt-get update; sudo apt-get install docker.io docker-compose -yq
```

### Configuration

#### Environment Variables

| Key | Value | Description |
|:-----------------:|:----------------------------------------------:|:------------------------------------------------------------------------------------------------------------------------------------:|
| NAME | `empty` | The name to use for generated backup files. By default the name will be auto generated based on the current UTC time ticks. |
| PASSWORD | `empty` | Optionally a password to use for the generated backup file. |
| INTERVAL | 0 | Optionally specify an interval in seconds to shedule reoccuring backup creations. This prevents the container from exiting when a backup has created unless theres an error. |
| RUN_AT_TIME | `empty` | Alternatively, but optionally you can specify a time in HH:MM:SS format when this instace will create backup files.
| WITH_DATE | 0 | If set to 1 the backup files will contain the current date (Year,Month,Day) in its file name. |
| WITH_TIME | 0 | If set to 1 the backup files will contain the current time (Hour,Minute,Second) in its file name. |
| MQTT_HOST | `empty` | Specify the hostname or ip address of your mqtt broker. If its empty the MQTT feature will be disabled and other params starting with *MQTT_* will be ignored. |
| MQTT_PORT | 1883 | Specify the port of your mqtt broker. This property is only used when the **MQTT_HOST** variable is not empty. |
| MQTT_HOSTID | `empty` | A domain name like value to identify this instance in MQTT messages. (monitor/+/backup/#). This property is only used when the **MQTT_HOST** variable is not empty. |
| MQTT_ID | `empty` | Specify your own client id which this instance will use or leave it empty to auto generate. This property is only used when the **MQTT_HOST** variable is not empty. |
| MQTT_USER | `empty` | Specify a username used for authentification for your mqtt broker. This property is only used when the **MQTT_HOST** variable is not empty. |
| MQTT_PASS | `empty` | Specify a password used for authentification for your mqtt broker. This property is only used when the **MQTT_HOST** variable is not empty. |
| ARCHIVE_TYPE | `empty` | Specify one of the supported types (currently: ftp or scp) to enable the automatic transfer when the backup file creation has completed. |
| ARCHIVE_ENDPOINT | `empty` | Specify the hostname / ip address with the target port in a combo like: *example.com:21* or *123.123.123.123:21*. If you're using the default port you can just specify the hostname / ip address. |
| ARCHIVE_USER | `empty` | Specify the username required for authentification on the remote server. |
| ARCHIVE_PASS | `empty` | Depending on your configuration you may or may not have to fill this field, however this is used for authentification aswell. |
| ARCHIVE_PATH | `empty` | Specify a relative path on the archive server which is used as base path for the automatic uploaded backup files. |

### Get it up and running

##### `docker run`

```bash
$ docker run -d --rm --name="backup_monitor" \
                     -v "/some/path/to/my-dir:/backup/my-dir" \
                     -v "/some/path/to/my-dir2:/backup/my-dir2" \
                     -v "{HOME}/backups:/output" \
                     -e NAME="my-backup" inquinator/devnet-backup-monitor:latest
```

##### `docker-compose.yml`
```yml
version: '3'
services:
  backup-monitor:
    ## Only use the restart option when running the container with INTERVAL specified,
    ## otherwise the container will exit upon completing its task.
    #restart: unless-stopped
    container_name: backup-monitor
    image: inquinator/devnet-backup-monitor:latest
    environment:
      ## Prefix your backup files with a specific name. Please beware that only letters, numbers and certain symbols
      ## are allowed. The allowed symbols are: .-_()[]
      - NAME=my-backup
      ## Optionally you can specify an interval to re-run the backup creation every x seconds.
#       - INTERVAL=43200
      ## Alternatively, but optionally you can specify a time in HH:MM:SS format when this instace will create backup files.
      ## In this example backups will be created everyday at 02:00 in the morning.
      ## Also check the volume section to ensure the timezone is correctly applied.
#       - RUN_AT_TIME=02:00:00
      ## Optionally you can specify a password to protect your backup files.
#       - PASSWORD=test123
      ## Includes the current local date in the backup file name as such YYYY-MM-DD
       - WITH_DATE=1
      ## Includes the current local time in the backup file name as such HH-MM-SS
       - WITH_TIME=1
      ## The domain name, hostname or ip address of your MQTT broker. If this variable is empty or
      ## unspecified the other MQTT variables will be ignored.
#       - MQTT_HOST=192.168.2.10
      ## Set the port of your MQTT broker to which the client connects, if enabled.
#       - MQTT_PORT=1883
      ## Set an identifier for this instance when MQTT is enabled. This value will be used in the topic name
      ## like so: /monitor/MQTT_HOSTID/backup/..
#       - MQTT_HOSTID=my-test-device
      ## Specify the archive type when automatic upload to remote storage is desired.
      ## Currently supported types: none, scp
      ## If the type is set to none or not specified at all the other ARCHIVE variables will be ignored.
#       - ARCHIVE_TYPE=scp
      ## The endpoint / address of your remote storage server.
#       - ARCHIVE_ENDPOINT=storage.my-awesome-host.com
      ## The user required for authentification on your remote storage.
#       - ARCHIVE_USER=user
      ## The password which may be required for authentification on your remote storage.
#       - ARCHIVE_PASS=password
      ## A relative path which should be used as base directory for file uploads on your remote storage.
#       - ARCHIVE_PATH=~/
    volumes:
      ## Basically you can add as many directories as you desire within the */backup/* directory,
      ## however they should be mounted as individual directories and **not directly** as */backup/*
      - /some/path/to/my-dir:/backup/my-dir
      - /some/path/to/my-dir2:/backup/my-dir2
      ## The output directory on the host machine where the backup files should be stored.
      - {HOME}/backups:/output
      ## Only required when using the variable `RUN_AT_TIME` to ensure the timezone within the container matches
      ## the one specified on your host system.
#      - /etc/localtime:/etc/localtime:ro
```

### Examples

##### MQTT Message when backup status changes:
```json
{
   "Date":"2023-05-13T13:15:45.8841021+00:00",
   "Name":"backup-2023-5-13-13-15-45.zip",
   "Paths":[
      "/backup/my-dir2",
      "/backup/my-dir1"
   ],
   "Status":"FileCreated",
   "SizeInBytes":2122423,
   "IsEncrypted":true,
   "IsAutoTransfer":true,
   "IsAutoRun":false,
   "Error":""
}
```

### Links

- [Dockerhub](https://hub.docker.com/r/inquinator/devnet-backup-monitor)

- [GitHub](https://github.com/Arosy/devnet-backup-monitor)

### TODO

- Implement more archive connectors such as: ftp, gdrive, etc.

### Contribute

- Feel free to submit any changes you see fit or do whatever you want, because its MIT licensed.

### Changelog

**v0.0.4**
- added support for more platforms: arm64, arm/v7
- improved source file ingestion which should be more reliable now, especially when a single file in a directory is inaccessible.

**v0.0.3**
- added the `RUN_AT_TIME` variable to specify a target time when to create backups instead of using the `INTERVAL` variable.

**v0.0.2**
- improved error handling especially for the file transfer feature.
- added support for uploading backup files to a remote server automatically.
- fixed a minor issue with variables in the dockerfile.
- extended mqtt implementation.

**v0.0.1**
- initial upload.