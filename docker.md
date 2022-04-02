# Running in Docker

```
docker build -t auth-gateway .
docker run -it --rm -p 8080:8080 -e Logging__Console__FormatterName=Simple --name auth-gateway auth-gateway
```

## Map Config Folder

```
docker run -it --rm -p 8080:8080 --volume /c/temp/conf:/app/conf -e GATEWAY_CONFIG=conf/conf.json -e Logging__Console__FormatterName=Simple --name auth-gateway auth-gateway
```

This assumes you put your config file locally into the folder ``c:\temp\conf`` and call it ``conf.json``.

