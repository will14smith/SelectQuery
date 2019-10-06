#!/bin/bash

set -eux

cd "$(dirname "$0")"

cd SelectQuery.Lambda
dotnet lambda package -o ../artifacts/lambda.zip -f netcoreapp2.1
cd ..

sls deploy "$@"