FROM mcr.microsoft.com/dotnet/sdk:8.0-noble

# Add dotnet tools to path.
ENV PATH="${PATH}:/root/.dotnet/tools"

# Set Node.js path
ENV PLAYWRIGHT_NODEJS_PATH="/usr/bin/node"

# Set target docfx version.
ARG DOCFX_VERSION=2.78.2

# Install DocFX as a dotnet tool.
RUN dotnet tool install docfx -g --version ${DOCFX_VERSION} && \
    docfx --version && \
    rm  -f /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/docfx.nupkg                         && \
    rm  -f /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/docfx.${DOCFX_VERSION}.nupkg        && \
    rm -rf /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net9.0

# Install Node.js and browser(chromium) with dependencies
RUN apt-get install -y -qq --update --no-install-recommends nodejs && \
    pwsh -File /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net8.0/any/playwright.ps1 install --with-deps chromium && \
    rm -rf /var/lib/apt/lists/* && \
    rm -rf /tmp/*

WORKDIR /opt/prj
VOLUME [ "/opt/prj" ]

ENTRYPOINT [ "docfx" ]
