docker stop rdtclient
docker rm rdtclient
docker build --tag rdtclient .
docker-compose up -d