FROM mcr.microsoft.com/dotnet/runtime:6.0
# Copy data for add-on
ADD artifacts.tar.gz /app
COPY run.sh /

RUN chmod a+x /run.sh
RUN chmod a+x /app/Easywave2Mqtt

CMD [ "/bin/bash", "/run.sh" ]
