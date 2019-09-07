FROM alpine:latest as build

RUN apk add -U wget gnupg && \
    wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /tmp/microsoft.asc.gpg

FROM mono:6.0

ARG BUILD_DATE
ARG VCS_REF

LABEL org.label-schema.build-date=$BUILD_DATE \
      org.label-schema.name="DocFX" \
      org.label-schema.description="DocFX CLI Docker replacement" \
      org.label-schema.url="https://github.com/dotnet/docfx" \
      org.label-schema.vcs-ref=$VCS_REF \
      org.label-schema.vcs-url="https://github.com/dotnet/docfx" \
      org.label-schema.vendor="Microsoft" \
      org.label-schema.schema-version="1.0"


RUN apt-get update && \
    apt-get install -y \
                    --no-install-recommends \
                    --no-install-suggests \
		    git apt-transport-https && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    adduser \
        --home /nonexistent \
        --shell /bin/false \
        --no-create-home \
        --gecos "" \
        --disabled-password \
        --disabled-login \
        docfx

# Copy downloaded and extracted DocFX sources to runtime container
COPY --chown=docfx:docfx ./target/Release/docfx /opt/docfx

# Copy launcher script to mimic CLI behavior
COPY ./tools/Deployment/docfx /usr/bin/docfx

COPY --from=build /tmp/microsoft.asc.gpg /etc/apt/trusted.gpg.d/microsoft.asc.gpg
ADD https://packages.microsoft.com/config/debian/9/prod.list /etc/apt/sources.list.d/microsoft-prod.list

# Install .NET Core SDK
RUN rm -f /etc/apt/sources.list.d/mono-official-stable.list && \
    apt-get update && \
    apt-get install -y \
                    --no-install-recommends \
                    --no-install-suggests \
		    dotnet-sdk-2.2 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# set user-context to unpriviledged user
USER docfx

# Default port for docfx serve
EXPOSE 8080

ENTRYPOINT ["/usr/bin/docfx"]
