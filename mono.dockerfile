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
		    git && \
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

# set user-context to unpriviledged user
USER docfx

# Default port for docfx serve
EXPOSE 8080

ENTRYPOINT ["/usr/bin/docfx"]
