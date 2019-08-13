## dotnet publish

```
$ sudo supervisorctl stop ccxt.ocollector
$ sudo dotnet restore -r ubuntu.18.04-x64
$ sudo dotnet publish -c Release -r ubuntu.18.04-x64 -f netcoreapp3.0 -o /var/ccxt.ocollector
$ sudo supervisorctl start ccxt.ocollector
```

## supervisor

```
$ sudo nano /etc/supervisor/conf.d/ccxt.ocollector.conf
```

```
[program:ccxt.ocollector]
command=/usr/bin/dotnet /var/ccxt.ocollector/ocollector.dll
directory=/var/ccxt.ocollector/
autostart=true
autorestart=true
stderr_logfile=/var/log/ccxt.ocollector.err.log
stdout_logfile=/var/log/ccxt.ocollector.out.log
environment=Hosting:Environment=Production
;priority=999						; the relative start priority (default 999)
;startsecs=10						; # of secs prog must stay up to be running (def. 1)
;startretries=3						; max # of serial start failures when starting (default 3)
;user=www-data						; setuid to this UNIX account to run the program
;stopsignal=SIGINT					; signal used to kill process (default TERM)
```

## logrotate

```
$ sudo nano /etc/logrotate.d/ccxt.ocollector
```

```
/var/log/ccxt.ocollector.out.log {
        rotate 4
        weekly
        missingok
        create 640 root adm
        notifempty
        compress
        delaycompress
        postrotate
		/usr/sbin/service supervisor reload > /dev/null
        endscript
}

/var/log/ccxt.ocollector.err.log {
        rotate 4
        weekly
        missingok
        create 640 root adm
        notifempty
        compress
        delaycompress
}
```