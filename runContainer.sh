#!/bin/bash
#
# Build:
#		DOCKER_BUILDKIT=1 docker build \
#		    --build-arg SOURCE_BRANCH=$(git rev-parse --abbrev-ref HEAD) \
#		    --build-arg SOURCE_COMMIT=$(git rev-parse HEAD) \
#		    -f Dockerfile.alpine \
#		    -t chaosengine/dotnetplayground:alpine .
#
# Run:
#		docker run -it --rm -p 8080:5000 --name dotnetplayground --env-file docker-env.txt -v /home/container/Dotnet-Playground/sockets:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/dotnetplayground
#		docker run -it --rm -p 8080:5000 --name dotnetplayground --env-file docker-env.txt -v /home/container/Dotnet-Playground/sockets:/sockets -v /home/container/Dotnet-Playground/shared:/shared -v /var/www/localhost/htdocs/webcamgallery:/webcamgallery chaosengine/dotnetplayground:alpine
#
docker run -d --rm -p 8080:8080 --name dotnetplayground -e TZ=$(cat /etc/timezone) --env-file docker-prod.env -v $(pwd)/shared:/shared -v $(pwd)/shared/webcamgallery:/webcamgallery:ro chaosengine/dotnetplayground:9.0
