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

**⚠️ Important: Delete the cache if you switch environment/profile. There is any mechanism (yet) to prevent this.**

AWS has quotas and we’re not about wasting money — so this tool keeps a sneaky little cache in a JSON file right in your working directory (for now).

Pro tip: every now and then, manually clear out the cache to avoid the tool getting confused and showing you yesterday’s news about your infrastructure.

In future release a redis connection can be used to cache the whole json file. Or - maybe - a bucket S3 :)

**⚠️ Important: Do not repeatedly delete the cache.**

**The caching system is designed to avoid redundant work and prevent unnecessary AWS API calls — which can cost both time and money.
If you keep clearing the cache, you're defeating the entire purpose of this tool. Use it responsibly.**

# Momo Tool (Message Observer & Matching Operator)
**THIS TOOL IS IN PREVIEW, USE AT YOUR OWN RISK**

Momo is a powerful command-line interface (CLI) tool designed to help developers and infrastructure engineers trace, 
observe, and validate message flows within distributed systems, particularly those leveraging AWS services such as SNS, SQS, S3, and MongoDB. 

Its core purpose is to verify that expected messages and events occur within a specified timeframe, 
while being designed to operate ephemerally and transparently—leaving no lasting footprint in your infrastructure—making 
it especially useful for integration testing, debugging, and validating event-driven architectures.

## Key Features and Capabilities

### 1. Message Tracing and Validation
- Monitors asynchronous messages traveling through AWS services or other infrastructure components.
- Users define **expectations** for messages or files in a JSON file.
- Matches expectations against actual events within a configurable timeout.
- Raises an exception if expectations are not met in time.

### 2. Timeout as a "Wait Until" Mechanism
- Continuously checks for expected conditions until they are met or the timeout expires.
- Provides flexible waiting without unnecessary delays.

### 3. Extensibility via DLL Integration
- Core logic available as a DLL for C# projects for **full extensibility**.
- Allows creation of **custom steps and validations** tailored to unique architectures.
- Easily integrates with testing frameworks like NUnit, XUnit, TUnit.

### 4. JSON-Based Configuration
- Defines expectations with:
    - Timeout for matching operation.
    - List of expectations including service ARNs, filters, and match rules.
- Supports nested parallel expectations for complex validation scenarios.

## Limitations

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
| s3 (Momo.Expectations.S3)             | `filename` **is mandatory** and expects an S3 file key to be found in the given bucket, `content` expect a JsonPath to be found with the given value                                                                                                  |
| lambda                                | (Will be available in future releases)                                                                                                                                                                                                                |
| mongo (Momo.Expectations.Mongo)       | Connect to a mongo database (database name must be included in query string) and assert a query result. `documents` is a typed key and it must be used to access fetched documents (which is always an array).                                                                                                                                               
| parallel (Momo.Expectations.Parallel) | allow previous steps to run in parallel mode. If a step fails, the whole parallel stack will throw an exception

## Value matching
At the moment the value matching operator is valid only for SNS service. It is planned to be released in the future versions for all plugins.

If you want to include this matching values, you can use `Value` record object

| Operator | Matching type                                                         |
|----------|-----------------------------------------------------------------------|
| equals   | Try to match primitive values (for strings is case insensitive)       |
| contains | Try to search for strings inside the value (case insensitive)         |
| match    | Try to match the given pattern against the value (case insensitive)   |
| lt       | (future releases) Less than                                           |
| gt       | (future releases) Greater than                                        |
| lte      | (future releases) Less Than Equal                                     |
| gte      | (future releases) Greater then equal                                  |

## Json File Validation Example

```json
{
  "traceId": "abc123",
  "timeout": 30,
  "expectations": [
    {
      "arn": "arn:aws:sns:us-east-1:000000000000:my-test-topic",
      "match": {
        "Message.event": {
          "contains": "user signup"
        }
      }
    },
    {
      "arn": "arn:aws:s3:::ingestion-bucket",
      "file": {
        "prefix": "worklist-ready-to-be-worked/variant-move-between-worklists",
        "key": "event-[0-9].json"
      },
      "match": {
        "content.location": "DC4"
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
            "content.location": "DC4"
          }
        },
        {
          "arn": "arn:aws:sns:us-east-1:000000000000:my-second-test-topic",
          "match": {
            "Message.event": {
              "equals": "user login"
            },
            "Message.status": {
              "equals": "200"
            }
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
---
- ?: I want to use the cli, but with another profile (eg. Localstack)
- !: Export an env variable called AWS_PROFILE with your configured aws profile, and run the cli
---
- ?: I want to use the cli, but with an sso profile (eg. mastermind-dev)
- !: login through your terminal first (`aws sso login --profile mastermind-dev`) and then run the cli
---
- ?: I want to use the cli, but with custom tokens
- !: Export the AWS env variables given by aws login and run the cli