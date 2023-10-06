FROM ubuntu:jammy

RUN apt-get update && \
    apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg-agent

RUN curl -sSL https://packagecloud.io/golang-migrate/migrate/gpgkey | apt-key add -
RUN echo "deb https://packagecloud.io/golang-migrate/migrate/ubuntu/ jammy main" > /etc/apt/sources.list.d/migrate.list
RUN apt-get update && \
    apt-get install -y migrate

COPY . .
#make sure init.sh has correct line endings
RUN apt-get update && apt-get install -y dos2unix
RUN dos2unix ./init.sh && apt-get --purge remove -y dos2unix && rm -rf /var/lib/apt/lists/*
RUN chmod +x init.sh

RUN migrate -version
ENTRYPOINT ["./init.sh"]