#!/bin/bash 
# First parameter is the execution location of this script instance

if [ "$2" = "get-command-definitions" ]; then
	# Definition format usually represented as a single line:

	# Script description|
	# command1|"Command1 description"
	# 	param|"Param description" end
	# end
	# command2|"Command2 description"
	# 	param|"Param description" end
	# end

	echo "Runs the console project from the bin/AutoTest.Net folder"
	exit
fi

./FSWatcher.Console/bin/AutoTest.Net/FSWatcher.Console.exe
