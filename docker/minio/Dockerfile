# Local MinIO image built from the last community source tag.
# minio/minio was removed from Docker Hub (Sep 2026), quay.io and dl.min.io no longer serve it.
# Used on the dev server (see docker-compose.override.example.yml). Build from the repo root:
#   docker build -t travelagency-minio:RELEASE.2025-10-15T17-29-55Z docker/minio
FROM golang:1.25-alpine AS build
ARG MINIO_TAG=RELEASE.2025-10-15T17-29-55Z
RUN apk add --no-cache git
RUN git clone --depth 1 --branch ${MINIO_TAG} https://github.com/minio/minio.git /src
WORKDIR /src
ENV CGO_ENABLED=0 GOFLAGS=-trimpath GOMAXPROCS=2
RUN go build -ldflags "-s -w" -o /out/minio .

FROM alpine:3.22
RUN apk add --no-cache ca-certificates curl
COPY --from=build /out/minio /usr/bin/minio
EXPOSE 9000 9001
VOLUME ["/data"]
ENTRYPOINT ["/usr/bin/minio"]
