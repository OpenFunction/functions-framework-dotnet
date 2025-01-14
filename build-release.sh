#!/bin/bash

if [[ -z "$1" ]]
then
  echo "Please specify the release tag"
  exit 1
fi

set -e

rm -rf tmp
mkdir tmp

git clone https://github.com/OpenFunction/functions-framework-dotnet.git \
  --depth 1 -b $1 --recursive tmp/release
  
cd tmp/release
./build.sh

