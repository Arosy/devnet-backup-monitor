# Changelog

**v0.1.0**
- added: backups are now **configured through \*.yml files** instead of only directory mounts.
- added: backups may now define file / directory exclusions.
- added: feature to ensure that broken archives / temporary files are not stockpiling.
- added: run multiple templates through a single container instance, instead of creating an instance per scenario.
- updated: replaced (DotNetZip)[https://www.nuget.org/packages/dotnetzip/] with (DotNetZip.Original)[https://www.nuget.org/packages/DotNetZip.Original/] which fixes a severe (CVE-2024-48510)[https://nvd.nist.gov/vuln/detail/CVE-2024-48510] security issue.
- updated: net8.0 instead of net6.0
- updated: vastly improved application performance

**v0.0.7**
- improved some logging and internal code behaviour, especially for locked files.

**v0.0.6**
- fixed an issue where packing files or directories which won't allow compression, e.g. due being in use or locked.
- fixed an issue with line idention in the example compose file.

**v0.0.5**
- fixed an issue with the `RUN_AT_TIME` parameter set, which could lead to running too early.
- fixed more issues with file ingestion which could lead to not creating backup files.

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
