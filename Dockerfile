FROM microsoft/aspnetcore

ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000

COPY AspNetCore.ExistingDb/bin/Release/netcoreapp2.0/publish/ /app
COPY startApp.sh /app
WORKDIR /app

ENV USER www-data
RUN	chown -R $USER:$USER /app
USER "$USER"

VOLUME /shared

ENTRYPOINT ["./startApp.sh"]
