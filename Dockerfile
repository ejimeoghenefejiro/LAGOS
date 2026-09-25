FROM node:22-bookworm-slim AS web
WORKDIR /web
COPY LGRRS-Web/package*.json ./
RUN npm ci
COPY LGRRS-Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS api
WORKDIR /src
COPY LGRRS-Api/ ./
RUN dotnet publish src/LGRRS.Api/LGRRS.Api.csproj -c Release -o /out
RUN rm -f /out/appsettings.Development.json

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=api /out ./
COPY --from=web /web/dist ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "LGRRS.Api.dll"]
