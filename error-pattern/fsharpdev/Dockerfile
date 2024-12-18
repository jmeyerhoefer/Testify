FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
 && apt-get -y install ca-certificates dotnet-sdk-6.0 \
 && apt-get -y install ca-certificates dotnet-sdk-7.0 \
 && apt-get -y install ca-certificates dotnet-sdk-8.0 \
 && rm -rf /var/lib/apt/lists/* \

# Create a warmup project to populate package cache
RUN mkdir warmup \
 && cd warmup \
 && dotnet new xunit -lang "F#" \
 # Add required packaged here that should end up in the cache: \
 && dotnet add package XunitXml.TestLogger --version 4.1.0 \
 && dotnet add package Microsoft.NET.Test.Sdk --version 17.11.1 \
 && dotnet add package MSTest.TestAdapter --version 3.6.1 \
 && dotnet add package MSTest.TestFramework --version 3.6.1 \
 && dotnet add package FsCheck --version 2.16.6 \
 && dotnet restore \
 && cd - \
 && rm -rf warmup /tmp/NuGetScratch
