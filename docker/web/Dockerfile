ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0-alpine3.23
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0-alpine3.23
ARG NODE_IMAGE=node:24-alpine3.23

FROM ${NODE_IMAGE} AS node-build
WORKDIR /src
COPY Tickflo.Web/package.json ./
COPY Tickflo.Web/pnpm-lock.yaml ./
COPY Tickflo.Web/pnpm-workspace.yaml ./

RUN corepack enable \
    && pnpm config set store-dir /root/.pnpm-store

RUN --mount=type=cache,target=/root/.pnpm-store \
    pnpm install --frozen-lockfile

COPY Tickflo.Web ./

RUN pnpm run build

FROM ${SDK_IMAGE} AS build
WORKDIR /src

COPY Tickflo.sln ./
COPY Tickflo.Core/Tickflo.Core.csproj Tickflo.Core/
COPY Tickflo.Web/Tickflo.Web.csproj Tickflo.Web/

RUN dotnet restore Tickflo.Web/Tickflo.Web.csproj

COPY . .

RUN dotnet publish Tickflo.Web/Tickflo.Web.csproj -c Release -o /app/publish

FROM ${RUNTIME_IMAGE} AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
COPY --from=node-build /src/wwwroot/css/dist.css ./wwwroot/css/dist.css

ENTRYPOINT ["dotnet", "Tickflo.Web.dll"]
