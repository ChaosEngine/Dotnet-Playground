#!/bin/sh
#
sleep 3s && chmod 666 /sockets/www.sock &

dotnet AspNetCore.ExistingDb.dll
