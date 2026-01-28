#!/bin/sh
if [ $# -eq 0 ]
then
  echo "use: ./publish.sh <runtime>"
  echo
  echo "example: ./publish.sh osx-x64"
  echo
  echo "possible runtimes: osx-arm64, osx-x64, win-x64, linux-x64, linux-arm64"
  echo "or use 'all' to build for all runtimes"
  echo
  exit 1
fi

if [ $1 = "all" ]
then
  runtimes=("osx-arm64" "osx-x64" "win-x64" "linux-x64" "linux-arm64")
else
  runtimes=($1)
fi

for runtime in "${runtimes[@]}"
do
  dotnet publish --self-contained -c release -r $runtime -p:PublishSingleFile=true -o dist/$runtime src

  sourceFilename="jwks"

  # Windows
  if [ -e dist/$runtime/$sourceFilename.exe ]
  then
      sourceFilename="jwks.exe"
  fi

  echo "Adding $sourceFilename to jwks-$runtime.zip..."
  zip -r -j dist/jwks-$runtime.zip dist/$runtime/$sourceFilename
done
