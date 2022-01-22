FROM node:current-alpine as react-builder
COPY ./ui /ui
WORKDIR /ui
RUN yarn && \
    yarn build

FROM mcr.microsoft.com/dotnet/sdk:6.0 as aspnet-builder
COPY ./backend /backend
WORKDIR /backend
RUN dotnet restore && \
    dotnet publish PlexSSO.sln -c Release -o build /p:CopyOutputSymbolsToPublishDirectory=false && \
    rm build/ui/index.html
COPY --from=react-builder /ui/build /backend/build/ui

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=aspnet-builder /backend/build /app
RUN mkdir -p /config && \
    chmod 777 /config
ENTRYPOINT ["dotnet", "PlexSSO.dll", "--config", "/config/"]
EXPOSE 4200
VOLUME [ "/config" ]
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 CMD [ "dotnet", "PlexSSO.dll", "--healthcheck" ]