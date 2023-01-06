FROM mcr.microsoft.com/dotnet/runtime:7.0
# Copy data for add-on
ADD app.tar.gz /app
COPY run.sh /

RUN chmod a+x /run.sh

CMD [ "/bin/bash", "/run.sh" ]
