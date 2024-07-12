FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim

# Add dotnet tools to path.
ENV PATH="${PATH}:/root/.dotnet/tools"

# Set target docfx version.
ARG DOCFX_VERSION=2.77.0

# Install DocFX as a dotnet tool.
RUN dotnet tool install docfx -g --version ${DOCFX_VERSION} && \
    docfx --version && \
    rm  -f /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/docfx.nupkg                         && \
    rm  -f /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/docfx.${DOCFX_VERSION}.nupkg        && \
    rm -rf /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net6.0

# Install Node.js and dependences for chromium PDF.
RUN apt-get update -qq && \
    apt-get install -y -qq --no-install-recommends \
    nodejs \
    libglib2.0-0 libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 \
    libdbus-1-3 libxcb1 libxkbcommon0 libatspi2.0-0 libx11-6 libxcomposite1 libxdamage1 \
    libxext6 libxfixes3 libxrandr2 libgbm1 libpango-1.0-0 libcairo2 libasound2 && \
    rm -rf /var/lib/apt/lists/* /tmp/*

# Install Chromium.
RUN PLAYWRIGHT_NODEJS_PATH="/usr/bin/node" && \
    ln -s /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/.playwright /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net8.0/any/.playwright && \
    pwsh -File /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net8.0/any/playwright.ps1 install chromium && \
    unlink /root/.dotnet/tools/.store/docfx/${DOCFX_VERSION}/docfx/${DOCFX_VERSION}/tools/net8.0/any/.playwright

WORKDIR /opt/prj
VOLUME [ "/opt/prj" ]

ENTRYPOINT [ "docfx" ]