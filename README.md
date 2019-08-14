# ccxt.collector

## dotnet core

### Register Microsoft key and feed
​
Before installing .NET, you'll need to register the Microsoft key, register the product repository, and install required dependencies. This only needs to be done once per machine.
​
Open a terminal and run the following commands:
​
```
wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
```
​
### Install the .NET SDK
​
Update the products available for installation, then install the .NET SDK.
​
In your terminal, run the following commands:
​
```
sudo add-apt-repository universe
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-2.2
```

## ubuntu server install utility

Install supervisor for restart when service stopped.

```
~$ sudo apt -y install supervisor
```

Install fail2ban vaccine program to prevent hacking.

```
~$ sudo apt -y install faile2ban
```

Install rabbit-mq for publishing orderbook stream to subscrivers.

```
~$ sudo apt -y install rabbitmq-server
~$ sudo rabbitmq-plugins enable rabbitmq_management
~$ sudo service rabbitmq-server restart
~$ sudo rabbitmqctl add_user odinsoft p@ssw0rd
~$ sudo rabbitmqctl set_user_tags odinsoft administrator
~$ sudo rabbitmqctl add_vhost OCollector
~$ sudo rabbitmqctl set_permissions -p OCollector odinsoft ".*" ".*" ".*"
```
