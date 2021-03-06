# Photon Counters plugin for AWS CloudWatch

Amazon CloudWatch is a monitoring service for AWS cloud resources and the applications you run on AWS. 
You can use Amazon CloudWatch to collect and track metrics, collect and monitor log files, and set alarms. 
Amazon CloudWatch can monitor AWS resources such as Amazon EC2 instances, Amazon DynamoDB tables, and Amazon RDS DB instances, as well as custom metrics generated by your applications and services, and any log files your applications generate. 
You can use Amazon CloudWatch to gain system-wide visibility into resource utilization, application performance, and operational health. 
You can use these insights to react and keep your application running smoothly.

To publish metrics to Amazon CloudWatch with "CounterPublisher" you need to use the plugin from this repository.
It uses native Amazon SDK CloudWatch client to call API over HTTP/HTTPS.

## How To Use

1. Copy content of this repository to the root directory of your Photon Server SDK. Current tested version is v4-0-29-11263.
2. Using Visual Studio, open "src-server\CounterPublisher.AWS.CloudWatch\CounterPublisher.AWS.CloudWatch.sln" solution and build project.
3. Modify CounterPublisher section with your own settings inside the following files:

    - "deploy\CounterPublisher\bin\CounterPublisher.dll.config" 
    - "deploy\LoadBalancing\Master\bin\Photon.LoadBalancing.dll.config"
    - "deploy\LoadBalancing\GameServer\bin\Photon.LoadBalancing.dll.config"

   An example can be found in this repository [here](https://github.com/PhotonEngine/photon.counterpublisher.cloudwatch/blob/master/deploy/CounterPublisher/bin/CounterPublisher.dll.config).
   
4. Restart Photon Server.

## Enable Debugging

To enable debugging of Photon Counters add the following to a "log4net.config" file to one of the server applications ("CounterPublisher", "MasterServer" or "GameServer"):

```
<logger name="ExitGames.Diagnostics">
  <level value="DEBUG" />    
</logger>
```

Log entries will start to show for the respective application.

![PhotonCountersOnCloudWatch](https://raw.githubusercontent.com/PhotonEngine/photon.counterpublisher.cloudwatch/master/PhotonCountersOnCloudWatch.PNG)

Read more about "[Publishing to Amazon AWS CloudWatch](https://doc.photonengine.com/en-us/server/v4/performance/photon-counters#publishing_to_amazon_aws_cloudwatch)".
