################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Database/MySQL/Connection.cpp \
../Database/MySQL/MySQL.cpp \
../Database/MySQL/ResultSet.cpp \
../Database/MySQL/Transaction.cpp 

OBJS += \
./Database/MySQL/Connection.o \
./Database/MySQL/MySQL.o \
./Database/MySQL/ResultSet.o \
./Database/MySQL/Transaction.o 

CPP_DEPS += \
./Database/MySQL/Connection.d \
./Database/MySQL/MySQL.d \
./Database/MySQL/ResultSet.d \
./Database/MySQL/Transaction.d 


# Each subdirectory must supply rules for building sources it contributes
Database/MySQL/%.o: ../Database/MySQL/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -D_DEBUG -I/usr/local/include -I/usr/local/Cellar/boost/1.64.0_1/include -I/usr/local/Cellar/openssl/1.0.2l/include -I/usr/local/Cellar/curl/7.55.1/include -I/usr/local/Cellar/mysql-connector-c/6.1.6/include -I/usr/local/mysql-connector-c-6.1.1/include -O0 -g3 -Wall -c -fmessage-length=0 -std=c++11 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@:%.o=%.d)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


