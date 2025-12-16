# syntax = docker/dockerfile:experimental
ARG BASE_IMAGE_VARIANT=bookworm-slim
FROM mcr.microsoft.com/dotnet/sdk:9.0-${BASE_IMAGE_VARIANT} AS build
RUN --mount=type=cache,target=/root/.nuget --mount=type=cache,target=/root/.local/ --mount=type=cache,target=/root/.cache/ --mount=type=cache,target=./node_modules
RUN curl -SLO https://deb.nodesource.com/nsolid_setup_deb.sh && \
    chmod 500 nsolid_setup_deb.sh && \
    ./nsolid_setup_deb.sh 22 && \
    apt-get install -y nodejs && npm install -g bun
WORKDIR /build

ENV DBKind="sqlite" ConnectionStrings__Sqlite="Filename=./bin/Debug/net9.0/Blogging.db"
ARG SOURCE_COMMIT
ARG SOURCE_BRANCH
ARG BUILD_CONFIG=${BUILD_CONFIG:-Release}
ARG RUNTIME_ID=${RUNTIME_ID:-linux-x64}

COPY ./InkBall/src/InkBall.Module/InkBall.Module.csproj ./InkBall/src/InkBall.Module/InkBall.Module.csproj
COPY ./InkBall/test/InkBall.Tests/InkBall.Tests.csproj ./InkBall/test/InkBall.Tests/InkBall.Tests.csproj
COPY ./DotnetPlayground.Tests/DotnetPlayground.Tests.csproj ./DotnetPlayground.Tests/DotnetPlayground.Tests.csproj
COPY ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySqlConfig.Tools/Pomelo.Extensions.Caching.MySqlConfig.Tools.csproj ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySqlConfig.Tools/Pomelo.Extensions.Caching.MySqlConfig.Tools.csproj
COPY ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySql/Pomelo.Extensions.Caching.MySql.csproj ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySql/Pomelo.Extensions.Caching.MySql.csproj
COPY ./Caching-MySQL/test/Pomelo.Extensions.Caching.MySql.Tests/Pomelo.Extensions.Caching.MySql.Tests.csproj ./Caching-MySQL/test/Pomelo.Extensions.Caching.MySql.Tests/Pomelo.Extensions.Caching.MySql.Tests.csproj
COPY ./IdentityManager2/src/IdentityManager2/IdentityManager2.csproj ./IdentityManager2/src/IdentityManager2/IdentityManager2.csproj
COPY ./IdentityManager2/src/IdentityManager2/Assets/ ./IdentityManager2/src/IdentityManager2/Assets/
COPY ./DotnetPlayground.Web/DotnetPlayground.Web.csproj ./DotnetPlayground.Web/DotnetPlayground.Web.csproj
COPY ./*.sln ./NuGet.config ./
RUN dotnet restore -r $RUNTIME_ID /p:Configuration=Release

COPY . .
RUN sed -i -e "s/GIT_HASH/$SOURCE_COMMIT/g" -e "s/GIT_BRANCH/$SOURCE_BRANCH/g" DotnetPlayground.Web/wwwroot/js/site.js
RUN dotnet test -v m
RUN bun install --prod --unsafe-perm
RUN dotnet publish -c $BUILD_CONFIG --self-contained -r $RUNTIME_ID DotnetPlayground.Web





ARG BASE_IMAGE_VARIANT=bookworm-slim
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-${BASE_IMAGE_VARIANT}
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

WORKDIR /app
ENV USER=nobody
ARG BUILD_CONFIG=${BUILD_CONFIG:-Release}
ARG RUNTIME_ID=${RUNTIME_ID:-linux-x64}
COPY --from=build --chown="$USER":"$USER" /build/DotnetPlayground.Web/bin/$BUILD_CONFIG/net9.0/$RUNTIME_ID/publish/ /build/startApp.sh ./

USER "$USER"

VOLUME /shared
EXPOSE 8080

ENTRYPOINT ["./DotnetPlayground.Web"]
