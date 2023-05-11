# devnet-backup-monitor
An Application written in C# to easily manage the creation and transfers of backups on linux machines.


### Configuration

#### Environment Variables

The configuration of your instance is done by using environment variables:

| Key | Value | Description |
|:-----------------:|:----------------------------------------------:|:------------------------------------------------------------------------------------------------------------------------------------:|
| NAME | backup | The name to use for generated backup files. |
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

#### Customization

- By default the container will run only once, that said you should re-run the container whenever you want to have a backup from the mounted source directories.

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
    image: inquinator/devnet-auth-monitor:latest
    environment:
      - NAME=my-backup
      ## Optionally you can specify an interval to re-run the backup creation every x seconds.
      #- INTERVAL=43200
      ## Optionally you can specify a password to protect your backup files.
      #- PASSWORD=test123
    volumes:
      ## Basically you can add as many directories as you desire within the */backup/* directory,
      ## however they should be mounted as individual directories and **not directly** as */backup/* 
      - /some/path/to/my-dir:/backup/my-dir
      - /some/path/to/my-dir2:/backup/my-dir2
      ## The output directory on the host machine where the backup files should be stored.
      - {HOME}/backups:/output
```