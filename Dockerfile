# syntax = docker/dockerfile:experimental
FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build
RUN --mount=type=cache,target=/root/.nuget --mount=type=cache,target=/root/.local/share/NuGet --mount=type=cache,target=/root/.npm/ --mount=type=cache,target=./DotnetPlayground.Web/node_modules
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash - && apt-get install -y nodejs
WORKDIR /build

ENV DBKind="sqlite" ConnectionStrings__Sqlite="Filename=./bin/Debug/net7.0/Blogging.db"
ARG SOURCE_COMMIT
ARG SOURCE_BRANCH
ARG BUILD_CONFIG=${BUILD_CONFIG:-Release}

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
RUN dotnet restore -r linux-x64

COPY . .
RUN sed -i -e "s/GIT_HASH/$SOURCE_COMMIT/g" -e "s/GIT_BRANCH/$SOURCE_BRANCH/g" DotnetPlayground.Web/wwwroot/js/site.js
RUN dotnet test --no-restore -v m
RUN dotnet publish --no-restore -c $BUILD_CONFIG --self-contained -r linux-x64 \
    -p:PublishTrimmed=true \
    DotnetPlayground.Web





FROM mcr.microsoft.com/dotnet/runtime-deps:7.0-bullseye-slim
WORKDIR /app
ENV USER=nobody TZ=Europe/Warsaw ASPNETCORE_URLS=http://+:5000
ARG BUILD_CONFIG=${BUILD_CONFIG:-Release}
COPY --from=build --chown="$USER":"$USER" /build/DotnetPlayground.Web/bin/$BUILD_CONFIG/net7.0/linux-x64/publish/ /build/startApp.sh ./

RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

USER "$USER"

VOLUME /shared
EXPOSE 5000

ENTRYPOINT ["./DotnetPlayground.Web"]
