################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Network/Router/LinkManager.cpp \
../Network/Router/Router.cpp \
../Network/Router/RouterCaster.cpp \
../Network/Router/RouterHandler.cpp \
../Network/Router/Session.cpp 

OBJS += \
./Network/Router/LinkManager.o \
./Network/Router/Router.o \
./Network/Router/RouterCaster.o \
./Network/Router/RouterHandler.o \
./Network/Router/Session.o 

CPP_DEPS += \
./Network/Router/LinkManager.d \
./Network/Router/Router.d \
./Network/Router/RouterCaster.d \
./Network/Router/RouterHandler.d \
./Network/Router/Session.d 


# Each subdirectory must supply rules for building sources it contributes
Network/Router/%.o: ../Network/Router/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -D_DEBUG -I/usr/include/mysql -O0 -g3 -Wall -c -fmessage-length=0 -std=c++11 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


