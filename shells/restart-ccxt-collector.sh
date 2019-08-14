#!/bin/bash

if [[ $EUID > 0 ]]; then # we can compare directly with this syntax.
  
  echo "Please run as root/sudo"
  exit 1

else
	
	supervisorctl stop ccxt.ocollector

	supervisorctl start ccxt.ocollector

fi
