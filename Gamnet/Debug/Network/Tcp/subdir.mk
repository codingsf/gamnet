################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Network/Tcp/Packet.cpp \
../Network/Tcp/Session.cpp \
../Network/Tcp/Tcp.cpp 

OBJS += \
./Network/Tcp/Packet.o \
./Network/Tcp/Session.o \
./Network/Tcp/Tcp.o 

CPP_DEPS += \
./Network/Tcp/Packet.d \
./Network/Tcp/Session.d \
./Network/Tcp/Tcp.d 


# Each subdirectory must supply rules for building sources it contributes
Network/Tcp/%.o: ../Network/Tcp/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -D_DEBUG -I/usr/local/include -I/usr/local/Cellar/boost/1.64.0_1/include -I/usr/local/Cellar/openssl/1.0.2l/include -I/usr/local/Cellar/curl/7.55.1/include -I/usr/local/Cellar/mysql-connector-c/6.1.6/include -I/usr/local/mysql-connector-c-6.1.1/include -O0 -g3 -Wall -c -fmessage-length=0 -std=c++11 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@:%.o=%.d)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '

