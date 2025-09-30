# Mastermind.ProfessorDebugger

This repository includes a set of tools designed to simplify working with AWS infrastructure

---

# Quickstart

Follow these steps to get started quickly:

## 0. Login to AWS or setup your aws credentials (if needed)

- If you are using an SSO login run: `aws sso login --profile [profile-name]`
- If you are using credentials file, create and store them in: `~/.aws/credentials` as described in AWS login

## 1. Download and build
Download the repo, restore packages and build locally 

Optionally, if you're using Bash, you can set an alias for convenience:
```bash
alias mpd='/path/to/your/repo/core/bin/Debug/net8.0/Core.exe'
```

## 2. Generate Momo File with Graph
The Graph project will generate a CLI to traverse your AWS infrastructure given an AWS ARN.

```bash
mpd graph [resource_arn] --output-as Momo --output-at ./test.json 
```
> This produces a .json file representing your infrastructure.

## 3. Run Momo in autogeneration mode
This mode listens to your infrastructure and enriches your JSON with a basic schema.
```bash
mpd momo ./test.json -a
```
> Quick scaffolding only—refine the generated schemas before using in production-grade tests.

## 4. Run Momo
This mode will listen to your infrastructure and try to match your expectations:
```bash
mpd momo ./test.json
```

---

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
  --output-as       (Default: Cli) Cli, Json, Graph or Momo (case sensitive)
  --output-to       Path to save the output (required and used only if --output-as=Momo)
  --cache-type      (Default: JsonFile) Only JsonFile is available for now (case sensitive)
  --max-level       Set the max level to stop
  --help            Display this help screen.
  --version         Display version information.
```

## Caching

**⚠️ Important: Delete the cache if you switch environment/profile. There is any mechanism (yet) to prevent this.**

AWS has quotas and we’re not about wasting money — so this tool keeps a sneaky little cache in a JSON file right in your working directory (for now).

Pro tip: every now and then, manually clear out the cache to avoid the tool getting confused and showing you yesterday’s news about your infrastructure.

In future release a redis connection can be used to cache the whole json file. Or - maybe - a bucket S3 :)

**⚠️ Important: Do not repeatedly delete the cache.**

**The caching system is designed to avoid redundant work and prevent unnecessary AWS API calls — which can cost both time and money.
If you keep clearing the cache, you're defeating the entire purpose of this tool. Use it responsibly.**

# Momo Tool (Message Observer & Matching Operator)
**THIS TOOL IS IN PREVIEW, USE AT YOUR OWN RISK**

```text
       _                        
       \`*-.                    
        )  _`-.                 
       .  : `. .                
       : _   '  \               
       ; *` _.   `*-._          
       `-.-'          `-.       
         ;       `       `.     
         :.       .        \    
         . \  .   :   .-'   .   
         '  `+.;  ;  '      :   
         :  '  |    ;       ;-. 
         ; '   : :`-:     _.`* ;
[bug] .*' /  .*' ; .*`- +'  `*' 
      `*-*   `*-*  `*-*'
```
_Art by Blazej Kozlowski_

Momo is a powerful command-line interface (CLI) tool designed to help developers and infrastructure engineers trace, 
observe, and validate message flows within distributed systems, particularly those leveraging AWS services such as SNS, SQS, S3, and MongoDB. 

Its core purpose is to verify that expected messages and events occur within a specified timeframe, 
while being designed to operate ephemerally and transparently—leaving no lasting footprint in your infrastructure—making 
it especially useful for integration testing, debugging, and validating event-driven architectures.

## Limitations

- **_If you forcefully close it, it misses a way to catch the event and dispose your resources (for now)_** - _(be kind with momo)_.
- Due to the nature of our shared AWS infrastructure and the possibility of manual or external message publication to SNS topics, it is **not possible to guarantee strict correlation between test actions and observed messages**.
- This tool attempts to assert the presence of expected messages within a specified window, but **cannot guarantee** that matched messages were produced exclusively by the test under execution.
- All test resources are deleted after each cycle to minimize contamination (not data), but as trace IDs or unique correlation identifiers cannot be enforced, there is a potential for false positives.
- *This tool do not provide an automatic detection of created resources during the test phase*. So remember to delete them in the `DisposeAsync` method to avoid dangling resources in your infrastructure.
- For highest reliability, use this tool in isolated environments or when no manual/external messages are being published.

## Cli
```bash
F31 Mastermind AWS Professor Debugger 1.0.0+dad39f1a5f55149981c490fc5513f64c73c179ee
Copyright (c) Matteo Tinta (F31)

cli momo [json_validation_file]

  -a --auto         (Default: false) Auto generation mode, will overwrite your existing file with autogenerated standard metadata.
  --help            Display this help screen.
  --version         Display version information.
```

## DLL

The core functionality of this project is also available as an independent DLL that you can include in your C# projects.
Base functionalities must be installed as well (indipendent libraries, see table below)

Using the DLL allows you to extend the tool by writing your own custom steps and validations that 
are not yet provided in the base version used by the CLI. 

For example, you could implement custom integrations such as 
SQL Server connections or other specific checks tailored to your infrastructure.

This flexibility enables seamless integration of Momo and validation 
capabilities directly into your existing applications or testing frameworks.

Here is a brief example on how to integrate with a custom step:

```c#
public class CustomStepHandler(bool shouldPass) : IStepHandler 
{
    public async Task<IMomoExpectation> GenerateExpectationAsync(IMomoExpectation baseConfig, CancellationToken cancellationToken)
    {
        //Autogenerate your expectation here
        
        //this method is called by -a --auto method in order to autogenerate basic metadata exception
    }
    
    public ValueTask DisposeAsync()
    {
        //dispose your resources
        //will be called after CheckAsync or in case of any exception during the matching process
        return ValueTask.CompletedTask;
    }

    public Task PrepareAsync(IMomoExpectation config, CancellationToken cancellationToken)
    {
        //prepare your step
        return Task.CompletedTask;
    }

    public Task<bool> CheckAsync(IMomoExpectation config, int timeout, CancellationToken cancellationToken)
    {
        //This step will be called multiple times, do not initialize anything here!
        return Task.FromResult(shouldPass);
    }
}

public class CustomExpectation(bool shouldPass) : IMomoExpectation 
{
    public IStepHandler GetStepHandler(MomoClientFactoryOptions options)
    {
        return new CustomStepHandler(shouldPass);
    }
}


//NUnit test class
public class UnitTestProject 
{
    [Test]
    public async Task This_Is_Passing_Test() 
    {
        var momoExpectation = new CustomExpectation(shouldPass: true);
        
        var expectationFile = new MomoExpectationFile()
        {
            Expectations = [momoExpectation]
        };
        
        var client = MomoClientFactory.FeedMomo(new MomoClientFactoryOptions()
        {
            ExpectationFile = expectationFile
        });
    
        await client.MatchExpectations(CancellationToken.None);
        
        //if an assert exception is raised test will fail, a step cannot be verified in the given timeout
    }
    
    [Test]
    public async Task This_Is_An_Expected_Failed_Test() 
    {
        var momoExpectation = new CustomExpectation(shouldPass: false);
        
        var expectationFile = new MomoExpectationFile()
        {
            Expectations = [momoExpectation]
        };
        
        var client = MomoClientFactory.FeedMomo(new MomoClientFactoryOptions()
        {
            ExpectationFile = expectationFile
        });
    
        await Assert.ThrowsAsync<AssertException>(async () => await client.MatchExpectations(CancellationToken.None), "message")
    }
}
```

## Available services (in CLI)

| Type                                  | Notes                                                                                                                                                                                                                                                 |
|---------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| sns (Momo.Expectations.SNS)           | It will create a temporary SQS queue and attach it to the specified SNS                                                                                                                                                                               |
| sqs                                   | **In order to avoid race conditions in your environments** direct SQS inspection isn't allowed, provide it's connected SNS topic as above. If there's no SNS, avoid checking the queue and look at the resources it triggers (like Lambda functions). |
| s3 (Momo.Expectations.S3)             | `file` **is mandatory** and expects an S3 file key to be found in the given bucket, `content` expect a JsonPath to be found with the given value                                                                                                      |
| lambda                                | (Will be available in future releases)                                                                                                                                                                                                                |
| mongo (Momo.Expectations.Mongo)       | Connect to a mongo database (database name must be included in query string) and assert a query result. `documents` is a typed key and it must be used to access fetched documents (which is always an array).                                        
| parallel (Momo.Expectations.Parallel) | allow previous steps to run in parallel mode. If a step fails, the whole parallel stack will throw an exception                                                                                                                                       

## Value matching
Values are matched by using a json schema.

If an attempt of using a json schema for matching a file which is not an S3 file, it will raise an exception

## S3 File searching
To directly find a file, avoid specifying `prefix` and just type the full file key like so
```json
{
      "arn": "arn:aws:s3:::ingestion-bucket",
      "file": {
        "key": "worklist-ready-to-be-worked/variant-move-between-worklists/event-0.json"
      },
      "match": {}
    }
```

In order to find a file, which can be dynamically composed (such as dates in the name), you can use a regex like so:
```json
{
      "arn": "arn:aws:s3:::ingestion-bucket",
      "file": {
        "prefix": "worklist-ready-to-be-worked/variant-move-between-worklists",
        "key": "event-[0-9].json"
      },
    }
```

or at root
```json
{
      "arn": "arn:aws:s3:::ingestion-bucket",
      "file": {
        "prefix": "",
        "key": "event-[0-9].json"
      },
    }
```

If multiple files are found, an exception will be raised. 

In future release it's planned a better way to compose the complete file key

## Json File Validation Example

```json
{
  "traceId": "abc123",
  "timeout": 30,
  "expectations": [
    {
      "arn": "arn:aws:sns:us-east-1:000000000000:my-test-topic",
      "match": {
        ...json schema
      }
    },
    {
      "arn": "arn:aws:s3:::ingestion-bucket",
      "file": {
        "prefix": "worklist-ready-to-be-worked/variant-move-between-worklists",
        "key": "event-[0-9].json"
      },
      "match": {
        ...content json schema
      }
    },
    {
      "parallelExpectations": [
        {
          "arn": "arn:aws:s3:::ingestion-bucket",
          "file": {
            "prefix": "worklist-ready-to-be-worked/variant-move-between-worklists",
            "key": "event-[0-9].json"
          },
          "match": {
            ...content json schema 
          }
        },
        {
          "arn": "arn:aws:sns:us-east-1:000000000000:my-second-test-topic",
          "match": {
            ...content json schema
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
                ...query result schema
              }
            }
          ]
        }
      ]
    }
  ]
}
```
---
## Autogeneration Mode (`--auto` or `-a`)

When the `--auto` flag is passed to the CLI, Momo actively listens to your infrastructure and **auto-generates a base 
expectation file** by capturing live events and generating minimal JSON schemas.

This is ideal for **quick scaffolding**, but you'll likely want to refine the generated schemas for production-grade tests.

Start from here:
```json
{
  "traceId": "abc123",
  "timeout": 30,
  "expectations": [
    {
      "parallelExpectations": [
        {
          "arn": "arn:aws:s3:::ingestion-bucket",
          "file": {
            "prefix": "worklist-ready-to-be-worked/variant-move-between-worklists",
            "key": "event-[0-9].json"
          }
        },
        {
          "arn": "arn:aws:sns:us-east-1:000000000000:my-second-test-topic"
        },
        {
          "connectionString": "mongodb://mongo:mongo@localhost:27017/NAP_Mastermind_lcl?directConnection=true&authSource=admin&retryWrites=true&w=majority",
          "match": [
            {
              "query": {
                "find": "uploadList",
                "filter": { "name": "123123" }
              }
            }
          ]
        }
      ]
    }
  ]
}
```
---

### What It Does (Per Service)

| Type       | Behavior                                                                                                                  |
|------------|---------------------------------------------------------------------------------------------------------------------------|
| `sns`      | Subscribes a temporary SQS queue to the specified SNS topic. Waits for the first message and infers a JSON schema.        |
| `s3`       | Downloads the specified file. If valid JSON, generates a basic schema from its contents.                                  |
| `mongo`    | Executes the given query with `limit: 1`. If a document is returned, it generates a schema based on the result.           |
| `parallel` | Runs nested generations in parallel. No schema generation occurs here. Structure of this step is preserved for test flow. |

### Best practices
- Avoid (if not strictly required to your test) listening for an s3 file, listen for the push notification instead. It's safer
- If you need to test a lambda execution, you can check following resources (mongo or sns) and listen for the correct messages output

---

### Notes

- Generated schemas are **minimal** and **intended as a starting point**.
- You should **manually adjust and validate the schemas** to ensure accurate test coverage.
- Reference: [JSON Schema documentation](https://json-schema.org/understanding-json-schema/reference)

---

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

---

# FAQ
- ?: I cannot use the CLI because the token is expired
- !: Save your tokens again (read the CLI HELP)
---
- ?: I want to use the cli, but with another profile (eg. Localstack)
- !: Export an env variable called AWS_PROFILE with your configured aws profile, and run the cli
---
- ?: I want to use the cli, but with an sso profile (eg. mastermind-dev)
- !: login through your terminal first (`aws sso login --profile mastermind-dev`) and then run the cli
---
- ?: I want to use the cli, but with custom tokens
- !: Export the AWS env variables given by aws login and run the cli