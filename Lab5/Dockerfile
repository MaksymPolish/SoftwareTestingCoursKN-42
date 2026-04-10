FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Lab5.Data/Lab5.Data.csproj", "Lab5.Data/"]
RUN dotnet restore "Lab5.Data/Lab5.Data.csproj"
COPY . .
WORKDIR "/src/Lab5.Data"
ARG CACHEBUST=1
RUN dotnet publish "Lab5.Data.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Lab5.Data.dll"]
