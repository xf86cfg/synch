#!/bin/bash
source ./varsfuncs.sh

echo_info "\n*** UNINSTALL STARTED ***"
echo_info "\nChecking current user privileges"
if [ $(whoami) == "root" ]; then
    echo_success "OK."
else
    echo_error "Uninstall is supposed to run using sudo."
    exit 1;
fi

if [ -f "${INSTALL_DESCRIPTOR}" ]; then
    DEFAULT_INSTALL_DIR=$(<$INSTALL_DESCRIPTOR)
fi

echo_info "\nSpecify the directory where ${BUILD_NAME} has been installed:"
clean_stdin
read -r -p "[${DEFAULT_INSTALL_DIR}]:" INSTALL_DIR
INSTALL_DIR=${INSTALL_DIR:-${DEFAULT_INSTALL_DIR}}

echo_info "\nAre you sure you want to uninstall ${BUILD_NAME}?"
clean_stdin
if confirm_yesno "Confirm uninstall"; then
    echo_info "Confirmed."
else
    echo_info "Declined."
    exit 2
fi

echo_info "\nChecking systemd"
if [ -f "${SYSTEMD_PATH}" ]; then
    echo_info "\nRemoving systemd service ${SYSTEMD_PATH}"
    if sudo systemctl disable ${BUILD_NAME} && sudo rm -rf ${SYSTEMD_PATH}; then
        echo_success "OK."
    else
        echo_error "Uninstall failed."
    exit 1
    fi    
else
    echo_info "Systemd skipped"
fi

echo_info "\nDeleting ${INSTALL_DIR}"
if sudo rm -r ${INSTALL_DIR}; then
    echo_success "OK."
else
    echo_error "Uninstall failed."
    exit 1
fi

if [ -f "${INSTALL_DESCRIPTOR}" ]; then
    sudo rm ${INSTALL_DESCRIPTOR}
fi

echo_success "\n*** UNINSTALL COMPLETED SUCCESSFULLY ***\n"