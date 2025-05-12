FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .

WORKDIR /app/App.Web
RUN dotnet publish App.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT [ "dotnet", "App.Web.dll" ]