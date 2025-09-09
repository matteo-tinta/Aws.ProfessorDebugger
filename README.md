# Mastermind.ProfessorDebugger
This repository includes all the tool to simplify my life when talking about AWS

## Lambda to nearest resource list
1. Configure your ~/.aws/credentials with the api key provided by AWS Credentials login page
2. Start the tool
3. Insert a lambda ARN (very important!)
4. Tool is not super fast, SNS scans all the S3 to find where it is being registered (for now)

## Caching

AWS has quotas and we’re not about wasting money — so this tool keeps a sneaky little cache in a JSON file right in your working directory.

Pro tip: every now and then, manually clear out the cache to avoid the tool getting confused and showing you yesterday’s news about your infrastructure.
