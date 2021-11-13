#!/bin/bash

DOCKER=/usr/bin/docker
#DOCKER=echo
image=''
dockerfile_name=''
dockerfile_suffix=''
dockerfile_args=''

case $1 in
    "latest"|"ubuntu"|"default"|"debian")
				image="latest";
				dockerfile_name="Dockerfile";
				echo "image would be $image";
			;;
    "" | "alpine")
				image="alpine6.0";
				dockerfile_name="Dockerfile.alpine";
				echo "image would be $image";
			;;
esac

case $2 in
    "oracle")
				dockerfile_suffix=".oracle6.0"
				dockerfile_args="--build-arg BUILD_CONFIG=Oracle"
				echo "subimage would be $dockerfile_suffix"
			;;
    *)
				dockerfile_suffix=""
				dockerfile_args=""
				echo "default subimage"
			;;
esac

#echo "${dockerfile_name}${dockerfile_suffix} | chaosengine/dotnetplayground:${image}${dockerfile_suffix}_last"
#exit 0

$DOCKER tag "chaosengine/dotnetplayground:${image}${dockerfile_suffix}" "chaosengine/dotnetplayground:${image}${dockerfile_suffix}_last"

DOCKER_BUILDKIT=1 $DOCKER build \
	--build-arg SOURCE_BRANCH="$(git rev-parse --abbrev-ref HEAD)" \
	--build-arg SOURCE_COMMIT="$(git rev-parse HEAD)" $dockerfile_args \
	--progress=auto \
	-f "${dockerfile_name}" \
	-t "chaosengine/dotnetplayground:${image}${dockerfile_suffix}" .

