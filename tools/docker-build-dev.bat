docker stop rdtclientdev
docker rm rdtclientdev
docker build --tag rdtclientdev .
docker run --cap-add=NET_ADMIN -d -v C:/Temp/RdtClient/:/data/downloads -v C:/Temp/RdtClient/:/data/db --log-driver json-file --log-opt max-size=10m -p 6500:6500 --name rdtclientdev rdtclientdev
docker exec -it rdtclientdev /bin/bash