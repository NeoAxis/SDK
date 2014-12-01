project(HACD_TEST)
include(${CMAKE_COMMON_INC})
add_executable(testHACD ${PROJECT_CPP_FILES} ${PROJECT_C_FILES} ${PROJECT_INC_FILES} ${PROJECT_INL_FILES})

target_link_libraries(testHACD HACD_LIB)
include_directories("${CMAKE_SOURCE_DIR}/HACD_Lib/inc")
