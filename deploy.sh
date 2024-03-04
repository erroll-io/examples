#!/bin/bash

set -e

functionName="minimal-api"
bucket="example-lambdas.erroll.io"
key="${functionName}.zip"

docker build --platform linux/arm64 --progress=plain -t minimal-api .

containerId=`docker run -t -d --rm minimal-api`
docker container cp ${containerId}:/source/publish/MinimalApi .
docker stop ${containerId}

zip -r ${key} -j MinimalApi src/MinimalApi/appsettings.json

rm -rf MinimalApi

aws s3 cp ${key} s3://${bucket}/${key}

aws lambda update-function-code --region us-west-2 --s3-bucket ${bucket} --s3-key ${key} --function-name ${functionName} --publish
