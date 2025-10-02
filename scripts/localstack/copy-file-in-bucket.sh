#/bin/bash

aws --endpoint-url=http://localhost:4566 s3 cp test-file.txt s3://my-local-bucket/test-file.txt
aws --endpoint-url=http://localhost:4566 s3 cp test-file.json s3://my-local-bucket/test-file.json
aws --endpoint-url=http://localhost:4566 s3 cp test-file.json s3://my-local-bucket/nested/path/test-file.json
