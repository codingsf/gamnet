################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Database/Redis/Connection.cpp \
../Database/Redis/Redis.cpp \
../Database/Redis/ResultSet.cpp 

OBJS += \
./Database/Redis/Connection.o \
./Database/Redis/Redis.o \
./Database/Redis/ResultSet.o 

CPP_DEPS += \
./Database/Redis/Connection.d \
./Database/Redis/Redis.d \
./Database/Redis/ResultSet.d 


# Each subdirectory must supply rules for building sources it contributes
Database/Redis/%.o: ../Database/Redis/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -D_DEBUG -I/usr/local/include -I/usr/local/Cellar/boost/1.64.0_1/include -I/usr/local/Cellar/openssl/1.0.2l/include -I/usr/local/Cellar/curl/7.55.1/include -I/usr/local/Cellar/mysql-connector-c/6.1.6/include -I/usr/local/mysql-connector-c-6.1.1/include -O0 -g3 -Wall -c -fmessage-length=0 -std=c++11 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@:%.o=%.d)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


