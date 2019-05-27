BUILD_NAME="synch"
PROJECT="synch.csproj"
CONFIG="Release"
RUNTIME="linux-x64"
TARGET="netcoreapp2.2"
VERBOSE="normal"
EXEC_FILE="synch"
DEFAULT_BUILD_DIR="bin/${CONFIG}/${TARGET}/${RUNTIME}"
DEFAULT_INSTALL_DIR="/opt/synch"
DEFAULT_USER="asterisk"
DEFAULT_GROUP="asterisk"
SYSTEMD_PATH="/etc/systemd/system/${BUILD_NAME}.service"
SETTINGS_FILE="appsettings.json"
BUILD_DESCRIPTOR=".buildpath"
INSTALL_DESCRIPTOR=".installpath"

function echo_info () {
    local BEGIN="\033[1;34m"
    local END="\033[0m"
    echo -e "${BEGIN}${1}${END}"
}

function echo_error () {
    local BEGIN="\033[0;31m"
    local END="\033[0m"
    echo -e "${BEGIN}${1}${END}" >&2
}

function echo_success () {
    local BEGIN="\033[1;32m"
    local END="\033[0m"
    echo -e "${BEGIN}${1}${END}"
}

function clean_stdin()
{
    while read -s -e -t 0.1; do : ; done
}

function confirm_yesno()
{
    read -r -p "${1} [y/N]" response
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])+$ ]]; then
        return 0
    else
        return 1
    fi
}

function setup_systemd_service()
{
    sudo echo "[Unit]" > $1
    sudo echo "Description=Synch service" >> $1
    sudo echo "After=network.target" >> $1
    sudo echo "[Service]" >> $1
    sudo echo "Type=simple" >> $1
    sudo echo "Restart=always" >> $1
    sudo echo "RestartSec=1" >> $1
    sudo echo "User=${USER}" >> $1
    sudo echo "ExecStart=${INSTALL_DIR}/${EXEC_FILE}" >> $1
    sudo echo "StandardOutput=syslog" >> $1
    sudo echo "StandardError=syslog" >> $1
    sudo echo "SyslogIdentifier=${BUILD_NAME}" >> $1
    sudo echo "[Install]" >> $1
    sudo echo "WantedBy=multi-user.target" >> $1
}