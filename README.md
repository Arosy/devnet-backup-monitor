# Project Overview

## Description

A lightweight and dockerized .net application which allows highly customizeable and easy creation of backups for any folder structure on your system(s).

## Features

- The backup files created are simple zip archives, meaning there should be no problem in accessing them on any platform.
- Protect your precious backup files by specifying a password.
- Either run the container a single time or keep it running with the interval option.
- An integrated MQTT client to push messages to a configured broker whenever certain actions are performed by this container.

## Requirements

- [docker](https://docs.docker.com/get-docker/)
- Ideally, but not required: [docker-compose](https://docs.docker.com/compose/install/)
- net8.0 when building from source

### Tested Platforms

| OS | Version | Arch |
|:--------------------------------------:|:----------------------:|:-------------------:|
| Kubuntu Desktop | 22.10 | amd64 |
| Ubuntu Desktop | 24.04 | amd64 |
| Ubuntu Desktop | 22.04 | amd64 |
| Ubuntu Desktop | 20.04 | amd64 |
| Raspbian OS | Any | armhf, aarch64 |


### `Install on ubuntu/debian/raspbian`
**install docker only**
```bash
sudo apt-get update && sudo apt-get install docker.io -yq
```
**OR! install with compose**
```bash
sudo apt-get update && sudo apt-get install docker.io docker-compose -yq
```

## Compile from sources

Please note that you need access to the `dotnet` command line utility, before you're able to compile the source code.

To build the app from its source code you simply need to run `./build-app.sh` located in the root repository directory.

## Configuration

### Backup Template

With the **0.1.0** update backups are now described with yml templates to precisely configure each scenario and yes, you can create as many yml templates as you desire.

```yml
name: test_backup.zip # the name of your backup archive created by this template.
inputs: # a collection of paths to describe which files or directory structures should be processed.
  - /home/arosy/ # either use a 1:1 approach
#  - /mounted/dir/with/other/name # or specify a drive path which is mounted into the container which can be different from the host.
output: /tmp/backups # in this directory a file with the given name will be created. same rules as for input applies, these paths just need to be accessible from the container.
#schedule: # optionally specify a schedule to enable that this template runs automatically.
#  week: 3 # when specified, this job will run once a month
#  day: "monday;wednesday;saturday" # when specified, this job will run on that weekday or supply multiple days where each value is seperated with a ;
#  hour: 4
#  minute: 20
#archive: # optionally encrypt your backup file with a password.
#  pass: mypasswd
#upload: # optionally specify an online storage where the output of this template will be stored.
#  mode: "scp" # 'scp'
#  host: user.your-storagebox.de
#  user: myusername
#  pass: somestrongpass
#  path: /backups/test # this directory needs to exist on the server, otherwise the upload will result in a file.
#mqtt: # optionally specify mqtt (currently WIP)
#  host: myhost
#  port: 1883
#  user: myuser
#  pass: mypass
#  id: my.test.backup
exclusions: # optionally, but recommend exclude any directory or file with support for wildcards for names and extensions.
  - ":" # special case when wine is installed
  - "/trash/" # recycle bin on linux
  - .cache # caching directory used by many apps
  - /snap # install directory for snap packages
settings: # advanced settings
  try_read: false # when set to true, a read operation is performed on each detected input file, to ensure files are readable.
  overwrite: true # when set to false, an error will be thrown when output backup file already exists.
  path_mode: "absolute" # 'absolute' or 'relative'

```

## Get it up and running

### `docker run`

**see the docker-compose.yml example for annotations**

```bash
$ docker run -d --rm --name="backup_monitor" \
                     -v "./templates:/templates:ro" \
                     -v "/home/arosy:/home/arosy:ro" \
                     -v "/etc/localtime:/etc/localtime:ro" \
                     -v "/tmp/backups:/tmp/backups:rw" \
                     inquinator/devnet-backup-monitor:latest
```

### `docker-compose.yml`
```yml
version: '3'
services:
  backup-monitor:
    ## Only use the restart option when running the container with INTERVAL specified,
    ## otherwise the container will exit upon completing its task.
    #restart: unless-stopped
    container_name: backup-monitor
    image: inquinator/devnet-backup-monitor:latest
    volumes:
      ## its possible to define as many yml templates as you desire and let them be managed by the container.
      - ./templates:/templates:ro
      ## specify your required input directories which are then read-only accessible by the container to
      ## zip them into an archive.
      - /home/arosy:/home/arosy:ro
      ## last but not least, we need to specify the output directory.
      - /tmp/backups:/tmp/backups:rw
      ## to ensure the timezone within the container matches the one specified on your host system.
      - /etc/localtime:/etc/localtime:ro
```

## Links

- [Dockerhub](https://hub.docker.com/r/inquinator/devnet-backup-monitor)

- [GitHub](https://github.com/Arosy/devnet-backup-monitor)

## TODO

- Implement more archive connectors such as: ftp, gdrive, etc.

- Add more QoL.

- More cleanup of obsolete code.

## Contribute

- Feel free to submit any changes you see fit or do whatever you want, because its MIT licensed.

## Changelog

- see **CHANGELOG.md** in the root directory.
