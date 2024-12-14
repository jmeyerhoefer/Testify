FROM codercom/code-server:latest

# Install .NET SDK 6.0
USER root
RUN set -ex \
 ; cd /root \
 ; wget -nv -O packages-microsoft-prod.deb "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" \
 ; dpkg -i packages-microsoft-prod.deb \
 ; rm packages-microsoft-prod.deb \
 ; apt-get update -qq -y \
 ; apt-get install dotnet-sdk-6.0 -y \
 ; apt-get clean \
 ; rm -rf /var/lib/apt/lists/*

# Install .NET SDK 7.0
USER root
RUN set -ex \
 ; cd /root \
 ; wget -nv -O packages-microsoft-prod.deb "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" \
 ; dpkg -i packages-microsoft-prod.deb \
 ; rm packages-microsoft-prod.deb \
 ; apt-get update -qq -y \
 ; apt-get install dotnet-sdk-7.0 -y \
 ; apt-get clean \
 ; rm -rf /var/lib/apt/lists/*

# Install .NET SDK 8.0
USER root
RUN set -ex \
 ; cd /root \
 ; wget -nv -O packages-microsoft-prod.deb "https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb" \
 ; dpkg -i packages-microsoft-prod.deb \
 ; rm packages-microsoft-prod.deb \
 ; apt-get update -qq -y \
 ; apt-get install dotnet-sdk-8.0 -y \
 ; apt-get clean \
 ; rm -rf /var/lib/apt/lists/*

# Create a warmup project to populate package cache
RUN mkdir warmup \
 && cd warmup \
 && dotnet new xunit -lang "F#" \
 # Add required packaged here that should end up in the cache:
 && dotnet add package Microsoft.NET.Test.Sdk --version 17.10.0 \
 && dotnet add package MSTest.TestAdapter --version 3.4.3 \
 && dotnet add package MSTest.TestFramework --version 3.4.3 \
 && dotnet add package FsCheck --version 2.16.6 \
 && dotnet restore \
 && cd - \
 && rm -rf warmup /tmp/NuGetScratch

# Execute dotnet restore
COPY custom_entrypoint.sh /usr/bin/custom_entrypoint.sh
ENTRYPOINT ["/usr/bin/custom_entrypoint.sh", "--bind-addr", "0.0.0.0:8080", "."]

# Install Ionide-FSharp extension
USER coder
RUN code-server --install-extension ionide.ionide-fsharp