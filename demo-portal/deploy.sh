#!/bin/bash

distributionDomainName="demo.erroll.io"
distributionBucketName="demo.erroll.io"

npm run build

aws s3 sync ./dist/ s3://${distributionBucketName}/ --exclude '.*' --delete

distributionId=$(aws cloudfront list-distributions --query 'DistributionList.Items[*].[Aliases.Items[0], Id]' --output text | grep ".*${distributionDomainName}.*" | cut -f2)

aws cloudfront create-invalidation --distribution-id ${distributionId} --paths '/*'
