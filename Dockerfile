FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim

# Add dotnet tools to path.
ENV PATH="${PATH}:/root/.dotnet/tools"

# Install DocFX as a dotnet tool.
RUN dotnet tool update -g docfx && \
    docfx --version

# Install dependences for chromium PDF.
RUN apt-get update -qq && apt-get install -y libglib2.0-0 libnss3 libnspr4 \
    libatk1.0-0 libatk-bridge2.0-0 libcups2 libdrm2 libdbus-1-3 libxcb1 \
    libxkbcommon0 libatspi2.0-0 libx11-6 libxcomposite1 \ 
    libxdamage1 libxext6 libxfixes3 libxrandr2 libgbm1 \
    libpango-1.0-0 libcairo2 libasound2

WORKDIR /opt/prj
VOLUME [ "/opt/prj" ]

ENTRYPOINT [ "docfx" ]