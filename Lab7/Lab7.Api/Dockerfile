FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Lab7.Api.csproj", "."]
RUN dotnet restore "Lab7.Api.csproj"

COPY . .
RUN dotnet build "Lab7.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab7.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "Lab7.Api.dll"]
