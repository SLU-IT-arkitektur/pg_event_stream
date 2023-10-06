FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY ./webconsumer.csproj .
RUN dotnet restore

# copy everything else and build
COPY . .
RUN dotnet publish -c Release -o out

#build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

# set time zone
RUN apk add --no-cache tzdata
ENV TZ=Europe/Stockholm

# create and use non-root user.. (requires us to use a port number higher than 1024, default 8080. override with Env variable PORT, e.g PORT=8181)
RUN adduser \
  --disabled-password \
  --home /webconsumer \
  --gecos '' webconsumer \
  && chown -R webconsumer /webconsumer
USER webconsumer



WORKDIR /app
COPY --from=build-env /app/out ./

ENTRYPOINT ["dotnet", "./webconsumer.dll"]
