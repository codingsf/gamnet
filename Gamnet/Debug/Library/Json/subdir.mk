################################################################################
# Automatically-generated file. Do not edit!
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
CPP_SRCS += \
../Library/Json/jsoncpp.cpp 

OBJS += \
./Library/Json/jsoncpp.o 

CPP_DEPS += \
./Library/Json/jsoncpp.d 


# Each subdirectory must supply rules for building sources it contributes
Library/Json/%.o: ../Library/Json/%.cpp
	@echo 'Building file: $<'
	@echo 'Invoking: GCC C++ Compiler'
	g++ -D_DEBUG -I/usr/include/mysql -O0 -g3 -Wall -c -fmessage-length=0 -std=c++11 -MMD -MP -MF"$(@:%.o=%.d)" -MT"$(@)" -o "$@" "$<"
	@echo 'Finished building: $<'
	@echo ' '


