#!/bin/bash
source ./varsfuncs.sh

echo_info "\n*** BUILD STAGE STARTED ***"
echo_info "\nSpecify destination build directory or press Enter to build in the default directory:"
#clean_stdin
#read -r -p "[${DEFAULT_BUILD_DIR}]:" BUILD_DIR
#BUILD_DIR=${BUILD_DIR:-${DEFAULT_BUILD_DIR}}

echo_info "\nCleaning ${BUILD_NAME}"
if dotnet clean $PROJECT --configuration $CONFIG --runtime $RUNTIME --verbosity $VERBOSE --output $DEFAULT_BUILD_DIR; then
    echo_success "${BUILD_NAME} cleaning completed."
else
    echo_error "${BUILD_NAME} cleaning failed."
    exit 1
fi

echo_info "\nBuilding ${BUILD_NAME}"
if dotnet build $PROJECT --configuration $CONFIG --runtime $RUNTIME --verbosity $VERBOSE --output $DEFAULT_BUILD_DIR; then
    echo $BUILD_DIR > $BUILD_DESCRIPTOR
    echo_success "${BUILD_NAME} build completed in $DEFAULT_BUILD_DIR"
else
    echo_error "${BUILD_NAME} build failed."
    exit 1
fi
echo_success "\n*** BUILD STAGE COMPLETED SUCCESSFULLY ***"
echo_success "\nPlease run 'sudo install.sh' to install ${BUILD_NAME} on this system.\n"