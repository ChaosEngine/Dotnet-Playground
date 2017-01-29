#!/bin/bash
#
docker run -it --rm -v $(pwd):/app --workdir /app microsoft/aspnetcore-build bash -c "dotnet restore && dotnet publish -c Release -o publish-output && find publish-output -type d -exec chmod u=rwx,g=rwx,o=rx {} \;; find publish-output -type f -exec chmod u=rw,g=rw,o=r {} \;"
