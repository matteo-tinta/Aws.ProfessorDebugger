#!/bin/bash
set -euo pipefail

# Configuration
REGION="us-east-1"
ENDPOINT="http://localhost:4566"
BUCKET_NAME="my-local-bucket"
TOPIC_NAME="my-sns-topic"
QUEUE_NAME="my-sqs-queue"

echo "Creating S3 bucket..."
aws --endpoint-url=$ENDPOINT s3api create-bucket \
    --bucket $BUCKET_NAME \
    --region $REGION

echo "Creating SNS topic..."
TOPIC_ARN=$(aws --endpoint-url=$ENDPOINT sns create-topic \
    --name $TOPIC_NAME \
    --region $REGION \
    --query 'TopicArn' \
    --output text)

echo "Creating SQS queue..."
QUEUE_URL=$(aws --endpoint-url=$ENDPOINT sqs create-queue \
    --queue-name $QUEUE_NAME \
    --region $REGION \
    --query 'QueueUrl' \
    --output text)

# Get the ARN of the SQS queue
QUEUE_ARN=$(aws --endpoint-url=$ENDPOINT sqs get-queue-attributes \
    --queue-url $QUEUE_URL \
    --attribute-names QueueArn \
    --region $REGION \
    --query 'Attributes.QueueArn' \
    --output text)

echo "Subscribing SQS to SNS..."
aws --endpoint-url=$ENDPOINT sns subscribe \
    --topic-arn $TOPIC_ARN \
    --protocol sqs \
    --notification-endpoint $QUEUE_ARN \
    --region $REGION

echo "Setting up SQS policy to allow SNS to send messages..."
POLICY=$(cat <<EOF
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": "*",
    "Action": "sqs:SendMessage",
    "Resource": "$QUEUE_ARN",
    "Condition": {
      "ArnEquals": {
        "aws:SourceArn": "$TOPIC_ARN"
      }
    }
  }]
}
EOF
)

aws --endpoint-url=$ENDPOINT sqs set-queue-attributes \
    --queue-url $QUEUE_URL \
    --attributes Policy="$(echo $POLICY)"

echo "Setting S3 event notification to publish to SNS on object creation..."
NOTIFICATION_CONFIG=$(cat <<EOF
{
  "TopicConfigurations": [
    {
      "TopicArn": "$TOPIC_ARN",
      "Events": ["s3:ObjectCreated:*"]
    }
  ]
}
EOF
)

aws --endpoint-url=http://localhost:4566 s3api put-bucket-notification-configuration \
  --bucket my-local-bucket \
  --notification-configuration '{
    "TopicConfigurations": [
      {
        "TopicArn": "arn:aws:sns:us-east-1:000000000000:my-sns-topic",
        "Events": ["s3:ObjectCreated:*"]
      }
    ]
  }'

echo "✅ All resources created and wired up in LocalStack!"
