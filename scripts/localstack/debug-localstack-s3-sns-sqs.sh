#!/bin/bash
set -euo pipefail

# Config
REGION="us-east-1"
ENDPOINT="http://localhost:4566"
BUCKET_NAME="my-local-bucket"
TOPIC_NAME="my-sns-topic"
QUEUE_NAME="my-sqs-queue"
TEST_FILE="test-file.txt"

# Helper
log() {
  echo -e "\n🔎 $1"
}

log "Getting Topic ARN..."
TOPIC_ARN=$(aws --endpoint-url=$ENDPOINT sns list-topics \
  --region $REGION \
  --query "Topics[?contains(TopicArn, '$TOPIC_NAME')].TopicArn" \
  --output text)

echo "TOPIC_ARN: $TOPIC_ARN"

log "Getting SQS Queue URL and ARN..."
QUEUE_URL=$(aws --endpoint-url=$ENDPOINT sqs get-queue-url \
  --queue-name $QUEUE_NAME \
  --region $REGION \
  --query 'QueueUrl' \
  --output text)

QUEUE_ARN=$(aws --endpoint-url=$ENDPOINT sqs get-queue-attributes \
  --queue-url $QUEUE_URL \
  --region $REGION \
  --attribute-names QueueArn \
  --query 'Attributes.QueueArn' \
  --output text)

echo "QUEUE_URL: $QUEUE_URL"
echo "QUEUE_ARN: $QUEUE_ARN"

log "Listing SNS Subscriptions..."
aws --endpoint-url=$ENDPOINT sns list-subscriptions \
  --region $REGION

log "Checking if the SQS subscription exists for SNS topic..."
aws --endpoint-url=$ENDPOINT sns list-subscriptions-by-topic \
  --topic-arn "$TOPIC_ARN" \
  --region $REGION

log "Verifying SQS queue policy allows SNS to send messages..."
aws --endpoint-url=$ENDPOINT sqs get-queue-attributes \
  --queue-url $QUEUE_URL \
  --attribute-names Policy \
  --region $REGION

log "Getting S3 bucket notification configuration..."
aws --endpoint-url=$ENDPOINT s3api get-bucket-notification-configuration \
  --bucket $BUCKET_NAME \
  --region $REGION

log "Creating a test file and uploading to S3 to trigger event..."
echo "Debug file: $(date)" > "$TEST_FILE"
aws --endpoint-url=$ENDPOINT s3 cp "$TEST_FILE" "s3://$BUCKET_NAME/$TEST_FILE"

log "Waiting 3 seconds for event propagation..."
sleep 3

log "Checking for message in SQS..."
aws --endpoint-url=$ENDPOINT sqs receive-message \
  --queue-url "$QUEUE_URL" \
  --max-number-of-messages 1 \
  --wait-time-seconds 1 \
  --region $REGION
