#!/bin/bash

# TODO: Optional configuration type
# ex. ./startup.sh build debug/release

# Shitcord + lavalink startup/build script
#
# Required dependencies:
#   - GNU screen
#   - .Net SDK
#   - Lavalink
#
# by Kihau 2022 

SHITCORD_DIR="${HOME}/Software/Shitcord"
LAVALINK_DIR="${HOME}/Servers/Lavalink"

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &> /dev/null && pwd)

function start {
    screen -dmS "lavalink"
    screen -S "lavalink" -X stuff "cd ${LAVALINK_DIR}/$(printf \\r)"
    screen -S "lavalink" -X stuff "java -jar lavalink.jar$(printf \\r)"

    screen -dmS "shitcord"
    screen -S "shitcord" -X stuff "cd ${SHITCORD_DIR}/$(printf \\r)"
    screen -S "shitcord" -X stuff "./Shitcord$(printf \\r)"
}

function stop {
    LAVALINK="$(screen -ls | grep lavalink)"
    if [ -n "$LAVALINK" ]; then
        echo $LAVALINK | cut -d. -f1 | awk '{print $1}' | xargs kill -9
        screen -wipe
    fi

    SHITCORD="$(screen -ls | grep shitcord)"
    if [ -n "$SHITCORD" ]; then
        echo $SHITCORD | cut -d. -f1 | awk '{print $1}' | xargs kill -9
        screen -wipe
    fi

    screen -wipe
}

function restart {
    stop
    start
}

function build {
    cd "${SCRIPT_DIR}/DSharpPlus"
    git pull
    
    cd "${SCRIPT_DIR}/Shitcord"
    git pull

    dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained false

    cd "${SCRIPT_DIR}/Shitcord/bin/Release/net7.0/linux-x64/publish/"
    mv Shitcord "${SHITCORD_DIR}/"
    find *.so -type f -print -exec mv -v {} "${SHITCORD_DIR}" \;

    if [ ! -d "${SHITCORD_DIR}/Resources" ]; then
        mv "Resources" "${SHITCORD_DIR}"
    fi
}

function rebuild {
    stop
    build
    start
}

# --------------------------------- #
#           ENTRY POINT             #
# --------------------------------- #

if [ ! -d "${LAVALINK_DIR}" ]; then
    mkdir -p "${LAVALINK_DIR}"
    echo "Creating new lavalink directory. Please copy lavalink files there"
    exit 0
fi

if [ ! -d "${SHITCORD_DIR}" ]; then
    mkdir -p "${SHITCORD_DIR}"
fi

case "$1" in
    start)
        start
    ;;
    stop)
        stop
    ;;
    restart)
        restart
    ;;
    build)
        build
    ;;
    rebuild)
        rebuild
    ;;
    
    *)
        echo "Usage: $0 {start|stop|restart|build|rebuild}"
esac
