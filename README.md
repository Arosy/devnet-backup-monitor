# devnet-backup-monitor

### Description

A lightweight and dockerized application which allows the hassle free creation of backups for any folder structure on your system(s).


### Features

- The backup files created are simple zip archives, meaning there should be no problem in accessing them on any platform.
- Protect your precious backup files by specifying a password.
- Either run the container a single time or keep it running with the interval option.
- An integrated MQTT client to push messages to a configured broker whenever certain actions are performed by this container.

### Configuration

#### Environment Variables

The configuration of your instance is done by using environment variables:

| Key | Value | Description |
|:-----------------:|:----------------------------------------------:|:------------------------------------------------------------------------------------------------------------------------------------:|
| NAME | `empty` | The name to use for generated backup files. By default the name will be auto generated based on the current UTC time ticks. |
| PASSWORD | `empty` | Optionally a password to use for the generated backup file. |
| INTERVAL | 0 | Optionally specify an interval in seconds to shedule reoccuring backup creations. This prevents the container from exiting when a backup has created unless theres an error. |
| WITH_DATE | 0 | If set to 1 the backup files will contain the current date (Year,Month,Day) in its file name. |
| WITH_TIME | 0 | If set to 1 the backup files will contain the current time (Hour,Minute,Second) in its file name. |
| MQTT_HOST | `empty` | Specify the hostname or ip address of your mqtt broker. If its empty the MQTT feature will be disabled and other params starting with *MQTT_* will be ignored. |
| MQTT_PORT | 1883 | Specify the port of your mqtt broker. This property is only used when the **MQTT_HOST** is set. |
| MQTT_HOSTID | `empty` | A domain name like value to identify this instance in MQTT messages. (monitor/+/backup/#) |
| MQTT_ID | `empty` | Specify your own client id which this instance will use or leave it empty to auto generate. |
| MQTT_USER | `empty` | Specify a username used for authentification for your mqtt broker. |
| MQTT_PASS | `empty` | Specify a password used for authentification for your mqtt broker. |

### Get it up and running

##### `docker run`

```bash
# You may want to change SESSION_NAME, ADMIN_PASSWORD or host-volume
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
#    environment:
#       - NAME=my-backup
      ## Optionally you can specify an interval to re-run the backup creation every x seconds.
#       - INTERVAL=43200
      ## Optionally you can specify a password to protect your backup files.
#       - PASSWORD=test123
      ## Includes the current local date in the backup file name as such YYYY-MM-DD
#       - WITH_DATE=1
      ## Includes the current local time in the backup file name as such HH-MM-SS
#       - WITH_TIME=1
    volumes:
      ## Basically you can add as many directories as you desire within the */backup/* directory,
      ## however they should be mounted as individual directories and **not directly** as */backup/* 
      - /some/path/to/my-dir:/backup/my-dir
      - /some/path/to/my-dir2:/backup/my-dir2
      ## The output directory on the host machine where the backup files should be stored.
      - /some/path/to/backups:/output
```

### Links

- TODO

### Changelog

**v0.0.2**
- fixed an issue where the specified password was not applied to backup files.

**v0.0.1**
- initial upload.