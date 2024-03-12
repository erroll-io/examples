#!/bin/bash

distributionDomainName="demo.erroll.io"

npm run build

aws s3 sync ./dist/ s3://demo.erroll.io/ --exclude '.*' --delete

distributionId=$(aws cloudfront list-distributions --query 'DistributionList.Items[*].[Aliases.Items[0], Id]' --output text | grep ".*${distributionDomainName}.*" | cut -f2)

aws cloudfront create-invalidation --distribution-id ${distributionId} --paths '/*'

