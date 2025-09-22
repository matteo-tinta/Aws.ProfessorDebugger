# Mastermind.ProfessorDebugger

This repository includes a set of tools designed to simplify working with AWS infrastructure

# How to use the CLI
1. Configure your ~/.aws/credentials with the api key provided by AWS Credentials login page
2. Download, build this repository (CLI Project)
3. **Optional**: copy the output bin directory somewhere easily accessible to you
4. Invoke the CLI as mentioned above

## Graph
This project includes a set of tools that generates a dependency graph of resources 
starting from a single AWS ARN.

Currently, the tool focuses on the following AWS services:
- Lambda
- SQS
- SNS
- S3

The project is composed of two main libraries:

- Core / Momo:
A reusable .NET 8.0 library that can be integrated into any C# project. It provides all the logic executed by the CLI.

- CLI:
A command-line interface built on top of the Core library. This is ideal for interactive exploration or scripting purposes.

**⚠️ Note 1: The graph generation process can be slow the first time, especially when the cache is empty. 
Please be patient — subsequent runs will be much faster thanks to local caching.**

**⚠️ Note 2: Resource resolution is not guaranteed to be 100% accurate. AWS resources can be complex and loosely 
coupled, and some relationships might be missed or inferred incorrectly. Always double-check results manually before 
relying on them for critical tasks. If you encounter an incorrect dependency resolution please report it asap**

## CLI

```bash
F31 Mastermind AWS Professor Debugger 1.0.0+dad39f1a5f55149981c490fc5513f64c73c179ee
Copyright (c) Matteo Tinta (F31)

cli graph [aws_arn] [args]

  --ignore-cache    (Default: false) Ignore stored cache. Actual present cache file will be overidden.
  --output-as       (Default: Cli) Cli, Json or Graph (case sensitive)
  --cache-type      (Default: JsonFile) Only JsonFile is available for now (case sensitive)
  --max-level       Set the max level to stop
  --help            Display this help screen.
  --version         Display version information.
```

## Caching

AWS has quotas and we’re not about wasting money — so this tool keeps a sneaky little cache in a JSON file right in your working directory (for now).

Pro tip: every now and then, manually clear out the cache to avoid the tool getting confused and showing you yesterday’s news about your infrastructure.

In future release a redis connection can be used to cache the whole json file. Or - maybe - a bucket S3 :)

**⚠️ Important: Do not repeatedly delete the cache.**

**The caching system is designed to avoid redundant work and prevent unnecessary AWS API calls — which can cost both time and money.
If you keep clearing the cache, you're defeating the entire purpose of this tool. Use it responsibly.**

# Momo Tool (Message Observer & Matching Operator)
**THIS TOOL IS IN PREVIEW, USE AT YOUR OWN RISK**

This fantastic tool comes in handy when you need to trace a series of messages inside your infrastructure. 

Create a json somewhere and feed it to momo. It will try to match all your expectations or return an exception if some are not respected in a given timeout

## Cli
```bash
F31 Mastermind AWS Professor Debugger 1.0.0+dad39f1a5f55149981c490fc5513f64c73c179ee
Copyright (c) Matteo Tinta (F31)

cli momo [json_validation_file]

  --help            Display this help screen.
  --version         Display version information.
```

## Allowed services

| Type    | Notes                                                                                                                                                                                    |
|---------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| sns     | It will create a temporary SQS queue and attach it to the specified SNS                                                                                                                  |
| sqs     | Direct SQS inspection isn't allowed, provide it's connected SNS topic as above. If there's no SNS, avoid checking the queue and look at the resources it triggers (like Lambda functions). |
| s3      | only "filename" expectation accepted for now (content expectations will be available in future releases)                                                                                 |
| lambda  | (Will be available in future releases)                                                                                                                                                   |
| mongo   | Connect to a mongo database (database name must be included in query string) and assert a query result 

## Json File Validation Example

```json
{
  "traceId": "abc123",
  "timeout": 30,
  "expectations": [
    {
      "arn": "arn:aws:sns:us-east-1:000000000000:my-topic",
      "match": {
        "Message.users[0].Name": "This is a name"
      }
    },
    {
      "arn": "arn:aws:s3:::ingestion-bucket",
      "match": {
        "filename": "file.txt"
      }
    },
    {
      "connectionString": "mongodb://mongo:mongo@localhost:27017/NAP_Mastermind_lcl?directConnection=true&authSource=admin&retryWrites=true&w=majority",
      "match": [
        {
          "query": {
            "find": "uploadList",
            "filter": { "name": "123123" }
          },
          "match": {
            "documents[0].name": "123123",
            "documents[1].name": "123123"
          }
        }
      ]
    }
  ]
}
```

# How to develop this tool
1. You do need to set your SSO AWS Profile called mastermind-dev (use AWS Explorer vs extension)
2. Start the Cli project within visual studio
3. Open a new branch from master
4. Submit a PR with your changes

<b>
💡 Note: If you encounter errors such as 
"AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY/AWS_SESSION_TOKEN were not set with AWS credentials",
you likely need to either:
- Restart Visual Studio, or
- Renew your SSO login using your usual workflow.
</b>

# FAQ
- ?: I cannot use the CLI because the token is expired
- !: Save your tokens again (read the CLI HELP)