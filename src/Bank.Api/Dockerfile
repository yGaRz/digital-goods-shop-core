FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY DigitalGoodsShop.sln ./
COPY src/Bank.Api/Bank.Api.csproj src/Bank.Api/
RUN dotnet restore src/Bank.Api/Bank.Api.csproj
COPY src/Bank.Api/ src/Bank.Api/
RUN dotnet publish src/Bank.Api/Bank.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8082
EXPOSE 8082
ENTRYPOINT ["dotnet", "Bank.Api.dll"]
