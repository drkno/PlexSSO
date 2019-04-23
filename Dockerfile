FROM node:current-alpine as builder
COPY ./backend /app
COPY ./ui /app/ui_build
RUN cd /app/ui_build && \
    yarn && \
    yarn build && \
    mv build ../ui && \
    cd /app && \
    rm -r ui_build

FROM python:3-slim
LABEL maintainer="Matthew Knox <matthew@makereti.co.nz>"
COPY --from=builder /app /app
RUN pip install -r /app/requirements.txt && \
    rm /app/requirements.txt
WORKDIR /app
EXPOSE 4200
ENTRYPOINT ["python", "main.py"]