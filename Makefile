.PHONY: docker run

CONTAINER=sa-telebot

docker:
	docker build -t $(CONTAINER) .

run:
	docker run -it $(CONTAINER)
