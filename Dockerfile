FROM ubuntu

COPY ./DNS2PROXY/DNS2PROXY /usr/local/bin/

RUN chmod a+x /usr/local/bin/DNS2PROXY

ENTRYPOINT ["DNS2PROXY"]