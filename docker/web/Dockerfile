ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0-alpine3.23
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.23
ARG NODE_IMAGE=node:24-alpine3.23

# Build the frontend assets

FROM ${NODE_IMAGE} AS node-build
WORKDIR /src
COPY Tickflo.Web/package.json Tickflo.Web/pnpm-lock.yaml Tickflo.Web/pnpm-workspace.yaml ./

RUN corepack enable \
    && pnpm config set store-dir /root/.pnpm-store

RUN --mount=type=cache,target=/root/.pnpm-store,sharing=locked \
    pnpm install --frozen-lockfile

COPY Tickflo.Web/wwwroot/css ./wwwroot/css
COPY Tickflo.Web/Pages ./

RUN pnpm run build

# Build the .NET application

FROM ${SDK_IMAGE} AS build
WORKDIR /src

COPY Tickflo.sln ./
COPY Tickflo.Core/Tickflo.Core.csproj ./Tickflo.Core/
COPY Tickflo.Web/Tickflo.Web.csproj ./Tickflo.Web/

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore --locked-mode Tickflo.Web/Tickflo.Web.csproj

COPY Tickflo.Core/. ./Tickflo.Core/
COPY Tickflo.Web/. ./Tickflo.Web/

WORKDIR /src/Tickflo.Web

RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish -c Release --no-restore -o /app/publish

# Create the runtime image

FROM ${RUNTIME_IMAGE} AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
COPY --from=node-build /src/wwwroot/css/dist.css ./wwwroot/css/dist.css

# Remove the original site.css if it exists
RUN rm ./wwwroot/css/site.css

ENTRYPOINT ["dotnet", "Tickflo.Web.dll"]
