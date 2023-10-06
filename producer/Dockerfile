FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
COPY ./producer.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o out

#build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

# set time zone
RUN apk add --no-cache tzdata
ENV TZ=Europe/Stockholm

# create and use non-root user.. 
RUN adduser \
  --disabled-password \
  --home /producer \
  --gecos '' producer \
  && chown -R producer /producer
USER producer

WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "./producer.dll"]