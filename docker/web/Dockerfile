ARG node_version=10
ARG WEB_BASE_HREF=/
FROM centos/nodejs-${node_version}-centos7:${node_version}
ENV WEB_BASE_HREF=${WEB_BASE_HREF}
ENV WEB1_BASE_HREF=${WEB_BASE_HREF}
WORKDIR /opt/app-root/src
COPY . .
RUN echo "$WEB1_BASE_HREF"
RUN echo "$WEB_BASE_HREF"
RUN npm install
USER root
RUN npm run build 