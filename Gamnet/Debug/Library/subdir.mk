################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Library/Base64.cpp \
../Library/Buffer.cpp \
../Library/Exception.cpp \
../Library/MD5.cpp \
../Library/Random.cpp \
../Library/Variant.cpp 

OBJS += \
./Library/Base64.o \
./Library/Buffer.o \
./Library/Exception.o \
./Library/MD5.o \
./Library/Random.o \
./Library/Variant.o 

CPP_DEPS += \
./Library/Base64.d \
./Library/Buffer.d \
./Library/Exception.d \
./Library/MD5.d \
./Library/Random.d \
./Library/Variant.d 


# Each subdirectory must supply rules for building sources it contributes
Library/%.o: ../Library/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -D_DEBUG -I/usr/local/include -I/usr/local/Cellar/boost/1.64.0_1/include -I/usr/local/Cellar/openssl/1.0.2l/include -I/usr/local/Cellar/curl/7.55.1/include -I/usr/local/Cellar/mysql-connector-c/6.1.6/include -I/usr/local/mysql-connector-c-6.1.1/include -O0 -g3 -Wall -c -fmessage-length=0 -std=c++11 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@:%.o=%.d)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


