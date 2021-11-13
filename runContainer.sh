#!/bin/bash
#
# Build:
#		DOCKER_BUILDKIT=1 docker build \
#		    --build-arg SOURCE_BRANCH=$(git rev-parse --abbrev-ref HEAD) \
#		    --build-arg SOURCE_COMMIT=$(git rev-parse HEAD) \
#		    -f Dockerfile.alpine \
#		    -t chaosengine/dotnetplayground:alpine6.0 .
#
# Run:
#		docker run -it --rm -p 8080:5000 --name dotnetplayground --env-file docker-env.txt -v /home/container/Dotnet-Playground/sockets:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/dotnetplayground:6.0
#		docker run -it --rm -p 8080:5000 --name dotnetplayground --env-file docker-env.txt -v /home/container/Dotnet-Playground/sockets:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/dotnetplayground:alpine6.0
#
docker run -d --rm -p 8080:5000 --name dotnetplayground --hostname DotnetPlayground --env-file docker-prod.env -v /run/mysqld:/sockets -v $(pwd)/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery:ro chaosengine/dotnetplayground:alpine6.0
