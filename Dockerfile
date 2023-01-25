#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY FingridMon.Service/FingridMon.Service.csproj FingridMon.Service/
RUN dotnet restore "FingridMon.Service/FingridMon.Service.csproj"
COPY . .
WORKDIR "/src/FingridMon.Service"
RUN dotnet build "FingridMon.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FingridMon.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#Entry
ENTRYPOINT ["dotnet", "FingridMon.Service.dll"]