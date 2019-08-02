#!/bin/bash
source ./varsfuncs.sh

echo_info "\n*** INSTALLATION STAGE STARTED ***"
echo_info "\nChecking current user privileges"
if [ $(whoami) == "root" ]; then
    echo_success "OK."
else
    echo_error "Install is supposed to run using sudo."
    exit 1;
fi


if [ -f "${BUILD_DESCRIPTOR}" ]; then
    DEFAULT_BUILD_DIR=$(<$BUILD_DESCRIPTOR)
fi

echo_info "\nSpecify build source directory or press Enter to use the default source directory:"
clean_stdin
read -r -p "[${DEFAULT_BUILD_DIR}]:" BUILD_DIR
BUILD_DIR=${BUILD_DIR:-${DEFAULT_BUILD_DIR}}

echo_info "\nSpecify installation directory or press Enter to install in the default directory:"
echo_info "WARNING: if installation directory exists its content will be fully overriden"
clean_stdin
read -r -p "[${DEFAULT_INSTALL_DIR}]:" INSTALL_DIR
INSTALL_DIR=${INSTALL_DIR:-${DEFAULT_INSTALL_DIR}}

echo_info "\nSpecify system user and group, that runs Asterisk process on this system or press Enter to use default values:"
echo_info "User:"
clean_stdin
read -r -p "[${DEFAULT_USER}]:" USER
USER=${USER:-${DEFAULT_USER}}
echo_info "Group:"
read -r -p "[${DEFAULT_GROUP}]:" GROUP
GROUP=${GROUP:-${DEFAULT_GROUP}}

echo_info "\nSetting up installation directory ${INSTALL_DIR}"
if [ -d "${INSTALL_DIR}" ]; then
    echo_info "${INSTALL_DIR} exists. Cleaning its content"
    if sudo rm -r ${INSTALL_DIR}/*; then
        echo_success "OK."
    else
        echo_error "Failed to clean content of ${INSTALL_DIR}."
        exit 1;
    fi
else
    echo_info "${INSTALL_DIR} doesn't exists."
    echo_info "Creating ${INSTALL_DIR}"
    if sudo mkdir -p "${INSTALL_DIR}"; then
        echo_success "OK."
    else
        echo_error "Failed to create installation directory." 
        exit 1
    fi
fi

echo_info "\nCopying ${BUILD_NAME} build into ${INSTALL_DIR}"
if sudo cp -r $BUILD_DIR/* ${INSTALL_DIR}; then
    echo $INSTALL_DIR > $INSTALL_DESCRIPTOR
    echo_success "OK."
else
    echo_error "Failed to copy build." 
    exit 1
fi

echo_info "\nSetting up file permissions on ${INSTALL_DIR}"
if sudo chown -R $USER:$GROUP $INSTALL_DIR && chmod +x $INSTALL_DIR/$EXEC_FILE; then
    echo_success "OK."
else
    echo_error "Failed to set up file permissions."
    exit 1
fi

echo_info "\nSetting up file capabilities on ${INSTALL_DIR}/${EXEC_FILE}"
if sudo setcap cap_net_bind_service=+ep ${INSTALL_DIR}/${EXEC_FILE}; then
    echo_success "OK."
else
    echo_error "Failed to set up file capabilities."
    exit 1
fi

echo_info "\nDo you want to configure systemd to run ${BUILD_NAME} as a service?"
clean_stdin
if confirm_yesno "Configure systemd?"; then
    echo_info "Confirmed."
else
    echo_info "Declined."
    echo_success "\n*** INSTALLAION STAGE COMPLETED SUCCESSFULLY ***"
    echo_success "\nSystemd has not been configured. You will need to run '${INSTALL_DIR}/${EXEC_FILE}' manually"
    echo_success "\nPlease configure ${INSTALL_DIR}/${SETTINGS_FILE} before first application run.\n"
    exit 2
fi

echo_info "\nSetting up systemd service ${SYSTEMD_PATH}"
if setup_systemd_service "${SYSTEMD_PATH}" && sudo systemctl enable "${BUILD_NAME}"; then
    echo_success "OK."
else
    echo_error "Failed to set up systemd service."
    exit 1
fi

echo_success "\n*** INSTALLAION STAGE COMPLETED SUCCESSFULLY ***"
echo_success "\nSystemd has been configured."
echo_success "\nPlease configure ${INSTALL_DIR}/${SETTINGS_FILE} before first application run.\n"
