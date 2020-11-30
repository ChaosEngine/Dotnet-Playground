#!/bin/bash
#
# Build:
#		DOCKER_BUILDKIT=1 docker build \
#			--build-arg SOURCE_BRANCH=$(git rev-parse --abbrev-ref HEAD) \
#			--build-arg SOURCE_COMMIT=$(git rev-parse HEAD) \
#			--build-arg PROJECT_VERSION=$(xmllint --xpath 'string(//Version)' AspNetCore.ExistingDb/AspNetCore.ExistingDb.csproj) \
#			-f Dockerfile.alpine \
#			-t chaosengine/aspnetcore:alpine3.1 .
#
# Run:
#		docker run -it --rm -p 8080:5000 --name aspnetcore --env-file docker-env.txt -v /home/container/EntityFramework.Docs/sockets:/sockets -v /home/container/EntityFramework.Docs/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/aspnetcore
#		docker run -it --rm -p 8080:5000 --name aspnetcore --env-file docker-env.txt -v /home/container/EntityFramework.Docs/sockets:/sockets -v /home/container/EntityFramework.Docs/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/aspnetcore:alpine
#
docker run -d --rm -p 8080:5000 --name dotnetplayground --env-file docker-prod.env -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery:ro chaosengine/dotnetplayground:alpine5.0
