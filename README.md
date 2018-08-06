# 分流 DNS 服务器

## DNS2PROXY

通过 SOCKS/HTTP 代理的 DNS 服务器，用于搭建无污染的 DNS 服务器

### Linux 简单编译
`gcc -Wall DNS2PROXY.c -o DNS2PROXY -lpthread`

### Windows 编译
VS...一键...你懂的

### Docker
`docker pull hmbsbige/dnserver:dns2proxy`

### 运行参数
```
Usage:

DNS2PROXY [/?] [/t] [/d] [/q] [/l[a]:FilePath] [/u:User /p:Password]
          [Socks5ServerIP[:Port]] [DNSServerIPorName[:Port]] [ListenIP[:Port]]

/?            to view this help
/t            to use a HTTP proxy instead of a SOCKS server
              (here: Socks5ServerIP = HttpProxyIP, no support for /u and /p)
/d            to disable the cache
/q            to suppress the text output
/l:FilePath   to create a new log file "FilePath"
/la:FilePath  to create a new log file or append to the existing "FilePath"
/u:User       user name if your SOCKS server uses user/password authentication
/p:Password   password if your SOCKS server uses user/password authentication

Default Socks5ServerIP:Port = 127.0.0.1:23333
Default DNSServerIPorName:Port = 1.1.1.1:53
Default ListenIP:Port = 127.0.0.1:5533
```

### 用法
关闭缓存，SOCKS 代理地址为 `10.1.1.1:23333`, 远程 DNS 服务器地址为 `1.1.1.1:53`, 监听端口为 `0.0.0.0:53`

#### Windows/Linux
```
DNS2PROXY /d 10.1.1.1:23333 1.1.1.1:53 0.0.0.0:53
```

#### Docker
```
docker run -itd --name=dns2proxy --restart=always -p 53:53/udp hmbsbige/dnserver:dns2proxy 10.1.1.1:23333 1.1.1.1:53 0.0.0.0:53
```

## DNServer
将指定列表内的域名用所指定的上游 DNS 服务器解析，不在列表内的域名用所指定的无污染 DNS 服务器解析

附：
[国内域名列表](https://raw.githubusercontent.com/HMBSbige/Text_Translation/master/chndomains.txt)

### 一些无污染公共 DNS 服务器
* 101.6.6.6:53
* 202.141.162.123:53
* 223.113.97.99:53
* 208.67.222.222:5353
* 208.67.220.220:443

当然也可以自行用 `DNS2PROXY` 自行搭建无污染 DNS 服务器

### 编译
```
dotnet publish -c release -r $RID
```
例如：
```
dotnet publish -c release -r win-x64
dotnet publish -c release -r linux-x64
dotnet publish -c release -r osx-x64
```
具体RID：https://docs.microsoft.com/zh-cn/dotnet/core/rid-catalog

### Win7、Win2008 等运行错误
```
Failed to load the dll from [?X], HRESULT: 0x80070057
```
安装补丁：KB2533623

https://support.microsoft.com/en-us/help/2533623/microsoft-security-advisory-insecure-library-loading-could-allow-remot

### .Net 编译所需：
https://www.microsoft.com/net/download/all

### Docker
`docker pull hmbsbige/dnserver:DNServer`

### 运行参数
```
  --verbose        (Default: false) Prints all messages to standard output.

  -b, --bindip     (Default: 0.0.0.0:53) Listen on...

  -u, --updns      (Default: 114.114.114.114:53) Up DNS Server

  -p, --puredns    Pure DNS Server

  -l, --list       (Default: chndomains.txt) Domains list file path

  --help           Display this help screen.

  --version        Display version information.
```
### 用法
上游 DNS 服务器为 `114.114.114.114,119.29.29.29`, 无污染 DNS 服务器为 `101.6.6.6:53`, 分流列表路径 `~/list/chndomains.txt`, 监听端口为 `0.0.0.0:53`

#### Windows/Linux
```
DNServer -u 114.114.114.114,119.29.29.29 -p 101.6.6.6:53 -l ~/list/chndomains.txt -b 0.0.0.0:53
```

#### Docker
```
docker run -itd --name=dns --restart=always -p 53:53/udp -v ~/list:/list:ro hmbsbige/dnserver:DNServer -u 114.114.114.114,119.29.29.29 -p 101.6.6.6:53 -l /list/chndomains.txt -b 0.0.0.0:53
```
