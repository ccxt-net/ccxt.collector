#!/bin/bash

if [[ $EUID > 0 ]]; then # we can compare directly with this syntax.
  
  echo "Please run as root/sudo"
  exit 1

else
	
	cd ~

	mkdir github.com
	cd github.com
	mkdir lisa3907
	cd lisa3907
	git clone https://github.com/lisa3907/ccxt.collector.git

	cd ccxt.collector
	chmod +x ./shells/publish.ccxt.collector.sh
	./shells/publish.ccxt.collector.sh

	cd ~
	rm -r github.com

fi
