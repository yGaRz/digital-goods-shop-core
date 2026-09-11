FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY DigitalGoodsShop.sln ./
COPY src/Buyer.Api/Buyer.Api.csproj src/Buyer.Api/
RUN dotnet restore src/Buyer.Api/Buyer.Api.csproj
COPY src/Buyer.Api/ src/Buyer.Api/
RUN dotnet publish src/Buyer.Api/Buyer.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
ENTRYPOINT ["dotnet", "Buyer.Api.dll"]
