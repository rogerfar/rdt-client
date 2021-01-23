docker build --tag rogerfar/rdtclient:amd64 --build-arg ARCH=amd64 .
docker push rogerfar/rdtclient:amd64

docker build -t rogerfar/rdtclient:arm64v8 --build-arg ARCH=arm64v8 .
docker push rogerfar/rdtclient:arm64v8

docker manifest create rogerfar/rdtclient:latest --amend rogerfar/rdtclient:amd64 --amend rogerfar/rdtclient:arm64v8

docker manifest push rogerfar/rdtclient:latest