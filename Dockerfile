# syntax = docker/dockerfile:experimental
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
RUN --mount=type=cache,target=/root/.nuget --mount=type=cache,target=/root/.local/share/NuGet --mount=type=cache,target=/root/.npm/ --mount=type=cache,target=./AspNetCore.ExistingDb/node_modules
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash - && apt-get install -y nodejs
WORKDIR /build

ENV DBKind="sqlite" ConnectionStrings__Sqlite="Filename=./bin/Debug/netcoreapp3.1/Blogging.db"
ARG SOURCE_COMMIT
ARG SOURCE_BRANCH

COPY ./InkBall/src/InkBall.Module/InkBall.Module.csproj ./InkBall/src/InkBall.Module/InkBall.Module.csproj
COPY ./InkBall/test/InkBall.Tests/InkBall.Tests.csproj ./InkBall/test/InkBall.Tests/InkBall.Tests.csproj
COPY ./AspNetCore.ExistingDb.Tests/AspNetCore.ExistingDb.Tests.csproj ./AspNetCore.ExistingDb.Tests/AspNetCore.ExistingDb.Tests.csproj
COPY ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySqlConfig.Tools/Pomelo.Extensions.Caching.MySqlConfig.Tools.csproj ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySqlConfig.Tools/Pomelo.Extensions.Caching.MySqlConfig.Tools.csproj
COPY ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySql/Pomelo.Extensions.Caching.MySql.csproj ./Caching-MySQL/src/Pomelo.Extensions.Caching.MySql/Pomelo.Extensions.Caching.MySql.csproj
COPY ./Caching-MySQL/test/Pomelo.Extensions.Caching.MySql.Tests/Pomelo.Extensions.Caching.MySql.Tests.csproj ./Caching-MySQL/test/Pomelo.Extensions.Caching.MySql.Tests/Pomelo.Extensions.Caching.MySql.Tests.csproj
COPY ./IdentityManager2/src/IdentityManager2/IdentityManager2.csproj ./IdentityManager2/src/IdentityManager2/IdentityManager2.csproj
COPY ./IdentityManager2/src/IdentityManager2/Assets/ ./IdentityManager2/src/IdentityManager2/Assets/
COPY ./DevReload/DevReload.csproj ./DevReload/DevReload.csproj
COPY ./AspNetCore.ExistingDb/AspNetCore.ExistingDb.csproj ./AspNetCore.ExistingDb/AspNetCore.ExistingDb.csproj
COPY ./*.sln ./NuGet.config ./
RUN dotnet restore -r linux-x64

COPY . .
RUN sed -i -e "s/GIT_HASH/$SOURCE_COMMIT/g" -e "s/GIT_BRANCH/$SOURCE_BRANCH/g" AspNetCore.ExistingDb/Views/Home/About.cshtml
RUN dotnet test -v m
RUN dotnet publish -c Release -r linux-x64 \
    #-p:PublishWithAspNetCoreTargetManifest=false #remove this after prerelease patch publish \
	/p:ShowLinkerSizeComparison=true /p:CrossGenDuringPublish=false \
    AspNetCore.ExistingDb





FROM mcr.microsoft.com/dotnet/core/runtime-deps:3.1-buster-slim
WORKDIR /app
COPY --from=build --chown=www-data:www-data /build/AspNetCore.ExistingDb/bin/Release/netcoreapp3.1/linux-x64/publish/ /build/startApp.sh ./

ENV TZ=Europe/Warsaw USER=www-data ASPNETCORE_URLS=http://+:5000
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

USER "$USER"

VOLUME /shared
EXPOSE 5000

ENTRYPOINT ["./AspNetCore.ExistingDb"]
