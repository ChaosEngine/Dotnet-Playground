#!/bin/bash

DOCKER=/usr/bin/docker
#DOCKER=echo
image=''
dockerfile_name=''
dockerfile_suffix=''
dockerfile_args=''
base_image_variant=''
runtime_id=''

case $1 in
    "latest"|"ubuntu"|"default"|"debian")
				image="latest";
				dockerfile_name="Dockerfile";
				base_image_variant="noble";
				runtime_id="linux-x64";
				echo "image would be $image";
			;;

    "arm64"|"aarch64")
				image="alpine";
				dockerfile_name="Dockerfile.alpine";
				base_image_variant="alpine-arm64v8";
				runtime_id="linux-musl-arm64";
				echo "image would be $image";
			;;
			
    "" | "alpine")
				image="alpine";
				dockerfile_name="Dockerfile.alpine";
				base_image_variant="alpine";
				runtime_id="linux-musl-x64";
				echo "image would be $image";
			;;
esac

case $2 in
    "oracle")
				dockerfile_suffix=".oracle"
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
	--build-arg SOURCE_COMMIT="$(git rev-parse HEAD)" \
	--build-arg BASE_IMAGE_VARIANT="${base_image_variant}" \
	--build-arg RUNTIME_ID="${runtime_id}" $dockerfile_args \
	--progress=auto \
	-f "${dockerfile_name}" \
	-t "chaosengine/dotnetplayground:${image}${dockerfile_suffix}" .

