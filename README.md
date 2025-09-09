# Mastermind.ProfessorDebugger
This repository includes all the tool to simplify my life when talking about AWS

Please be aware this tool is very slow when cache is not being saved yet. Give it some time

## Cli Help

```bash
Core 1.0.0+e42df8780a21e00a2af8dfb56457773dd3f77ce5
Copyright (C) 2025 Core

core [aws_arn] [args]

  --ignore-cache    (Default: false) Ignore stored cache. Actual present cache file will be overidden.
  --output-as       (Default: Cli) Cli or Json (case sensitive)
  --cache-type      (Default: JsonFile) Only JsonFile is available for now (case sensitive)
  --max-level       Set the max level to stop
  --help            Display this help screen.
  --version         Display version information.

```


## Lambda to nearest resource list
1. Configure your ~/.aws/credentials with the api key provided by AWS Credentials login page
2. Start the tool
3. Insert a lambda ARN (very important!)
4. Tool is not super fast, SNS scans all the S3 to find where it is being registered (for now)

## Caching

AWS has quotas and we’re not about wasting money — so this tool keeps a sneaky little cache in a JSON file right in your working directory (for now).

Pro tip: every now and then, manually clear out the cache to avoid the tool getting confused and showing you yesterday’s news about your infrastructure.

In future release a redis connection can be used to cache the whole json file. Or - maybe - a bucket S3 :) 