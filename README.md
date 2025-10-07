# Mastermind.ProfessorDebugger

This repository includes a set of tools designed to simplify working with AWS infrastructure

---

# Quickstart

Follow these steps to get started quickly:

## 0. Login to AWS or setup your aws credentials (if needed)

- If you are using an SSO login run: `aws sso login --profile [profile-name]`
- If you are using credentials file, create and store them in: `~/.aws/credentials` as described in AWS login
- export AWS_PROFILE in your console, for example in git bash is: `export AWS_PROFILE="mastermind-dev"`

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

## Print types (output-as)
### Cli
Default. Prints resources as a readable dependency tree, easy to scan in the terminal.
- `--output-at` is ignored

### Json
Outputs resources as JSON with `parents` and `children` properties, starting from the given ARN.
- `--output-at` is ignored

### Graph
Outputs the graph structure in JSON format, suitable for graph engines or further processing.
- `--output-at` is ignored

### Momo
Requires `--output-at`. Saves a Momo expectation file for scaffolding.
- `--max-level` is ignored
- No autogeneration is performed — only discovered resources (arns) are included.
- Traversal stops at the given ARN or at its nearest children, ensuring the file is ready for future checks.
- If a resource has multiple parents, they are output as a `ParallelExpectation` block. **Since the intended flow cannot be inferred, you’ll need to clean this up manually**.
- S3 nodes prefix and file key are left empty and **must be manually set**

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

- **Resource disposal:** Momo attempts to dispose of resources when a process finishes or fails (CTRL+C, Process Exit or Unhandled Exception), but forceful termination may leave lingering resources. Monitor and clean up manually if needed.
- **Shared infrastructure risk:** In environments with shared SNS topics, SQS queues, or databases, Momo cannot guarantee that observed messages or data changes originated from the current test. External activity may cause false positives.
- **No automatic resource detection:** Momo does not detect new resources created during tests. Always explicitly clean up resources in `DisposeAsync` to avoid dangling resources.
- **Message correlation limitations:** Without enforced trace IDs or unique identifiers, Momo cannot strictly correlate messages with test actions. Use isolated or controlled environments for high-confidence testing.
- **Partial AWS coverage:** Only SNS, SQS, S3, and MongoDB are currently supported; Lambda support is planned. Complex AWS resource relationships may be missed or inferred incorrectly.
- **Cache sensitivity:** Momo relies on a local JSON cache to prevent redundant API calls. Clearing the cache frequently defeats this purpose and may cause slower runs or inconsistent results.

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

## Declarative Flow (cli)

## Basic Example
This is a basic example of structure of a momo generated file
```json
{
  "traceId": "abc123",
  "timeout": 20,
  "do": [
    {
      "_type": "s3",
      "operation": "Publish",
      "bucketName": "my-local-bucket",
      "destinationCompleteFileKey": "test-file.json",
      "sourceFilePath": "./bucket-test-file.json"
    }
  ],
  "expectations": [
    {
      "parallelExpectations": [
        {
          "arn": "arn:aws:sns:us-east-1:000000000000:my-sns-topic",
          "match": null
        },
        {
          "arn": "arn:aws:s3:::my-local-bucket",
          "file": {
            "key": "test-file.json"
          },
          "match": null
        }
      ]
    },
    {
      "connectionString": "mongodb://mongo:mongo@localhost:27017/NAP_Mastermind_lcl?directConnection=true\u0026authSource=admin\u0026retryWrites=true\u0026w=majority",
      "match": [
        {
          "query": {
            "find": "uploadList",
            "filter": {
              "name": "123123"
            },
            "limit": 1
          },
          "match": null
        }
      ]
    }
  ]
}
```

### Available services

| Type                                  | Notes                                                                                                                                                                                                                                                 |
|---------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| sns (Momo.Expectations.SNS)           | It will create a temporary SQS queue and attach it to the specified SNS                                                                                                                                                                               |
| sqs                                   | **In order to avoid race conditions in your environments** direct SQS inspection isn't allowed, provide it's connected SNS topic as above. If there's no SNS, avoid checking the queue and look at the resources it triggers (like Lambda functions). |
| s3 (Momo.Expectations.S3)             | `file` **is mandatory**, `match` expect a JsonPath to be found with the given value                                                                                                                                                                   |
| lambda                                | (Will be available in future releases)                                                                                                                                                                                                                |
| mongo (Momo.Expectations.Mongo)       | Connect to a mongo database (database name must be included in query string) and assert a query result. `documents` is a typed key and it must be used to access fetched documents (which is always an array).                                        
| parallel (Momo.Expectations.Parallel) | allow previous steps to run in parallel mode. If a step fails, the whole parallel stack will throw an exception

## Commands
Momo declarative flow supports now commands!

Commands allow you to automate some actions in order to trigger your async flows. Commands are declared as follow in the `do` directive

```json
{
  "traceId": "abc123",
  "timeout": 30,
  "do": [
    {
      "_type": "expectation type",
      "other": "properties",
      "goes": "here",
      "_undo": {
        "other": "properties",
        "goes": "here",
      }
    }
  ],
  "expectations": [
    
  ]
}
```
> `_undo` is the same type of command, but is executed in case of test going bad or when the command fails. The contract is the same

> You can launch multiple commands at the start of the test. Commands are executed after the first step is prepared (example: if you are watching an SNS resource as first step, the commands will wait until the SQS is ready)
> In case of parallel steps, commands are executed after all parallel steps are prepared.

Available services are:

- ### S3
S3 command allows you to publish a file in a given bucket at a specified key. Here an example of S3 Publish operation:
```json
{
  "traceId": "abc123",
  "timeout": 30,
  "do": [
    {
      "_type": "s3",
      "operation": "Publish",
      "bucketName": "my-local-bucket",
      "destinationCompleteFileKey": "test-file.json",
      "sourceFilePath": "./bucket-test-file.json"
    }
  ],
  "expectations": [
    ...expectations
  ]
}
```
> More actions will be available in future such as `Delete`.
> You can use `Publish` to also trigger a reload of an already present file reloading the same file again

- ### Rest
Rest command allows you to make a REST call in whatever methods you need. Only JSON payload can be used
```json
{
  "traceId": "abc123",
  "timeout": 30,
  "do": [
    {
      "_type": "rest",
      "endpoint": "http://rest.endpoint",
      "method": "POST",
      "jsonBody": {
        "this object is your body": true,
        "as it is": "true",
        "also nested": {
          "is it true": true
        }
      }
    }
  ],
  "expectations": [
    ...expectations
  ]
}
```


## Value matching
Values are matched by using a json schema.

If an attempt of using a json schema for matching a file which is not an S3 file, it will raise an exception

## S3 File Searching

Momo can validate files in S3 buckets by matching on file keys. There are two main approaches:

### Exact key

If you know the full key, provide it directly:

```json
{
    "arn": "arn:aws:s3:::ingestion-bucket",
    "file": {
      "key": "worklist-ready-to-be-worked/variant-move-between-worklists/event-0.json"
    },
}
```
        
### Pattern-based key

If keys are dynamic (e.g. contain dates or IDs), use a prefix plus a regex key:
```json
{
  "arn": "arn:aws:s3:::ingestion-bucket",
  "file": {
    "prefix": "worklist-ready-to-be-worked/variant-move-between-worklists",
    "key": "event-[0-9]+\\.json"
  }
}
```
> The search is **not recursive**. Files are matched only inside the given `prefix` path, not in subfolders.
> To search at the bucket root, set `"prefix"`: `""`.

### Matching rules
1. File key must match the regex (case-insensitive).
2. File must have LastModified within 1 minute before the tool started. Older files are ignored.
3. A maximum of 50 results is returned
4. If multiple files match, Momo fails with an error.

When a match is found, Momo updates the expectation file with the resolved key so the same file won’t be re-matched in future runs.

## Json File Validation Example

```json
{
  "traceId": "abc123",
  "timeout": 30,
  "do": [],
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
  "do": [],
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
- If you really want to expect an S3 file to be uploaded togheter the SNS notification that will come out, use a `parallelExpectations` to avoid missing out messages
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