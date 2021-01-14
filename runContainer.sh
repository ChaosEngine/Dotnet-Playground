#!/bin/bash
#
# Build:
#		DOCKER_BUILDKIT=1 $DOCKER build \
#		    --build-arg SOURCE_BRANCH=$(git rev-parse --abbrev-ref HEAD) \
#		    --build-arg SOURCE_COMMIT=$(git rev-parse HEAD) \
#		    -f Dockerfile.alpine \
#		    -t chaosengine/dotnetplayground:alpine5.0 .
#
# Run:
#		docker run -it --rm -p 8080:5000 --name dotnetplayground --env-file docker-env.txt -v /home/container/Dotnet-Playground/sockets:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/dotnetplayground
#		docker run -it --rm -p 8080:5000 --name dotnetplayground --env-file docker-env.txt -v /home/container/Dotnet-Playground/sockets:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/dotnetplayground:alpine
#
docker run -d --rm -p 8080:5000 --name dotnetplayground --env-file docker-prod.env -v /run/mysqld:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery:ro chaosengine/dotnetplayground:alpine5.0
