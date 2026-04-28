FROM alpine:3.20

RUN apk add --no-cache socat

COPY deploy/mikro-relay/relay.sh /usr/local/bin/relay.sh

RUN chmod +x /usr/local/bin/relay.sh

ENTRYPOINT ["/usr/local/bin/relay.sh"]